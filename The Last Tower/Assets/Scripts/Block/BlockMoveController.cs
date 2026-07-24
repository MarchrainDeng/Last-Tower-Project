using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/*
----------------------------------------
【功能 / 機能】
控制当前下落方块的移动与旋转。
左摇杆控制左右移动（一次移动一格），
LB 逆时针旋转，RB 顺时针旋转。

現在落下中のブロックの移動・回転を制御する。
左スティックで左右に1マス移動し、
LBで反時計回り、RBで時計回りに回転する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/03

【更新/更新】
2026/07/03：
追加了加速下落功能，相关函数：HandleFall()
加速下降機能を追加しました。関連する関数：HandleFall()

2026/07/07:
修改了移动方式。由离散的固定距离移动改为了连续移动。相关函数：HandleMove()
移動方法を変更しました。離散的な固定距離の移動から、連続的な移動へと変更しました。関連関数：HandleMove()

2026/07/09：
增加了键盘操作
キーボード操作を追加しました。

2026/07/11
避免了选择方块卡牌后会直接处于高速下落状态的问题，相关函数：HandleFall()
ブロックカードを選択した後に直接高速落下状態になる問題を回避しました。関連関数：HandleFall()

2026/07/12
避免了让方块直接掉出地图时，无法继续选择方块的问题，相关函数：SetFlowManager(),HandleDeadZone()
ブロックがマップから直接落ちた際に選択を続けることができなくなる問題を回避しました。関連する関数：SetFlowManager()、HandleDeadZone()

---------------------------------------
*/

public class BlockMoveController : MonoBehaviour
{
    [Header("Move")]
    public float deadZone = 0.2f;
    public float moveSpeed = 0.01f;

    //public float moveDistance = 0.5f;   // 每次移动一格的距离
    //public float stickThreshold = 0.5f; // 摇杆触发阈值

    [Header("Rotate")]
    public float rotateAngle = 90f;

    // 旋转速度（度/秒）
    // 回転速度（度/秒）
    public float rotateSpeed = 180f;

    [Header("Fall Settings")]
    // 下落速度（单位：Unity单位/秒）
    // 落下速度（単位：Unityユニット/秒）
    public float fallSpeed = 2f;

    // 加速下落倍率
    // 高速落下倍率
    public float fastFallMultiplier = 3f;

    private Gamepad gamepad;

    private bool stickReturnedToCenter = true;

    // 是否已经等待A键松开
    // Aボタンが一度離されるのを待っているか
    private bool waitingForFastFallRelease = true;

    [Header("Dead Zone")]

    // 越界判定Y坐标
    // 範囲外判定Y座標
    public float deadLineY = -8f;

    // 是否已经触发越界
    // 範囲外処理済みか
    private bool isDead = false;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    private BlockSelectionFlowManager flowManager;

    // 刚体
    // Rigidbody
    private Rigidbody2D rb;

    private float moveInput;

    [Header("Input Settings")]

    // 顺时针旋转按键
    // 時計回り回転ボタン
    [SerializeField]
    private ConfigurableGamepadButton rotateClockwiseButton =
    ConfigurableGamepadButton.East;

    /// <summary>
    /// 可配置的手柄按键
    /// 設定可能なゲームパッドボタン
    /// </summary>
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 作为保险，自动寻找场景中的流程管理器
        // 念のため、シーン内のフローマネージャーを自動検索する
        if (flowManager == null)
        {
            flowManager =
                FindFirstObjectByType<BlockSelectionFlowManager>();
        }

        if (flowManager == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: BlockSelectionFlowManager " +
                "没有找到。请确认场景中存在并处于启用状态。" +
                "\nBlockSelectionFlowManagerが見つかりません。" +
                "シーン内に存在し、有効になっているか確認してください。",
                gameObject
            );
        }
    }

    private void Update()
    {
        gamepad = Gamepad.current;

        //if (gamepad == null)
          //  return;

        // 追加：一時停止中は操作を無効化
        if (GameStateManager.IsPaused)
            return;

        ReadMoveInput();

        HandleRotate();
        HandleDeadZone();
    }

    private void FixedUpdate()
    {
        gamepad = Gamepad.current;

        //if (gamepad == null)
            //return;

        // 追加：一時停止中は操作を無効化
        if (GameStateManager.IsPaused)
            return;

        //HandleMove();
        //HandleFall();
        HandleMovement();
    }

    private void HandleMove()
    {
        // 获取左摇杆水平输入
        // 左スティックの水平入力を取得する
        float input = Gamepad.current.leftStick.x.ReadValue();

        //Debug.Log(input);

        // 键盘输入（A/D）
        // キーボード入力（A/D）
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                input = -1f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                input = 1f;
            }
        }

        // 死区处理
        // デッドゾーン処理
        if (Mathf.Abs(input) < deadZone)
            input = 0f;

        // 平滑移动
        // スムーズに移動する
        Vector2 targetPosition =
            rb.position +
            Vector2.right *
            input *
            moveSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);
    }

    private void HandleRotate()
    {
        if (gamepad.leftShoulder.isPressed)
        {
            SmoothRotateCounterClockwise();
        }

        if (gamepad.rightShoulder.isPressed)
        {
            SmoothRotateClockwise();
        }

        ButtonControl clockwiseButton =
        GetGamepadButton(rotateClockwiseButton);

        // 手柄：顺时针旋转
        // ゲームパッド：時計回りに回転
        if (clockwiseButton != null &&
            clockwiseButton.wasPressedThisFrame)
        {
            RotateClockwise();
        }

        // 键盘 W：顺时针旋转
        // キーボードW：時計回りに回転
        if (Keyboard.current != null &&
            Keyboard.current.wKey.wasPressedThisFrame)
        {
            RotateClockwise();
        }
    }

    private void MoveLeft()
    {
        //transform.position += Vector3.left * moveDistance;
    }

    private void MoveRight()
    {
        //transform.position += Vector3.right * moveDistance;
    }

    private void RotateClockwise()
    {
        transform.Rotate(0, 0, -rotateAngle);
    }

    private void RotateCounterClockwise()
    {
        transform.Rotate(0, 0, rotateAngle);
    }

    /// <summary>
    /// 顺时针持续旋转
    /// 時計回りに連続回転
    /// </summary>
    private void SmoothRotateClockwise()
    {
        transform.Rotate(
            0,
            0,
            -rotateSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 逆时针持续旋转
    /// 反時計回りに連続回転
    /// </summary>
    private void SmoothRotateCounterClockwise()
    {
        transform.Rotate(
            0,
            0,
            rotateSpeed * Time.deltaTime
        );
    }

    private void HandleFall()
    {
        float currentSpeed = fallSpeed;

        bool xPressed =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.isPressed;

        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed;

        // 新方块生成后，必须先松开A键
        // 新しいブロック生成後は、一度Aボタンを離す必要がある
        if (waitingForFastFallRelease)
        {
            if (!xPressed)
            {
                waitingForFastFallRelease = false;
            }
        }
        else
        {
            // 松开后再次按住A，才允许快速下落
            // 一度離した後、再度Aを押している間のみ高速落下する
            if (xPressed || spacePressed)
            {
                currentSpeed *= fastFallMultiplier;
            }
        }

        //transform.position += Vector3.down * currentSpeed * Time.deltaTime;

        Vector2 targetPosition =
            rb.position +
            Vector2.down *
            currentSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);
    }

    /// <summary>
    /// 设置流程管理器
    /// フローマネージャーを設定する
    /// </summary>
    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }

    /// <summary>
    /// 检测是否进入死亡区域
    /// デッドゾーンに入ったか判定する
    /// </summary>
    private void HandleDeadZone()
    {
        if (isDead)
            return;

        if (transform.position.y < deadLineY)
        {
            isDead = true;

            // 通知流程管理器开始下一轮选择
            // フローマネージャーへ次の選択開始を通知する
            if (flowManager != null)
            {
                Debug.Log("next selection");
                flowManager.OnCurrentBlockLanded();
            }

            Destroy(gameObject);
        }
    }

    private void ReadMoveInput()
    {
        moveInput = 0f;

        if (Gamepad.current != null)
        {
            moveInput =
                Gamepad.current.leftStick.x.ReadValue();
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
                moveInput = -1f;
            else if (Keyboard.current.dKey.isPressed)
                moveInput = 1f;
        }

        if (Mathf.Abs(moveInput) < deadZone)
            moveInput = 0f;
    }

    /// <summary>
    /// 处理方块移动（左右移动+下落）
    /// ブロックの移動（左右移動＋落下）
    /// </summary>
    private void HandleMovement()
    {
        float currentFallSpeed = fallSpeed;

        bool xPressed =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.isPressed;

        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed;

        // 新方块生成后，必须先松开A键
        // 新しいブロック生成後は、一度Aボタンを離す必要がある
        if (waitingForFastFallRelease)
        {
            if (!xPressed)
            {
                waitingForFastFallRelease = false;
            }
        }
        else
        {
            // 松开后再次按住A，才允许高速下落
            // 一度離した後、再度Aを押した時のみ高速落下
            if (xPressed || spacePressed)
            {
                currentFallSpeed *= fastFallMultiplier;
            }
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
    /// 根据配置获取手柄按键
    /// 設定に応じたゲームパッドボタンを取得する
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
