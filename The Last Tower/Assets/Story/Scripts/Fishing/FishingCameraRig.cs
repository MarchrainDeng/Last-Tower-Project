using UnityEngine;

/// <summary>
/// 釣り画面の「カメラ」
/// UIなので本物のカメラは動かさず、中身（root）を拡大・移動して寄り引きを表現する
///
/// - Look    : 注目する位置と倍率をなめらかに追う
/// - Punch   : 一瞬だけグッと寄る
/// - Shake   : 揺れ（トラウマ値。重ねがけすると大きくなり、時間で収まる）
/// - SlowMo  : 一瞬スローにする（釣りの処理は TimeScale を掛けた時間で進める）
/// </summary>
public class FishingCameraRig : MonoBehaviour
{
    public RectTransform root;
    public float followTime = 0.35f;
    public float maxShake = 22f;
    public float maxShakeAngle = 2.5f;

    Vector2 focus, focusTarget, focusVelocity;
    float zoom = 1f, zoomTarget = 1f, zoomVelocity;
    float punch, trauma;
    float slowScale = 1f, slowTimer;
    float followOverride = -1f;

    public float TimeScale => slowTimer > 0f ? slowScale : 1f;
    public Vector2 Focus => focus;
    public float Zoom => zoom * (1f + punch);

    /// <summary>
    /// 注目点と倍率を指定する。followTime を渡すとその速さで追う（次の Look まで）
    /// </summary>
    public void Look(Vector2 target, float targetZoom, float time = -1f)
    {
        focusTarget = target;
        zoomTarget = targetZoom;
        followOverride = time;
    }

    /// <summary>
    /// 補間せずに即座に移動する
    /// </summary>
    public void Snap(Vector2 target, float targetZoom)
    {
        focus = focusTarget = target;
        zoom = zoomTarget = targetZoom;
        focusVelocity = Vector2.zero;
        zoomVelocity = 0f;
        Apply();
    }

    public void Punch(float amount)
    {
        punch = Mathf.Max(punch, amount);
    }

    public void Shake(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void SlowMo(float scale, float duration)
    {
        slowScale = scale;
        slowTimer = duration;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float follow = followOverride > 0f ? followOverride : followTime;

        focus = Vector2.SmoothDamp(focus, focusTarget, ref focusVelocity, follow, Mathf.Infinity, dt);
        zoom = Mathf.SmoothDamp(zoom, zoomTarget, ref zoomVelocity, follow, Mathf.Infinity, dt);
        punch = Mathf.MoveTowards(punch, 0f, dt * 1.2f) * Mathf.Exp(-dt * 6f);
        trauma = Mathf.MoveTowards(trauma, 0f, dt * 1.1f);
        if (slowTimer > 0f) slowTimer -= dt;

        Apply();
    }

    void Apply()
    {
        if (root == null) return;

        float z = Zoom;
        // 揺れはトラウマの2乗（小さい揺れは控えめ、大きい揺れは派手に）
        float shake = trauma * trauma;
        float t = Time.unscaledTime * 25f;
        Vector2 offset = new Vector2(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * maxShake * shake;
        float angle = (Mathf.PerlinNoise(t, 5.1f) - 0.5f) * 2f * maxShakeAngle * shake;

        root.localScale = Vector3.one * z;
        root.anchoredPosition = -focus * z + offset;
        root.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
