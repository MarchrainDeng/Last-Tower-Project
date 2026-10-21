using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// マイタウンの場所ボタン1つ分
/// マウスのクリックをコントローラーへ中継し、選択状態の見た目を更新する
/// </summary>
public class MyTownLocationButton : MonoBehaviour, IPointerClickHandler
{
    [Header("── 場所 ──────────────────────")]
    public string locationId;
    public string displayName;
    [Tooltip("ストーリー進行で解放されるまで使えない場所は OFF")]
    public bool unlockedByDefault = true;

    [Header("── 表示 ──────────────────────")]
    public Image background;
    public TMP_Text label;
    public Color normalColor = new Color(0.20f, 0.22f, 0.30f, 0.75f);
    public Color selectedColor = new Color(0.95f, 0.75f, 0.30f, 1f);
    public Color lockedColor = new Color(0.10f, 0.10f, 0.12f, 0.45f);
    public float selectedScale = 1.15f;

    MyTownController controller;
    int index;

    public bool IsUnlocked { get; private set; }

    public void Init(MyTownController owner, int i)
    {
        controller = owner;
        index = i;
        IsUnlocked = MyTownProgress.IsUnlocked(locationId, unlockedByDefault);
        if (label != null) label.text = IsUnlocked ? displayName : "？？？";
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = !IsUnlocked ? lockedColor : (selected ? selectedColor : normalColor);
        transform.localScale = Vector3.one * (selected && IsUnlocked ? selectedScale : 1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.ClickLocation(index);
    }
}
