using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームオーバー時の進行演出を制御するスクリプト
///
/// 【ボタン操作】
/// - buttonGroup表示後、スティック左右で ③リプレイ/④スタッフロール/⑤タイトル を選択
/// - 決定ボタンで実行
/// </summary>
public class GameOverSequence : MonoBehaviour
{
    [Header("── UI参照 ────────────────────────")]
    [SerializeField] private CanvasGroup bgPanelGroup;        // 真っ黒な背景用CanvasGroup
    [SerializeField] private RectTransform gameOverTextRect;  // 「Game Over!」テキストのRectTransform
    [SerializeField] private List<GameObject> resultItems;     // ゲーム内容（1:倒した敵の数、2:配置したブロックの数...）
    [SerializeField] private GameObject buttonGroup;          // ボタン群（③リプレイ / ④スタッフロール / ⑤タイトル）

    [Header("── 統計テキスト（resultItemsと同じ並び順） ──")]
    [SerializeField] private TMP_Text enemiesDefeatedText;
    [SerializeField] private TMP_Text blocksPlacedText;
    [SerializeField] private TMP_Text blocksDroppedText;
    [SerializeField] private TMP_Text blocksConnectedText;

    [Header("── 演出パラメータ ──────────────────")]
    [SerializeField] private Vector2 textTopPos = new Vector2(0, 250f); // テキスト移動後の座標
    [SerializeField] private float fadeDuration = 0.5f;        // 暗転にかかる時間
    [SerializeField] private float textMoveDuration = 0.5f;    // テキスト上移動にかかる時間
    [SerializeField] private float itemDisplayInterval = 0.8f; // 各項目の表示間隔（秒）

    [Header("── ボタン操作（横並び：③④⑤） ────────")]
    [SerializeField] private RectTransform[] buttonRects = new RectTransform[3]; // 0:リプレイ 1:スタッフロール 2:タイトル
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float normalScale = 0.9f;
    [SerializeField] private float scaleLerpSpeed = 10f; // 拡大縮小の速さ
    [SerializeField] private GamepadButton confirmGamepadButton = GamepadButton.South;
    [SerializeField] private float stickDeadZone = 0.5f;
    [SerializeField] private float navInputCooldown = 0.2f;

    // 各ボタンの実行内容（Inspectorで直接呼びたい場合はUnityEventにしてもよい）
    [Header("── ボタンの挙動 ────────────────")]
    [SerializeField] private string gameSceneName = "GameScene"; // リプレイで再読み込みするシーン
    [SerializeField] private string titleSceneName = "MainMenu"; // タイトルへ戻るシーン
    [SerializeField] private GameObject staffRollObject;         // スタッフロールのGameObject（SetActiveで表示）
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
        gameOverTextRect.gameObject.SetActive(false);
        foreach (var item in resultItems)
        {
            if (item != null) item.SetActive(false);
        }
        if (buttonGroup != null)
        {
            buttonGroup.SetActive(false);
        }
        buttonInputEnabled = false;

        // 1. 体力が0になったら画面が真っ黒になる（Fade In）
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            bgPanelGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        bgPanelGroup.alpha = 1f;

        // 2. なった後すぐ「Game Over!」を表示
        gameOverTextRect.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(1.2f);

        // 3. 「Game Over!」が上に移動
        Vector2 startPos = gameOverTextRect.anchoredPosition;
        timer = 0f;
        while (timer < textMoveDuration)
        {
            timer += Time.unscaledDeltaTime;
            gameOverTextRect.anchoredPosition = Vector2.Lerp(startPos, textTopPos, timer / textMoveDuration);
            yield return null;
        }
        gameOverTextRect.anchoredPosition = textTopPos;

        // 4. ゲーム内容を1項目ずつ順番に表示
        foreach (var item in resultItems)
        {
            if (item != null)
            {
                item.SetActive(true);
                yield return new WaitForSecondsRealtime(itemDisplayInterval);
            }
        }

        // 5. 全部表示した後、ボタン（リプレイ・スタッフロール・タイトル）を表示
        yield return new WaitForSecondsRealtime(0.3f);
        if (buttonGroup != null)
        {
            buttonGroup.SetActive(true);
        }

        // ボタン操作を有効化
        buttonFocus = 0;
        navInputTimer = navInputCooldown;
        buttonInputEnabled = true;
    }

    void Update()
    {
        if (!buttonInputEnabled) return;

        // 選択中のボタンだけ拡大、それ以外は縮小（毎フレーム滑らかに補間）
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
        buttonInputEnabled = false; // 二重押し防止

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