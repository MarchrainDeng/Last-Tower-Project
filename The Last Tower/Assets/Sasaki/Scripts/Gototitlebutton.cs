using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルトUIのGameObjectにアタッチする
/// Aボタン / クリックでタイトルシーンに遷移する
/// </summary>
public class GoToTitleButton : MonoBehaviour
{
    [Header("── 遷移先シーン ────────────────")]
    public string titleSceneName = "Title";

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

        bool pressed = MenuInput.ConfirmPressed() || MenuInput.LeftClicked();

        if (pressed && isVictory)
        {
            StartSequence();
            return;
        }
        else if (pressed && !isVictory)
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