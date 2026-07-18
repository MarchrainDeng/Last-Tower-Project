using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルトUIのGameObjectにアタッチする
/// コントローラーの指定ボタンでタイトルシーンに遷移する
/// </summary>
public class GoToTitleButton : MonoBehaviour
{
    [Header("── 遷移先シーン ────────────────")]
    public string titleSceneName = "Title";

    [Header("── 操作 ──────────────────────")]
    public GamepadButton toggleGamepadButton = GamepadButton.South; // 遷移に使うコントローラーボタン

    void Update()
    {
        if (Gamepad.current != null && Gamepad.current[toggleGamepadButton].wasPressedThisFrame)
        {
            GoToTitle();
        }
    }

     void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}