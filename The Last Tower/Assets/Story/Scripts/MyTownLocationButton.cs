using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// マイタウンの場所（建物）1つ分
/// マウスのクリックをコントローラーへ中継し、選択状態の見た目を更新する
/// pivotは足元（下端中央）。奥行きに応じた大きさと霧の色は MyTownWorld から決める
/// </summary>
public class MyTownLocationButton : MonoBehaviour, IPointerClickHandler
{
    [Header("── 場所 ──────────────────────")]
    public string locationId;
    public string displayName;
    [Tooltip("ストーリー進行で解放されるまで使えない場所は OFF")]
    public bool unlockedByDefault = true;

    [Header("── 表示 ──────────────────────")]
    public Image background;            // 建物の壁
    public Image[] windows;             // 明かりの灯る窓
    public TMP_Text label;              // 建物の上の看板
    public Color normalColor = new Color(0.20f, 0.22f, 0.30f, 1f);
    public Color selectedColor = new Color(0.95f, 0.75f, 0.30f, 1f);
    public Color lockedColor = new Color(0.10f, 0.10f, 0.12f, 1f);
    public Color windowColor = new Color(0.85f, 0.35f, 0.20f, 1f);
    public Color windowSelectedColor = new Color(1f, 0.85f, 0.45f, 1f);
    public Color labelColor = new Color(0.85f, 0.85f, 0.90f, 1f);
    public Color labelSelectedColor = new Color(1f, 0.85f, 0.40f, 1f);
    public float selectedScale = 1.06f;

    MyTownController controller;
    MyTownWorld world;
    int index;
    float baseScale = 1f;

    public bool IsUnlocked { get; private set; }

    public void Init(MyTownController owner, int i)
    {
        controller = owner;
        index = i;
        world = GetComponentInParent<MyTownWorld>();
        baseScale = world != null ? world.ScaleAt(transform.localPosition.y) : 1f;
        IsUnlocked = MyTownProgress.IsUnlocked(locationId, unlockedByDefault);
        if (label != null) label.text = IsUnlocked ? displayName : "？？？";
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        bool lit = selected && IsUnlocked;

        if (background != null)
            background.color = Fog(!IsUnlocked ? lockedColor : (lit ? selectedColor : normalColor));

        if (windows != null)
        {
            foreach (var w in windows)
                if (w != null) w.color = Fog(!IsUnlocked ? lockedColor : (lit ? windowSelectedColor : windowColor));
        }

        if (label != null)
            label.color = lit ? labelSelectedColor : Fog(labelColor);

        transform.localScale = Vector3.one * baseScale * (lit ? selectedScale : 1f);
    }

    Color Fog(Color color)
    {
        return world != null ? world.ApplyFog(color, transform.localPosition.y) : color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.ClickLocation(index);
    }
}
