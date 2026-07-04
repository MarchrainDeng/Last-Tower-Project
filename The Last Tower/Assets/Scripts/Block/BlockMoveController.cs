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

---------------------------------------
*/

public class BlockMoveController : MonoBehaviour
{
    [Header("Move")]
    public float moveDistance = 0.5f;   // 每次移动一格的距离
    public float stickThreshold = 0.5f; // 摇杆触发阈值

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
        float x = gamepad.leftStick.x.ReadValue();

        // 摇杆回到中间后，才允许下一次移动
        if (Mathf.Abs(x) < 0.2f)
        {
            stickReturnedToCenter = true;
            return;
        }

        // 持续推着摇杆时，不重复移动
        if (!stickReturnedToCenter)
            return;

        if (x > stickThreshold)
        {
            MoveRight();
            stickReturnedToCenter = false;
        }
        else if (x < -stickThreshold)
        {
            MoveLeft();
            stickReturnedToCenter = false;
        }
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
    }

    private void MoveLeft()
    {
        transform.position += Vector3.left * moveDistance;
    }

    private void MoveRight()
    {
        transform.position += Vector3.right * moveDistance;
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

        if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
        {
            currentSpeed *= fastFallMultiplier;
        }

        transform.position += Vector3.down * currentSpeed * Time.deltaTime;
    }
}
