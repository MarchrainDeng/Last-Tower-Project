using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
public class TutorialPageController : MonoBehaviour
{
    [Header("Tutorial Pages")]

    // 按顺序放入所有教学说明页面
    // すべてのチュートリアルページを順番に設定する
    [SerializeField]
    private GameObject[] tutorialPages;

    [Header("Input")]

    // 下一页按键
    [SerializeField]
    private GamepadButton nextButton =
    GamepadButton.East;

    // 上一页按键
    [SerializeField]
    private GamepadButton beforeButton =
        GamepadButton.West;

    // 当前页面编号
    // 現在のページ番号
    private int currentPageIndex = -1;

    // 当前是否正在教学
    // 現在チュートリアル中か
    private bool isTutorialActive = false;

    [Header("First Play UI")]

    [SerializeField]
    private GameObject firstPlayPanel;

    private bool canAcceptInput = false;

    private void Start()
    {
        // 游戏开始时隐藏所有教学页面
        // ゲーム開始時にすべてのページを非表示にする
        HideAllPages();
    }

    private void Update()
    {
        if (!isTutorialActive)
            return;

        if (!canAcceptInput)
            return;

        if (Gamepad.current == null)
            return;

        if (Gamepad.current[nextButton].wasPressedThisFrame)
        {
            ShowNextPage();
            return;
        }

        if (Gamepad.current[beforeButton].wasPressedThisFrame)
        {
            ShowPreviousPage();
        }
    }

    /// <summary>
    /// 开始教学
    /// チュートリアルを開始する
    /// </summary>
    public void StartTutorial()
    {
        if (tutorialPages == null ||
        tutorialPages.Length == 0)
        {
            return;
        }

        isTutorialActive = true;
        currentPageIndex = 0;

        ShowPage(currentPageIndex);

        // 刚进入教学时先不接受输入
        canAcceptInput = false;

        StartCoroutine(EnableInputNextFrame());
    }

    /// <summary>
    /// 显示下一页
    /// 次のページを表示する
    /// </summary>
    private void ShowNextPage()
    {
        if (!isTutorialActive)
            return;

        currentPageIndex++;

        // 已经超过最后一页
        // 最後のページを超えた場合
        if (currentPageIndex >=
            tutorialPages.Length)
        {
            EndTutorial();
            return;
        }

        ShowPage(currentPageIndex);
    }

    /// <summary>
    /// 返回上一页
    /// 前のページへ戻る
    /// </summary>
    private void ShowPreviousPage()
    {
        if (!isTutorialActive)
            return;

        // 当前是第一页
        if (currentPageIndex == 0)
        {
            ReturnToFirstPlayPanel();
            return;
        }

        // 返回上一页
        currentPageIndex--;

        ShowPage(currentPageIndex);
    }

    /// <summary>
    /// 从教学第一页返回初次游玩确认界面
    /// チュートリアル1ページ目から初回プレイ確認画面へ戻る
    /// </summary>
    private void ReturnToFirstPlayPanel()
    {
        // 隐藏所有教学页面
        HideAllPages();

        // 结束当前教学状态
        isTutorialActive = false;
        currentPageIndex = -1;

        // 重新显示初次游玩界面
        if (firstPlayPanel != null)
        {
            firstPlayPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 显示指定页面
    /// 指定したページを表示する
    /// </summary>
    private void ShowPage(int index)
    {
        HideAllPages();

        if (index < 0 ||
            index >= tutorialPages.Length)
        {
            return;
        }

        if (tutorialPages[index] != null)
        {
            tutorialPages[index]
                .SetActive(true);
        }
    }

    /// <summary>
    /// 结束教学
    /// チュートリアルを終了する
    /// </summary>
    private void EndTutorial()
    {
        HideAllPages();

        isTutorialActive = false;
        currentPageIndex = -1;

        // =====================================
        // 教学全部结束后的事件写这里
        // チュートリアル終了後の処理
        // =====================================

        Debug.Log("Tutorial Finished");
    }

    /// <summary>
    /// 隐藏所有教学页面
    /// すべてのページを非表示にする
    /// </summary>
    private void HideAllPages()
    {
        if (tutorialPages == null)
            return;

        foreach (GameObject page
                 in tutorialPages)
        {
            if (page != null)
            {
                page.SetActive(false);
            }
        }
    }

    private IEnumerator EnableInputNextFrame()
    {
        // 等待一帧，避免进入教学的B键同时触发下一页
        yield return null;

        canAcceptInput = true;
    }
}