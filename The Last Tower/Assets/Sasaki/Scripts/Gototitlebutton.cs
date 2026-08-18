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

    public BlockManager blockManager;

    [SerializeField]
    private float autoStartDelay = 5f;

    private float timer = 0f;
    private bool hasStarted = false;

    public GameObject canvas;

    public bool isVictory = false;

    void Update()
    {
        if (hasStarted)
            return;

        if (Gamepad.current != null && Gamepad.current[toggleGamepadButton].wasPressedThisFrame && isVictory)
        {
            StartSequence();
            return;
        }
        else if (Gamepad.current != null && Gamepad.current[toggleGamepadButton].wasPressedThisFrame && !isVictory)
        {
            GoToTitle();
        }

            // 自动倒计时
            timer += Time.unscaledDeltaTime;

        if (timer >= autoStartDelay)
        {
            StartSequence();
        }
    }

     void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    private void StartSequence()
    {
        if (hasStarted)
            return;

        hasStarted = true;
        Time.timeScale = 1f;
        canvas.SetActive(false);
        GameStateManager.SetPaused(false);
        blockManager.StartFinalSequence();
    }
}