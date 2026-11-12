using UnityEngine;

/// <summary>
/// プレイヤーを追うカメラ。マップの外は映さない
/// </summary>
[RequireComponent(typeof(Camera))]
public class WorldMapCameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("追従の遅れ（小さいほどぴったり）")]
    public float smoothTime = 0.15f;
    [Tooltip("映してよい範囲（ワールド座標）")]
    public Rect bounds = new Rect(-24f, -15f, 48f, 30f);

    Camera cam;
    Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    /// <summary>
    /// 遅れなしでターゲットの位置へ合わせる（シーン開始時・復帰時）
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        transform.position = Clamp(target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, Clamp(target.position), ref velocity, smoothTime);
    }

    Vector3 Clamp(Vector3 p)
    {
        if (cam == null) cam = GetComponent<Camera>();
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float x = bounds.width > halfW * 2f ? Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW) : bounds.center.x;
        float y = bounds.height > halfH * 2f ? Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH) : bounds.center.y;
        return new Vector3(x, y, transform.position.z);
    }
}
