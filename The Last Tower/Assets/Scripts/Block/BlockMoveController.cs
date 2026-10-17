using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/*
----------------------------------------
【功能 / 機能】
控制当前下落方块的移动、旋转和快速下落。
現在落下中のブロックの移動・回転・高速落下を制御する。

【键盘鼠标操作 / キーボード・マウス操作】
A / D：左右移动
S：快速下落
Space：顺时针90度旋转
鼠标左键：平滑逆时针旋转
鼠标右键：平滑顺时针旋转

【手柄操作 / ゲームパッド操作】
左摇杆：左右移动
A：快速下落
LB：平滑逆时针旋转
RB：平滑顺时针旋转
可配置按键：顺时针90度旋转

【负责人 / 担当】
Deng Guangpeng
トウ コウホウ
----------------------------------------
*/

public class BlockMoveController : MonoBehaviour
{
    [Header("Move Settings")]

    // 摇杆死区
    // スティックのデッドゾーン
    public float deadZone = 0.2f;

    // 水平移动速度
    // 水平方向の移動速度
    public float moveSpeed = 0.01f;


    [Header("Rotate Settings")]

    // 平滑旋转速度（度/秒）
    // スムーズ回転速度（度/秒）
    public float rotateSpeed = 180f;


    [Header("Fall Settings")]

    // 正常下落速度
    // 通常の落下速度
    public float fallSpeed = 2f;

    // 快速下落倍率
    // 高速落下倍率
    public float fastFallMultiplier = 3f;


    [Header("Dead Zone")]

    // 越界判定Y坐标
    // 範囲外判定Y座標
    public float deadLineY = -8f;


    [Header("Input Settings")]

    // 手柄顺时针90度旋转按键
    // ゲームパッドの時計回り90度回転ボタン
    [SerializeField]
    private ConfigurableGamepadButton rotateClockwiseButton =
        ConfigurableGamepadButton.East;


    // 手柄按键配置
    // ゲームパッドボタン設定
    public enum ConfigurableGamepadButton
    {
        South,
        North,
        West,
        East,
        LeftShoulder,
        RightShoulder,
        LeftStick,
        RightStick,
        Start,
        Select,
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight
    }


    // 当前手柄
    // 現在のゲームパッド
    private Gamepad gamepad;

    // 刚体
    // Rigidbody
    private Rigidbody2D rb;

    // 水平移动输入
    // 水平方向の移動入力
    private float moveInput;

    // 快速下落输入
    // 高速落下入力
    private bool fastFallInput;

    // 是否等待快速下落按键松开
    // 高速落下ボタンが離されるのを待つか
    private bool waitingForFastFallRelease = true;

    // 是否已经触发越界
    // 範囲外処理済みか
    private bool isDead = false;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    private BlockSelectionFlowManager flowManager;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 自动寻找流程管理器
        // フローマネージャーを自動検索
        if (flowManager == null)
        {
            flowManager =
                FindFirstObjectByType<BlockSelectionFlowManager>();
        }

        if (flowManager == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: BlockSelectionFlowManager " +
                "没有找到，请确认场景中存在并处于启用状态。" +
                "\nBlockSelectionFlowManagerが見つかりません。",
                gameObject
            );
        }
    }


    private void Update()
    {
        gamepad = Gamepad.current;

        // 暂停时禁止操作
        // ポーズ中は操作を無効化
        if (GameStateManager.IsPaused)
        {
            moveInput = 0f;
            fastFallInput = false;
            return;
        }

        ReadMoveInput();
        ReadFallInput();
        HandleRotate();
        HandleDeadZone();
    }


    private void FixedUpdate()
    {
        // 暂停时禁止移动
        // ポーズ中は移動を無効化
        if (GameStateManager.IsPaused)
            return;

        if (isDead)
            return;

        HandleMovement();
    }


    /// <summary>
    /// 读取水平移动输入
    /// 水平方向の移動入力を取得
    /// </summary>
    private void ReadMoveInput()
    {
        moveInput = 0f;

        // 手柄左摇杆
        // ゲームパッドの左スティック
        if (gamepad != null)
        {
            moveInput = gamepad.leftStick.x.ReadValue();
        }

        // 键盘A/D
        // キーボードA/D
        if (Keyboard.current != null)
        {
            bool leftPressed =
                Keyboard.current.aKey.isPressed;

            bool rightPressed =
                Keyboard.current.dKey.isPressed;

            if (leftPressed && !rightPressed)
            {
                moveInput = -1f;
            }
            else if (rightPressed && !leftPressed)
            {
                moveInput = 1f;
            }
            else if (leftPressed && rightPressed)
            {
                moveInput = 0f;
            }
        }

        // 死区处理
        // デッドゾーン処理
        if (Mathf.Abs(moveInput) < deadZone)
        {
            moveInput = 0f;
        }
    }


    /// <summary>
    /// 读取快速下落输入
    /// 高速落下入力を取得
    /// </summary>
    private void ReadFallInput()
    {
        // 手柄A键
        // ゲームパッドのAボタン
        bool gamepadPressed =
            gamepad != null &&
            gamepad.buttonSouth.isPressed;

        // 键盘S键
        // キーボードのSキー
        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.sKey.isPressed;

        bool isPressed = gamepadPressed || keyboardPressed;

        // 新方块生成后，必须先松开快速下落按键
        // 新しいブロック生成後は、一度ボタンを離す必要がある
        if (waitingForFastFallRelease)
        {
            if (!isPressed)
            {
                waitingForFastFallRelease = false;
            }

            fastFallInput = false;
            return;
        }

        // 按住A或S时快速下落
        // AまたはSを押している間は高速落下
        fastFallInput = isPressed;
    }


    /// <summary>
    /// 处理方块移动（左右移动+下落）
    /// ブロックの移動（左右移動＋落下）
    /// </summary>
    private void HandleMovement()
    {
        float currentFallSpeed = fallSpeed;

        // 快速下落
        // 高速落下
        if (fastFallInput)
        {
            currentFallSpeed *= fastFallMultiplier;
        }

        // 合并水平移动和下落
        // 左右移動と落下をまとめて処理する
        Vector2 movement = new Vector2(
            moveInput * moveSpeed,
            -currentFallSpeed
        );

        rb.MovePosition(
            rb.position +
            movement * Time.fixedDeltaTime
        );
    }


    /// <summary>
    /// 处理旋转输入
    /// 回転入力を処理
    /// </summary>
    private void HandleRotate()
    {
        // 手柄LB：平滑逆时针旋转
        // ゲームパッドLB：反時計回りにスムーズ回転
        bool smoothCounterClockwise =
            gamepad != null &&
            gamepad.leftShoulder.isPressed;

        // 手柄RB：平滑顺时针旋转
        // ゲームパッドRB：時計回りにスムーズ回転
        bool smoothClockwise =
            gamepad != null &&
            gamepad.rightShoulder.isPressed;


        // 鼠标左键：平滑逆时针旋转
        // マウス左ボタン：反時計回りにスムーズ回転
        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            smoothCounterClockwise = true;
        }

        // 鼠标右键：平滑顺时针旋转
        // マウス右ボタン：時計回りにスムーズ回転
        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            smoothClockwise = true;
        }


        // 只按住一个方向时执行平滑旋转
        // 一方向のみ押されている場合に回転する
        if (smoothCounterClockwise && !smoothClockwise)
        {
            SmoothRotateCounterClockwise();
        }
        else if (smoothClockwise && !smoothCounterClockwise)
        {
            SmoothRotateClockwise();
        }


        // 手柄可配置按键：顺时针90度旋转
        // ゲームパッド設定ボタン：時計回り90度回転
        ButtonControl clockwiseButton =
            GetGamepadButton(rotateClockwiseButton);

        bool gamepadRotatePressed =
            clockwiseButton != null &&
            clockwiseButton.wasPressedThisFrame;


        // 键盘Space：顺时针90度旋转
        // キーボードSpace：時計回り90度回転
        bool keyboardRotatePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;


        if (gamepadRotatePressed || keyboardRotatePressed)
        {
            RotateClockwise();
        }
    }


    /// <summary>
    /// 顺时针旋转到下一个90度角度
    /// 時計回りに次の90度角度まで回転
    /// </summary>
    private void RotateClockwise()
    {
        // 获取当前Z轴角度
        // 現在のZ軸角度を取得
        float currentAngle = transform.eulerAngles.z;

        // 防止浮点数误差
        // 浮動小数点誤差を防ぐ
        currentAngle =
            Mathf.Round(currentAngle * 100f) / 100f;

        // 计算顺时针方向的下一个90度目标角度
        // 時計回り方向の次の90度角度を計算
        float targetAngle =
            Mathf.Ceil(currentAngle / 90f) * 90f - 90f;

        // 将角度限制在0～360之间
        // 角度を0～360の範囲に収める
        targetAngle =
            Mathf.Repeat(targetAngle, 360f);

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );
    }


    /// <summary>
    /// 平滑顺时针旋转
    /// 時計回りにスムーズ回転
    /// </summary>
    private void SmoothRotateClockwise()
    {
        transform.Rotate(
            0f,
            0f,
            -rotateSpeed * Time.deltaTime
        );
    }


    /// <summary>
    /// 平滑逆时针旋转
    /// 反時計回りにスムーズ回転
    /// </summary>
    private void SmoothRotateCounterClockwise()
    {
        transform.Rotate(
            0f,
            0f,
            rotateSpeed * Time.deltaTime
        );
    }


    /// <summary>
    /// 设置方块选择流程管理器
    /// ブロック選択フローマネージャーを設定
    /// </summary>
    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }


    /// <summary>
    /// 检测是否进入死亡区域
    /// デッドゾーンに入ったか判定
    /// </summary>
    private void HandleDeadZone()
    {
        if (isDead)
            return;

        if (transform.position.y < deadLineY)
        {
            isDead = true;

            // 通知流程管理器开始下一轮选择
            // フローマネージャーへ次の選択開始を通知
            if (flowManager != null)
            {
                Debug.Log("next selection");
                flowManager.OnCurrentBlockLanded();
            }

            Destroy(gameObject);
        }
    }


    /// <summary>
    /// 根据配置获取手柄按键
    /// 設定に応じたゲームパッドボタンを取得
    /// </summary>
    private ButtonControl GetGamepadButton(
        ConfigurableGamepadButton button)
    {
        if (gamepad == null)
            return null;

        switch (button)
        {
            case ConfigurableGamepadButton.South:
                return gamepad.buttonSouth;

            case ConfigurableGamepadButton.North:
                return gamepad.buttonNorth;

            case ConfigurableGamepadButton.West:
                return gamepad.buttonWest;

            case ConfigurableGamepadButton.East:
                return gamepad.buttonEast;

            case ConfigurableGamepadButton.LeftShoulder:
                return gamepad.leftShoulder;

            case ConfigurableGamepadButton.RightShoulder:
                return gamepad.rightShoulder;

            case ConfigurableGamepadButton.LeftStick:
                return gamepad.leftStickButton;

            case ConfigurableGamepadButton.RightStick:
                return gamepad.rightStickButton;

            case ConfigurableGamepadButton.Start:
                return gamepad.startButton;

            case ConfigurableGamepadButton.Select:
                return gamepad.selectButton;

            case ConfigurableGamepadButton.DpadUp:
                return gamepad.dpad.up;

            case ConfigurableGamepadButton.DpadDown:
                return gamepad.dpad.down;

            case ConfigurableGamepadButton.DpadLeft:
                return gamepad.dpad.left;

            case ConfigurableGamepadButton.DpadRight:
                return gamepad.dpad.right;

            default:
                return null;
        }
    }
}