using TMPro;
using UnityEngine;

/// <summary>
/// ワールド選択マップ上の1ワールド（名所の絵＋名札）
/// - 未解放：絵を暗くして雲をかぶせ、錠前を出す。カーソルは乗せられるが入れない
/// - 解放済み：ふつうに表示
/// - クリア済み：旗を立てる
/// カーソルが乗っている間は後ろの集中線を明滅させる
/// </summary>
public class WorldSelectNode : MonoBehaviour
{
    public WorldData world;

    [Header("── 見た目 ──────────────────────")]
    [Tooltip("未解放のとき暗くする絵")]
    public SpriteRenderer[] artRenderers;
    public Color lockedTint = new Color(0.62f, 0.62f, 0.62f, 1f);
    public GameObject lockIcon;
    [Tooltip("未解放のときにかぶせる雲")]
    public GameObject fog;
    [Tooltip("クリア済みの旗")]
    public GameObject clearedMark;
    [Tooltip("名札の文字")]
    public TMP_Text label;
    [Tooltip("名札（選択中は少し大きくする）")]
    public Transform labelRoot;

    [Header("── 選択 ──────────────────────")]
    [Tooltip("選択中に光る集中線")]
    public SpriteRenderer glow;
    public Color glowColor = new Color(0.11f, 0.10f, 0.10f, 1f);
    public float glowPulseSpeed = 4f;
    [Tooltip("カーソル（上向きの矢印の先端）を出す位置（このワールドの足元からの相対位置）")]
    public Vector2 cursorOffset = new Vector2(0f, -1.5f);
    [Tooltip("マウスで選ぶ範囲")]
    public Collider2D pickArea;

    public bool IsSelected { get; private set; }
    public bool IsUnlocked => WorldMapProgress.IsWorldUnlocked(world);
    public bool IsCleared => WorldMapProgress.IsWorldCleared(world);
    public string WorldId => world != null ? world.worldId : name;

    public Vector2 Position => transform.position;
    public Vector2 CursorPosition => (Vector2)transform.position + cursorOffset;

    Color[] baseColors;
    float glowBaseScale = 1f;
    Vector3 labelBaseScale = Vector3.one;

    void Awake()
    {
        if (glow != null) glowBaseScale = glow.transform.localScale.x;
        if (labelRoot != null) labelBaseScale = labelRoot.localScale;
        if (artRenderers != null)
        {
            baseColors = new Color[artRenderers.Length];
            for (int i = 0; i < artRenderers.Length; i++)
                if (artRenderers[i] != null) baseColors[i] = artRenderers[i].color;
        }
    }

    void Start()
    {
        Refresh();
        ApplySelected();
    }

    /// <summary>保存状況から見た目を作り直す</summary>
    public void Refresh()
    {
        bool unlocked = IsUnlocked;
        bool cleared = IsCleared;

        if (artRenderers != null && baseColors != null)
            for (int i = 0; i < artRenderers.Length; i++)
                if (artRenderers[i] != null)
                    artRenderers[i].color = unlocked ? baseColors[i] : baseColors[i] * lockedTint;

        if (lockIcon != null) lockIcon.SetActive(!unlocked);
        if (fog != null) fog.SetActive(!unlocked);
        if (clearedMark != null) clearedMark.SetActive(cleared);

        if (label != null && world != null)
            label.text = "<size=70%>" + world.NumberLabel + "</size>\n" + world.worldName;
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected == selected) return;
        IsSelected = selected;
        ApplySelected();
    }

    void ApplySelected()
    {
        if (glow != null) glow.enabled = IsSelected;
        if (labelRoot != null) labelRoot.localScale = labelBaseScale * (IsSelected ? 1.15f : 1f);
    }

    /// <summary>この点（ワールド座標）がマウスで選ぶ範囲に入っているか</summary>
    public bool Contains(Vector2 worldPoint)
    {
        return pickArea != null && pickArea.OverlapPoint(worldPoint);
    }

    void Update()
    {
        if (glow == null || !glow.enabled) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * glowPulseSpeed);
        var c = glowColor;
        c.a *= Mathf.Lerp(0.45f, 1f, t);
        glow.color = c;
        glow.transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1.05f, t) * glowBaseScale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CursorPosition, 0.3f);
    }
}
