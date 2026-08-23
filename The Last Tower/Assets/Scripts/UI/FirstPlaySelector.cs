using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FirstPlaySelector : MonoBehaviour
{
    [Header("Buttons")]

    // “是”按钮
    [SerializeField]
    private Button yesButton;

    // “否”按钮
    [SerializeField]
    private Button noButton;

    [Header("Selection Settings")]

    // 当前选中的按钮
    // 0 = 是
    // 1 = 否
    [SerializeField]
    private int currentIndex = 0;

    // 摇杆触发切换的阈值
    [SerializeField]
    private float stickThreshold = 0.5f;

    // 摇杆回中阈值
    [SerializeField]
    private float returnThreshold = 0.2f;

    // 摇杆是否已经回中
    private bool stickReturned = true;

    [Header("Button Images")]

    [SerializeField]
    private Image yesButtonImage;

    [SerializeField]
    private Image noButtonImage;

    [SerializeField]
    private Sprite selectedSprite;

    [SerializeField]
    private Sprite normalSprite;

    private void Start()
    {
        UpdateSelection();
    }

    private void Update()
    {
        HandleStickSelection();
    }

    /// <summary>
    /// 处理左摇杆选择
    /// </summary>
    private void HandleStickSelection()
    {
        if (Gamepad.current == null)
            return;

        float horizontal =
            Gamepad.current.leftStick.x.ReadValue();

        // 摇杆必须先回中，才能进行下一次选择
        if (!stickReturned)
        {
            if (Mathf.Abs(horizontal) <= returnThreshold)
            {
                stickReturned = true;
            }

            return;
        }

        // 向左
        if (horizontal <= -stickThreshold)
        {
            currentIndex = 0;

            stickReturned = false;

            UpdateSelection();
        }
        // 向右
        else if (horizontal >= stickThreshold)
        {
            currentIndex = 1;

            stickReturned = false;

            UpdateSelection();
        }
    }

    /// <summary>
    /// 更新当前选中的按钮
    /// </summary>
    /// <summary>
    /// 更新按钮选中状态
    /// ボタンの選択状態を更新する
    /// </summary>
    private void UpdateSelection()
    {
        if (yesButtonImage == null ||
            noButtonImage == null)
        {
            return;
        }

        if (currentIndex == 0)
        {
            // “是”被选中
            // 「はい」が選択中
            yesButtonImage.sprite =
                selectedSprite;

            noButtonImage.sprite =
                normalSprite;
        }
        else
        {
            // “否”被选中
            // 「いいえ」が選択中
            yesButtonImage.sprite =
                normalSprite;

            noButtonImage.sprite =
                selectedSprite;
        }
    }
}
