using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Update()
    {
        gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        HandleMove();
        HandleRotate();
        
    }

    private void FixedUpdate()
    {
        gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        HandleFall();
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
        transform.position +=
            Vector3.right *
            input *
            moveSpeed *
            Time.deltaTime;
    }

    private void HandleRotate()
    {
        if (gamepad.leftShoulder.wasPressedThisFrame)
        {
            RotateCounterClockwise();
        }

        if (gamepad.rightShoulder.wasPressedThisFrame)
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

    private void HandleFall()
    {
        float currentSpeed = fallSpeed;

        bool aPressed =
            Gamepad.current != null &&
            Gamepad.current.buttonSouth.isPressed;

        bool spacePressed =
            Keyboard.current != null &&
            Keyboard.current.spaceKey.isPressed;

        // 新方块生成后，必须先松开A键
        // 新しいブロック生成後は、一度Aボタンを離す必要がある
        if (waitingForFastFallRelease)
        {
            if (!aPressed)
            {
                waitingForFastFallRelease = false;
            }
        }
        else
        {
            // 松开后再次按住A，才允许快速下落
            // 一度離した後、再度Aを押している間のみ高速落下する
            if (aPressed || spacePressed)
            {
                currentSpeed *= fastFallMultiplier;
            }
        }

        transform.position += Vector3.down * currentSpeed * Time.deltaTime;
    }
}
