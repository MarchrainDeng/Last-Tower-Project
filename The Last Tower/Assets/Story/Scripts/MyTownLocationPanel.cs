using TMPro;
using UnityEngine;

/// <summary>
/// 場所を選択すると表示する半透明の黒いUI
/// 今は場所名と「戻る」だけ。場所ごとの機能はここに載せていく
/// </summary>
public class MyTownLocationPanel : MonoBehaviour
{
    public TMP_Text titleLabel;

    public bool IsOpen => gameObject.activeSelf;

    public void Show(string locationName)
    {
        if (titleLabel != null) titleLabel.text = locationName;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
