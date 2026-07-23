using UnityEngine;

public class TestFall : MonoBehaviour
{
    [Header("Fall Settings")]
    // 下落速度（单位：Unity单位/秒）
    // 落下速度（単位：Unityユニット/秒）
    public float fallSpeed = 2f;

    private void Update()
    {
        // 以固定速度向下移动
        // 一定速度で下方向へ移動する
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }
}
