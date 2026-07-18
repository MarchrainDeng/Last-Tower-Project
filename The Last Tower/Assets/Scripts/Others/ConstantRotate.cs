using UnityEngine;

public class ConstantRotate : MonoBehaviour
{
    [Header("Rotate Settings")]

    // 每秒旋转角度（度）
    // 1秒あたりの回転角度（度）
    public float rotateSpeed = 90f;

    // 旋转轴
    // 回転軸
    public Vector3 rotateAxis = Vector3.forward;

    private void Update()
    {
        transform.Rotate(
            rotateAxis,
            rotateSpeed * Time.deltaTime,
            Space.Self
        );
    }
}
