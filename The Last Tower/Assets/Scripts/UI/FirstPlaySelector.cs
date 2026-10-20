using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPlaySelector : MonoBehaviour
{
    [Header("References")]

    // 教学页面管理器
    // チュートリアルページ管理
    [SerializeField]
    private TutorialPageController tutorialPageController;

    [SerializeField] private float inputLockDuration = 0.5f;

    // 鼠标点击用的按钮（为空时按名字自动查找）
    // マウスクリック用のボタン（未設定なら子オブジェクトを名前で探す）
    [SerializeField] private RectTransform yesButton;
    [SerializeField] private RectTransform noButton;

    private float inputLockTimer;

    private void Awake()
    {
        if (yesButton == null)
            yesButton = transform.Find("YesButton") as RectTransform;
        if (noButton == null)
            noButton = transform.Find("NoButton") as RectTransform;
    }

    private void OnEnable()
    {
        // 表示されるたびに入力をロックする（チュートリアルから戻った時の誤入力防止）
        inputLockTimer = inputLockDuration;
    }

    private void Update()
    {
        // 场景开始后的短时间内禁止输入
        // シーン開始直後の一定時間は入力を禁止する
        if (inputLockTimer > 0f)
        {
            inputLockTimer -= Time.unscaledDeltaTime;
            return;
        }

        // A键 / 点击Yes：Yes
        // Aボタン / Yesクリック：Yes
        if (MenuInput.ConfirmPressed() ||
            (MenuInput.LeftClicked() && MenuInput.IsPointerOver(yesButton)))
        {
            ConfirmYes();
            return;
        }

        // B键 / 点击No：No
        // Bボタン / Noクリック：No
        if (MenuInput.CancelPressed() ||
            (MenuInput.LeftClicked() && MenuInput.IsPointerOver(noButton)))
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