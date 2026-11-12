using UnityEngine;

/// <summary>
/// ワールドマップを走る乗り物（今は仮の絵）
/// 入力は WorldMapController が読み、ここでは移動と見た目の向きだけを行う
/// 山・水域・崖・建物には Collider2D があり、物理で通れないようにしている
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WorldMapVehicle : MonoBehaviour
{
    [Header("── 移動 ──────────────────────")]
    public float moveSpeed = 5f;          // 1秒あたりのユニット数
    public float acceleration = 40f;      // 速度の追従の速さ

    [Header("── 見た目 ──────────────────────")]
    [Tooltip("進行方向へ向ける見た目")]
    public Transform body;
    [Tooltip("ON：進行方向へ回す（上から見た乗り物）／OFF：左右反転だけ（横から見たキャラ）")]
    public bool rotateToFace = true;
    public float turnSpeed = 720f;        // 1秒あたりの回転角
    [Tooltip("走っている間の上下の揺れ")]
    public float bobHeight = 0.04f;
    public float bobSpeed = 18f;

    Rigidbody2D rb;
    Vector2 input;
    float facingAngle = -90f; // 下向き（画面手前）で開始
    float bobTime;
    Vector3 bodyBasePosition;

    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (body != null) bodyBasePosition = body.localPosition;
    }

    /// <summary>
    /// 移動入力（長さ1以下）。0 を渡すと止まる
    /// </summary>
    public void SetInput(Vector2 direction)
    {
        input = Vector2.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// 指定位置へ瞬間移動する（復帰位置に置くとき）
    /// </summary>
    public void Teleport(Vector2 position, Vector2 facing)
    {
        input = Vector2.zero;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.position = position;
        if (facing != Vector2.zero) facingAngle = SnapAngle(facing);
        ApplyBodyRotation(true);
    }

    void FixedUpdate()
    {
        Vector2 target = input * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (input.sqrMagnitude > 0.01f)
        {
            facingAngle = SnapAngle(input);
            bobTime += Time.deltaTime * bobSpeed;
        }
        else
        {
            bobTime = 0f;
        }

        ApplyBodyRotation(false);

        if (body != null)
            body.localPosition = bodyBasePosition + Vector3.up * Mathf.Abs(Mathf.Sin(bobTime)) * bobHeight;
    }

    bool facingRight = true;

    void ApplyBodyRotation(bool immediate)
    {
        if (body == null) return;

        if (!rotateToFace)
        {
            // 横向きの絵は左右反転だけ（真上・真下へ動くときは向きを保つ）
            float x = Mathf.Cos(facingAngle * Mathf.Deg2Rad);
            if (x > 0.1f) facingRight = true;
            else if (x < -0.1f) facingRight = false;
            var s = body.localScale;
            s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
            body.localScale = s;
            return;
        }

        // 絵は右向きで作っているので、そのまま角度を当てる
        var target = Quaternion.Euler(0f, 0f, facingAngle);
        body.localRotation = immediate
            ? target
            : Quaternion.RotateTowards(body.localRotation, target, turnSpeed * Time.deltaTime);
    }

    // 8方向に丸めた角度
    static float SnapAngle(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Mathf.Round(angle / 45f) * 45f;
    }
}
