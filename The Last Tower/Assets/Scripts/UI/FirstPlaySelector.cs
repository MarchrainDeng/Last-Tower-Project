using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPlaySelector : MonoBehaviour
{
    [Header("References")]

    // 教学页面管理器
    // チュートリアルページ管理
    [SerializeField]
    private TutorialPageController tutorialPageController;

    private void Update()
    {
        if (Gamepad.current == null)
            return;

        // B键：Yes
        // Bボタン：Yes
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            ConfirmYes();
            return;
        }

        // A键：No
        // Aボタン：No
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ConfirmNo();
        }
    }

    /// <summary>
    /// 选择Yes，进入新手教学
    /// Yesを選択し、チュートリアルを開始する
    /// </summary>
    private void ConfirmYes()
    {
        // 关闭初次游玩确认界面
        // 初回プレイ確認画面を閉じる
        gameObject.SetActive(false);

        if (tutorialPageController != null)
        {
            tutorialPageController.StartTutorial();
        }
    }

    /// <summary>
    /// 选择No，跳过新手教学
    /// Noを選択し、チュートリアルをスキップする
    /// </summary>
    private void ConfirmNo()
    {
        // 关闭初次游玩确认界面
        // 初回プレイ確認画面を閉じる
        gameObject.SetActive(false);

        // ====================================
        // 跳过教学后的处理写在这里
        // チュートリアルをスキップした後の処理
        // ====================================

        Debug.Log("Tutorial Skipped");

        SceneFadeManager.Instance.LoadScene("MainScene");
    }

    /// <summary>
    /// 重新打开初次游玩确认界面
    /// 初回プレイ確認画面を再表示する
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
    }
}