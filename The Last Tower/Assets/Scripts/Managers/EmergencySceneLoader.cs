using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EmergencySceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]

    // 应急返回的场景名称
    // 緊急時に戻るシーン名
    [SerializeField] private string titleSceneName = "Title";

    [Header("Emergency Input Settings")]

    // 需要同时长按的时间
    // 同時長押しに必要な時間
    [SerializeField] private float holdDuration = 3f;

    // 扳机判定阈值
    // トリガー入力の判定しきい値
    [SerializeField] private float triggerThreshold = 0.8f;

    private float holdTimer = 0f;

    // 防止重复触发
    // 重複発動を防止する
    private bool hasTriggered = false;

    private void Update()
    {
        if (hasTriggered)
            return;

        if (Gamepad.current == null)
        {
            holdTimer = 0f;
            return;
        }

        // 获取LT和RT的输入值
        // LTとRTの入力値を取得する
        float leftTrigger =
            Gamepad.current.leftTrigger.ReadValue();

        float rightTrigger =
            Gamepad.current.rightTrigger.ReadValue();

        // 判断两个扳机是否同时按下
        // 両方のトリガーが同時に押されているか判定する
        bool bothPressed =
            leftTrigger >= triggerThreshold &&
            rightTrigger >= triggerThreshold;

        if (bothPressed)
        {
            // 使用真实时间，暂停状态下也可以正常计时
            // ポーズ中でも動作するように実時間を使用する
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdDuration)
            {
                EmergencyReturnToTitle();
            }
        }
        else
        {
            // 中途松开任意一个按键就重新计时
            // 途中でどちらかを離した場合はリセットする
            holdTimer = 0f;
        }
    }

    /// <summary>
    /// 强制返回标题界面
    /// タイトル画面へ強制的に戻る
    /// </summary>
    private void EmergencyReturnToTitle()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        // 恢复游戏时间
        // ゲーム時間を元に戻す
        Time.timeScale = 1f;

        // 如果你的项目使用GameStateManager
        // GameStateManagerを使用している場合
        GameStateManager.SetPaused(false);

        // 强制返回标题场景
        // タイトルシーンへ強制的に戻る
        SceneManager.LoadScene(titleSceneName);
    }
}