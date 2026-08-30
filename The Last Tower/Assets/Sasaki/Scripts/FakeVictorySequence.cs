using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// 偽勝利（ボス戦勝利後の偽エンディング）演出を制御するスクリプト
///
/// 【流れ】
/// 1. ボスHP0 → 画面が1秒かけて完全に黒くなる
/// 2. 画面中央に「勝利！」表示
/// 3. 2秒後、「勝利！」が上に0.5秒で移動、ゲーム内容を1秒ごとに1項目表示
/// 4. 3つ目の項目表示時にカメラシェイク＋コントローラー振動が1回発生
/// 5. 1秒後、画面が3秒間大きく揺れ、UI全体が3秒かけて崩壊（落下＋回転）
/// 6. 崩壊完了後、ボスの叫びでタワーも破壊し、トウscene（最終ブロック構築フェーズ）へ移行
///    ※transitionToTouSceneOnCollapse を false にすると旧仕様（③④⑤ボタン表示）に戻せる
/// </summary>
public class FakeVictorySequence : MonoBehaviour
{
    [Header("── UI参照 ────────────────────────")]
    [SerializeField] private CanvasGroup bgPanelGroup;         // 真っ黒な背景用CanvasGroup
    [SerializeField] private RectTransform victoryTextRect;    // 「勝利！」テキストのRectTransform
    [SerializeField] private List<GameObject> resultItems;     // ゲーム内容（1:倒した敵の数、2:配置したブロックの数...）

    [Header("── 統計テキスト（resultItemsと同じ並び順） ──")]
    [SerializeField] private TMP_Text enemiesDefeatedText;
    [SerializeField] private TMP_Text blocksPlacedText;
    [SerializeField] private TMP_Text blocksDroppedText;
    [SerializeField] private TMP_Text blocksConnectedText;
    [SerializeField] private GameObject buttonGroup;           // ボタン群（③リプレイ / ④スタッフロール / ⑤タイトル）
    [SerializeField] private RectTransform uiRoot;             // 崩壊させるUI全体のルート

    [Header("── 演出パラメータ ──────────────────")]
    [SerializeField] private Vector2 textTopPos = new Vector2(0, 250f); // テキスト移動後の座標
    [SerializeField] private float fadeDuration = 1f;           // 暗転にかかる時間
    [SerializeField] private float textMoveDuration = 0.5f;     // テキスト上移動にかかる時間
    [SerializeField] private float itemDisplayInterval = 1f;    // 各項目の表示間隔（秒）
    [SerializeField] private int shakeOnItemIndex = 2;          // 何個目の項目表示時に揺れるか（0始まり、3つ目=2）

    [Header("── 崩壊前の揺れ ────────────────────")]
    [SerializeField] private float preCollapseDelay = 1f;       // 項目表示後、崩壊が始まるまでの待機
    [SerializeField] private float collapseShakeDuration = 3f;  // 画面が大きく揺れる時間
    [SerializeField] private float collapseDuration = 3f;       // UIが崩壊するまでの時間

    [Header("── カメラシェイク・振動 ────────────")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float itemShakeVibeLow = 0.3f;
    [SerializeField] private float itemShakeVibeHigh = 0.5f;
    [SerializeField] private float itemShakeVibeDuration = 0.2f;
    [SerializeField] private float collapseVibeLow = 0.6f;
    [SerializeField] private float collapseVibeHigh = 0.9f;

    [Header("── トウscene移行（新仕様） ──────────")]
    [Tooltip("true: 崩壊完了後、ボスの叫びでタワーも破壊してトウscene(最終ブロック構築フェーズ)へ移行する。false: 旧仕様どおりその場でリザルトボタンを表示する")]
    [SerializeField] private bool transitionToTouSceneOnCollapse = true;

    [Header("── ボタン操作（横並び：③④⑤） ────────")]
    [SerializeField] private RectTransform[] buttonRects = new RectTransform[3]; // 0:リプレイ 1:スタッフロール 2:タイトル
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float normalScale = 0.9f;
    [SerializeField] private float scaleLerpSpeed = 10f;
    [SerializeField] private GamepadButton confirmGamepadButton = GamepadButton.South;
    [SerializeField] private float stickDeadZone = 0.5f;
    [SerializeField] private float navInputCooldown = 0.2f;

    [Header("── ボタンの挙動 ────────────────")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string titleSceneName = "MainMenu";
    [SerializeField] private GameObject staffRollObject;

    int buttonFocus = 0;
    bool buttonInputEnabled = false;
    float navInputTimer = 0f;

    /// <summary>
    /// 演出の開始
    /// </summary>
    public void PlaySequence()
    {
        StartCoroutine(SequenceCoroutine());
    }

    private IEnumerator SequenceCoroutine()
    {
        // ─── 統計値を反映 ───
        if (GameStatsManager.Instance != null)
        {
            if (enemiesDefeatedText != null) enemiesDefeatedText.text = GameStatsManager.Instance.EnemiesDefeated.ToString();
            if (blocksPlacedText != null) blocksPlacedText.text = GameStatsManager.Instance.BlocksPlaced.ToString();
            if (blocksDroppedText != null) blocksDroppedText.text = GameStatsManager.Instance.BlocksDropped.ToString();
            if (blocksConnectedText != null) blocksConnectedText.text = GameStatsManager.Instance.BlocksConnected.ToString();
        }

        // ─── 初期状態の設定 ───
        bgPanelGroup.alpha = 0f;
        victoryTextRect.gameObject.SetActive(false);
        foreach (var item in resultItems)
        {
            if (item != null) item.SetActive(false);
        }
        if (buttonGroup != null)
            buttonGroup.SetActive(false);
        buttonInputEnabled = false;

        // 1. ボスHP0 → 画面が1秒かけて完全に黒くなる
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            bgPanelGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        bgPanelGroup.alpha = 1f;

        // 2. 画面中央に「勝利！」表示
        victoryTextRect.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);

        // 3. 「勝利！」が上に移動
        Vector2 startPos = victoryTextRect.anchoredPosition;
        timer = 0f;
        while (timer < textMoveDuration)
        {
            timer += Time.unscaledDeltaTime;
            victoryTextRect.anchoredPosition = Vector2.Lerp(startPos, textTopPos, timer / textMoveDuration);
            yield return null;
        }
        victoryTextRect.anchoredPosition = textTopPos;

        // ゲーム内容を1秒ごとに1項目ずつ表示
        for (int i = 0; i < resultItems.Count; i++)
        {
            if (resultItems[i] != null)
                resultItems[i].SetActive(true);

            // 4. 指定した項目（3つ目）表示時に一回揺れる
            if (i == shakeOnItemIndex)
            {
                if (cameraShake != null)
                    cameraShake.Shake();
                GamepadVibrationManager.Instance?.PlayVibration(itemShakeVibeLow, itemShakeVibeHigh, itemShakeVibeDuration);
            }

            yield return new WaitForSecondsRealtime(itemDisplayInterval);
        }

        // 5. 1秒後、画面が3秒間大きく揺れて、UIが3秒かけて崩壊
        yield return new WaitForSecondsRealtime(preCollapseDelay);

        StartCoroutine(CollapseShakeRoutine());
        yield return StartCoroutine(CollapseUIRoutine());

        // 6. 崩壊完了後
        Debug.Log("[FakeVictorySequence] 崩壊演出完了。transitionToTouSceneOnCollapse=" + transitionToTouSceneOnCollapse);
        if (transitionToTouSceneOnCollapse)
        {
            // ボスの叫びでタワーも破壊し、トウscene(最終ブロック構築フェーズ)へ移行する
            // BlockManager.StartFinalSequence() が既存のタワー破壊/カメラ移動/最終ブロック選択の開始処理を持っている
            if (BlockManager.Instance != null)
            {
                Debug.Log("[FakeVictorySequence] BlockManager.Instance.StartFinalSequence() を呼び出します");
                BlockManager.Instance.StartFinalSequence();
            }
            else
            {
                Debug.LogWarning("[FakeVictorySequence] BlockManager.Instance が見つからないため、トウsceneへ移行できませんでした");
            }
        }
        else
        {
            // (旧仕様) その場でリザルトボタンを表示する
            if (buttonGroup != null)
                buttonGroup.SetActive(true);

            buttonFocus = 0;
            navInputTimer = navInputCooldown;
            buttonInputEnabled = true;
        }
    }

    // ─── 画面が大きく揺れ続ける（カメラ＋コントローラー） ────────────
    IEnumerator CollapseShakeRoutine()
    {
        if (cameraShake == null) yield break;

        float timer = 0f;
        while (timer < collapseShakeDuration)
        {
            cameraShake.Shake();
            GamepadVibrationManager.Instance?.PlayVibration(collapseVibeLow, collapseVibeHigh, 0.15f);
            timer += 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    // ─── UI全体が下に落下しながら回転して崩壊する ────────────────────
    IEnumerator CollapseUIRoutine()
    {
        if (uiRoot == null) yield break;

        // uiRoot直下の子要素それぞれを個別に崩壊させる
        List<RectTransform> pieces = new List<RectTransform>();
        foreach (Transform child in uiRoot)
        {
            var rt = child.GetComponent<RectTransform>();
            if (rt != null) pieces.Add(rt);
        }

        // 各要素ごとにランダムな回転方向・落下速度・遅延を設定
        var startPositions = new Vector2[pieces.Count];
        var startRotations = new float[pieces.Count];
        var fallDistances = new float[pieces.Count];
        var rotateSpeeds = new float[pieces.Count];
        var delays = new float[pieces.Count];

        for (int i = 0; i < pieces.Count; i++)
        {
            startPositions[i] = pieces[i].anchoredPosition;
            startRotations[i] = pieces[i].eulerAngles.z;
            fallDistances[i] = Random.Range(600f, 1200f);
            rotateSpeeds[i] = Random.Range(-360f, 360f);
            delays[i] = Random.Range(0f, collapseDuration * 0.3f);
        }

        float timer = 0f;
        while (timer < collapseDuration)
        {
            timer += Time.unscaledDeltaTime;

            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == null) continue;

                float localTime = Mathf.Max(0f, timer - delays[i]);
                float duration = Mathf.Max(0.01f, collapseDuration - delays[i]);
                float t = Mathf.Clamp01(localTime / duration);

                // 重力っぽく加速しながら落下（イーズイン）
                float fallT = t * t;
                float y = startPositions[i].y - fallDistances[i] * fallT;

                pieces[i].anchoredPosition = new Vector2(startPositions[i].x, y);
                pieces[i].eulerAngles = new Vector3(0f, 0f, startRotations[i] + rotateSpeeds[i] * t);

                // 落下しながら徐々に薄くする
                var canvasGroup = pieces[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        // 崩壊終了後、UI要素は非表示にしておく
        if (uiRoot != null)
            uiRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!buttonInputEnabled) return;

        // 選択中のボタンだけ拡大、それ以外は縮小
        for (int i = 0; i < buttonRects.Length; i++)
        {
            if (buttonRects[i] == null) continue;
            float targetScale = (i == buttonFocus) ? selectedScale : normalScale;
            float current = buttonRects[i].localScale.x;
            float next = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * scaleLerpSpeed);
            buttonRects[i].localScale = new Vector3(next, next, 1f);
        }

        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (horizontal > stickDeadZone)
            {
                MoveButtonFocus(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                MoveButtonFocus(-1);
                navInputTimer = navInputCooldown;
            }
        }

        if (Gamepad.current[confirmGamepadButton].wasPressedThisFrame)
        {
            ConfirmButton();
        }
    }

    void MoveButtonFocus(int direction)
    {
        int count = buttonRects.Length;
        buttonFocus = (buttonFocus + direction + count) % count;
    }

    void ConfirmButton()
    {
        buttonInputEnabled = false;

        switch (buttonFocus)
        {
            case 0: // リプレイ
                Time.timeScale = 1f;
                SceneManager.LoadScene(gameSceneName);
                break;

            case 1: // スタッフロール
                if (staffRollObject != null)
                    staffRollObject.SetActive(true);
                break;

            case 2: // タイトル
                Time.timeScale = 1f;
                SceneManager.LoadScene(titleSceneName);
                break;
        }
    }
}