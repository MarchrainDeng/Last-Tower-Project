using TMPro;
using UnityEngine;

/// <summary>
/// ワールドマップ上で「近づいて決定できる場所」の共通部分（台座・マイタウン出入口）
/// 近づくと発光し、頭上に簡易情報の看板を出す。離れると両方消える
/// </summary>
public abstract class WorldMapSpot : MonoBehaviour
{
    [Header("── 接近 ──────────────────────")]
    [Tooltip("この距離以内に近づくと簡易情報を出す（ワールド座標）")]
    public float approachRadius = 2.2f;
    [Tooltip("復帰するときに乗り物を置く位置（この場所からの相対位置）")]
    public Vector2 frontOffset = new Vector2(0f, -1.8f);

    [Header("── 表示 ──────────────────────")]
    [Tooltip("接近したときに光る輪")]
    public SpriteRenderer glow;
    public Color glowColor = new Color(1f, 0.9f, 0.45f, 0.75f);
    public float glowPulseSpeed = 4f;
    [Tooltip("頭上の看板（簡易情報）")]
    public GameObject signRoot;
    public TMP_Text signText;

    public abstract string SpotId { get; }

    /// <summary>決定で入れるか（未解放の台座は false）</summary>
    public abstract bool CanEnter { get; }

    public bool IsNear { get; private set; }

    public Vector2 Position => transform.position;
    public Vector2 FrontPosition => (Vector2)transform.position + frontOffset;

    protected virtual void Start()
    {
        Refresh();
        ApplyNear();
    }

    public void SetNear(bool near)
    {
        if (IsNear == near) return;
        IsNear = near;
        ApplyNear();
    }

    /// <summary>
    /// 保存状況から見た目を作り直す
    /// </summary>
    public virtual void Refresh()
    {
        if (signText != null) signText.text = BuildSignText();
    }

    protected abstract string BuildSignText();

    void ApplyNear()
    {
        if (signRoot != null) signRoot.SetActive(IsNear);
        if (glow != null) glow.enabled = IsNear && CanEnter;
    }

    protected virtual void Update()
    {
        if (glow == null || !glow.enabled) return;
        // ゆっくり明滅させる
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed);
        var c = glowColor;
        c.a *= Mathf.Lerp(0.45f, 1f, t);
        glow.color = c;
        glow.transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1.05f, t) * glowBaseScale;
    }

    float glowBaseScale = 1f;

    protected virtual void Awake()
    {
        if (glow != null) glowBaseScale = glow.transform.localScale.x;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, approachRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(FrontPosition, 0.3f);
    }
}
