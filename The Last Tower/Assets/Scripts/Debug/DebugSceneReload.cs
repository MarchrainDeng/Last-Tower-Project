using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugSceneReload : MonoBehaviour
{
    private void Update()
    {
        // 按下R键时重新加载当前场景
        // Rキーを押した時、現在のシーンを再読み込みする
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            ReloadCurrentScene();
        }
    }

    /// <summary>
    /// 重新加载当前场景
    /// 現在のシーンを再読み込みする
    /// </summary>
    private void ReloadCurrentScene()
    {
        // 防止之前的暂停状态残留
        // 以前の一時停止状態が残らないようにする
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
