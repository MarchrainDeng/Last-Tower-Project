using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialSceneTest : MonoBehaviour
{
    [SerializeField]
    private string tutorialSceneName = "TutorialScene";

    private void Update()
    {
        // 按下G键进入Tutorial场景
        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
    }
}