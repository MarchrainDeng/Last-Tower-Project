using UnityEngine;
using UnityEngine.InputSystem;

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

    private Gamepad gamepad;
    private Rigidbody2D rb;

    private float moveInput;
    private bool fastFallInput;
    private bool waitingForFastFallRelease = true;
    private bool isDead = false;

    private BlockSelectionFlowManager flowManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

    private void Start()
    {
        // 新生成的方块自动登记到当前场景的教学
        // 新しく生成されたブロックをチュートリアルへ登録する
        BlockOperationTutorial.Instance?.RegisterBlock(this);
    }

    private void Update()
    {
        gamepad = Gamepad.current;

        // 暂停或对话期间禁止操作
        // ポーズ中または会話中は操作を禁止する
        if (GameStateManager.IsPaused ||
            DialogueManager.IsInputBlocked)
        {
            moveInput = 0f;
            fastFallInput = false;

            // 教学中，对话后需要先松开快速下落键
            // チュートリアル中は会話後に高速落下ボタンを離す必要がある
            if (BlockOperationTutorial.Instance != null)
            {
                waitingForFastFallRelease = true;
            }

            return;
        }

        ReadMoveInput();
        ReadFallInput();
        HandleRotate();
        HandleDeadZone();
    }

    private void FixedUpdate()
    {
        if (GameStateManager.IsPaused ||
            DialogueManager.IsInputBlocked)
        {
            return;
        }

        if (isDead)
            return;

        HandleMovement();
    }

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

        // 摇杆死区处理
        // スティックのデッドゾーン処理
        if (Mathf.Abs(moveInput) < deadZone)
        {
            moveInput = 0f;
        }
    }

    private void ReadFallInput()
    {
        // 手柄A键
        // ゲームパッドAボタン
        bool gamepadPressed =
            gamepad != null &&
            gamepad.buttonSouth.isPressed;

        // 键盘S键
        // キーボードSキー
        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.sKey.isPressed;

        bool isPressed = gamepadPressed || keyboardPressed;

        // 新方块生成后，先等待快速下落键松开
        // 新しいブロック生成後は高速落下ボタンが離されるのを待つ
        if (waitingForFastFallRelease)
        {
            if (!isPressed)
            {
                waitingForFastFallRelease = false;
            }

            fastFallInput = false;
            return;
        }

        fastFallInput = isPressed;
    }

    private void HandleMovement()
    {
        float currentFallSpeed = fallSpeed;

        if (fastFallInput)
        {
            currentFallSpeed *= fastFallMultiplier;
        }

        Vector2 movement = new Vector2(
            moveInput * moveSpeed,
            -currentFallSpeed
        );

        // 记录输入对应的实际位移
        // 入力に対応する実移動量を記録する
        BlockOperationTutorial.Instance?.RecordMovementStep(
            this,
            rb,
            moveInput,
            fastFallInput &&
            fastFallMultiplier > 1f &&
            fallSpeed > 0f
        );

        rb.MovePosition(
            rb.position +
            movement * Time.fixedDeltaTime
        );
    }

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

        // 只按一个方向时执行平滑旋转
        // 一方向のみ押されている場合にスムーズ回転する
        if (smoothCounterClockwise && !smoothClockwise)
        {
            SmoothRotateCounterClockwise();
        }
        else if (smoothClockwise && !smoothCounterClockwise)
        {
            SmoothRotateClockwise();
        }

        // 手柄Y键：固定为顺时针90度旋转
        // ゲームパッドYボタン：時計回り90度回転に固定
        bool gamepadRotatePressed =
            gamepad != null &&
            gamepad.buttonNorth.wasPressedThisFrame;

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

    private void RotateClockwise()
    {
        float previousAngle = transform.eulerAngles.z;

        // 保留原有的90度档位旋转规则
        // 元の90度刻みの回転処理を維持する
        float currentAngle = transform.eulerAngles.z;

        currentAngle =
            Mathf.Round(currentAngle * 100f) / 100f;

        float targetAngle =
            Mathf.Ceil(currentAngle / 90f) * 90f - 90f;

        targetAngle =
            Mathf.Repeat(targetAngle, 360f);

        transform.rotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        // 离散旋转单独记录，不计入平滑旋转
        // 段階回転を個別に記録し、スムーズ回転には含めない
        BlockOperationTutorial.Instance?.RecordSnapRotation(
            this,
            Mathf.DeltaAngle(
                previousAngle,
                transform.eulerAngles.z
            )
        );
    }

    private void SmoothRotateClockwise()
    {
        float previousAngle = transform.eulerAngles.z;

        transform.Rotate(
            0f,
            0f,
            -rotateSpeed * Time.deltaTime
        );

        // 记录平滑旋转实际产生的角度
        // スムーズ回転で実際に発生した角度を記録する
        BlockOperationTutorial.Instance?.RecordSmoothRotation(
            this,
            Mathf.DeltaAngle(
                previousAngle,
                transform.eulerAngles.z
            )
        );
    }

    private void SmoothRotateCounterClockwise()
    {
        float previousAngle = transform.eulerAngles.z;

        transform.Rotate(
            0f,
            0f,
            rotateSpeed * Time.deltaTime
        );

        // 记录平滑旋转实际产生的角度
        // スムーズ回転で実際に発生した角度を記録する
        BlockOperationTutorial.Instance?.RecordSmoothRotation(
            this,
            Mathf.DeltaAngle(
                previousAngle,
                transform.eulerAngles.z
            )
        );
    }

    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }

    private void HandleDeadZone()
    {
        if (isDead)
            return;

        if (transform.position.y < deadLineY)
        {
            isDead = true;

            // 教学期间越界，由教学统一销毁、清零并重试
            // チュートリアル中の範囲外は削除・リセット・再試行する
            if (BlockOperationTutorial.Instance != null &&
                BlockOperationTutorial.Instance.TryHandleLostBlock(this))
            {
                return;
            }

            if (flowManager != null)
            {
                Debug.Log("next selection");
                flowManager.OnCurrentBlockLanded();
            }

            Destroy(gameObject);
        }
    }
}