using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EmergencySceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("Emergency Input Settings")]
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float triggerThreshold = 0.8f;

    private float holdTimer = 0f;

    // 是否已经触发
    // すでに発動したか
    private bool hasTriggered = false;

    // 是否正在等待玩家松开LT和RT
    // LTとRTが離されるのを待っているか
    private bool waitingForRelease = false;

    private void Update()
    {
        if (Gamepad.current == null)
        {
            holdTimer = 0f;
            return;
        }

        float leftTrigger =
            Gamepad.current.leftTrigger.ReadValue();

        float rightTrigger =
            Gamepad.current.rightTrigger.ReadValue();

        bool bothPressed =
            leftTrigger >= triggerThreshold &&
            rightTrigger >= triggerThreshold;

        // 已触发后，等待玩家把两个扳机松开
        // 発動後は両トリガーが離されるまで待つ
        if (waitingForRelease)
        {
            if (!bothPressed)
            {
                waitingForRelease = false;
                hasTriggered = false;
                holdTimer = 0f;
            }

            return;
        }

        if (hasTriggered)
            return;

        if (bothPressed)
        {
            // 暂停状态下也可以正常计时
            // ポーズ中でも正常に計測する
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdDuration)
            {
                EmergencyReturnToTitle();
            }
        }
        else
        {
            // 任意一个扳机松开就重新计时
            // どちらかを離したらリセットする
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
        waitingForRelease = true;
        holdTimer = 0f;

        // 恢复游戏时间
        // ゲーム時間を元に戻す
        Time.timeScale = 1f;

        // 解除游戏暂停状态
        // ゲームのポーズ状態を解除する
        GameStateManager.SetPaused(false);

        // 强制返回标题场景
        // タイトルシーンへ強制的に戻る
        SceneManager.LoadScene(titleSceneName);
    }
}