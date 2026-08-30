using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// 仕様8：インク演出が終わって必要なUIだけ残った後に表示する、
/// 「home に戻る」か「replay」かの最終選択UIを制御するスクリプト
///
/// 【使い方】
/// ・rootUI に、このボタン選択を含むUI（Canvas配下のGameObjectなど）をアサインする
/// ・buttonRects[0] = リプレイボタン、buttonRects[1] = ホーム(タイトル)ボタン をアサインする
/// ・仕様4〜7（キャノン発射／インク爆発／インクが引いてUIが残る演出）が完成したら、
///   その演出の最後で Show() を呼び出すようにする
///
/// ※現時点では仕様4〜7が未実装のため、暫定的に BlockManager.OnCountdownFinished() から
///   直接呼び出してテストできるようにしている（本実装ではインク演出完了後に差し替える）
/// </summary>
public class FinalResultChooser : MonoBehaviour
{
    [Header("── UI参照 ────────────────────────")]
    [SerializeField] private GameObject rootUI;               // このチューザー全体のルート（非表示⇔表示を切り替える）
    [SerializeField] private CanvasGroup fadeGroup;            // フェードインさせたい場合はアサイン（任意）
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("── 統計テキスト（FakeVictorySequence/GameOverSequenceと同じ並び） ──")]
    [SerializeField] private TMP_Text enemiesDefeatedText;
    [SerializeField] private TMP_Text blocksPlacedText;
    [SerializeField] private TMP_Text blocksDroppedText;
    [SerializeField] private TMP_Text blocksConnectedText;

    [Header("── ボタン操作（0:リプレイ 1:ホーム） ──")]
    [SerializeField] private RectTransform[] buttonRects = new RectTransform[2]; // 0:リプレイ 1:ホーム(タイトル)
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float normalScale = 0.9f;
    [SerializeField] private float scaleLerpSpeed = 10f;
    [SerializeField] private GamepadButton confirmGamepadButton = GamepadButton.South;
    [SerializeField] private float stickDeadZone = 0.5f;
    [SerializeField] private float navInputCooldown = 0.2f;

    [Header("── ボタンの挙動 ────────────────")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string titleSceneName = "MainMenu";

    int buttonFocus = 0;
    bool buttonInputEnabled = false;
    float navInputTimer = 0f;

    /// <summary>
    /// home/replay 選択UIを表示して入力を受け付け始める
    /// </summary>
    public void Show()
    {
        if (rootUI != null)
            rootUI.SetActive(true);

        if (GameStatsManager.Instance != null)
        {
            if (enemiesDefeatedText != null) enemiesDefeatedText.text = GameStatsManager.Instance.EnemiesDefeated.ToString();
            if (blocksPlacedText != null) blocksPlacedText.text = GameStatsManager.Instance.BlocksPlaced.ToString();
            if (blocksDroppedText != null) blocksDroppedText.text = GameStatsManager.Instance.BlocksDropped.ToString();
            if (blocksConnectedText != null) blocksConnectedText.text = GameStatsManager.Instance.BlocksConnected.ToString();
        }

        buttonFocus = 0;
        navInputTimer = navInputCooldown;
        buttonInputEnabled = true;

        if (fadeGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        fadeGroup.alpha = 0f;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;
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

            case 1: // ホーム(タイトル)
                Time.timeScale = 1f;
                SceneManager.LoadScene(titleSceneName);
                break;
        }
    }
}
