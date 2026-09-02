using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EmergencySceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Title";

    private bool hasTriggered = false;

    private void Update()
    {
        if (hasTriggered)
            return;

        if (Keyboard.current == null)
            return;

        // 检测Ctrl
        // Ctrlキーを検出する
        bool ctrlPressed =
            Keyboard.current.leftCtrlKey.isPressed ||
            Keyboard.current.rightCtrlKey.isPressed;

        // 检测Alt
        // Altキーを検出する
        bool altPressed =
            Keyboard.current.leftAltKey.isPressed ||
            Keyboard.current.rightAltKey.isPressed;

        // Esc + Ctrl + Alt
        // Esc + Ctrl + Alt
        if (Keyboard.current.escapeKey.isPressed &&
            ctrlPressed &&
            altPressed)
        {
            EmergencyReturn();
        }
    }

    /// <summary>
    /// 强制返回指定场景
    /// 指定シーンへ強制的に戻る
    /// </summary>
    private void EmergencyReturn()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        // 防止暂停状态影响返回后的游戏
        // 一時停止状態が次のシーンに影響しないようにする
        Time.timeScale = 1f;

        GameStateManager.SetPaused(false);

        SceneManager.LoadScene(targetSceneName);
    }
}