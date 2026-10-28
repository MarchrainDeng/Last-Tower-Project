using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// さかな図鑑
/// 種類ごとに、釣ったことがあれば名前・最大サイズ・釣った数・説明を、無ければ「？？？」を出す
/// カードは開くたびに作り直す（10枚なので十分軽い）
/// </summary>
public class FishingLogPanel : MonoBehaviour
{
    public TMP_Text title;
    public RectTransform grid;
    public Sprite circle;
    public TMP_FontAsset font;

    public int columns = 5;
    public Vector2 cardSize = new Vector2(320f, 330f);
    public float spacing = 24f;

    public bool IsOpen => gameObject.activeSelf;

    public void Open(FishSpecies[] species)
    {
        gameObject.SetActive(true);
        for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);

        int caught = 0;
        int rows = Mathf.CeilToInt(species.Length / (float)columns);
        float width = columns * cardSize.x + (columns - 1) * spacing;
        float height = rows * cardSize.y + (rows - 1) * spacing;

        for (int i = 0; i < species.Length; i++)
        {
            int col = i % columns, row = i / columns;
            var pos = new Vector2(-width * 0.5f + cardSize.x * 0.5f + col * (cardSize.x + spacing),
                                   height * 0.5f - cardSize.y * 0.5f - row * (cardSize.y + spacing));
            if (MakeCard(species[i], i + 1, pos)) caught++;
        }

        if (title != null) title.text = "さかな図鑑  " + caught + " / " + species.Length;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    bool MakeCard(FishSpecies s, int number, Vector2 position)
    {
        bool known = FishingRecord.IsCaught(s.id);

        var card = Image(grid, "Card_" + s.id, null, position, cardSize, new Color(0.10f, 0.12f, 0.18f, 0.95f));

        // 魚の絵（楕円＋しっぽ）。知らない魚はシルエット
        Color fishColor = known ? s.color : new Color(0.05f, 0.05f, 0.08f);
        float len = Mathf.Lerp(90f, 230f, Mathf.InverseLerp(60f, 220f, s.shadowLength));
        Image(card, "Tail", circle, new Vector2(-len * 0.45f, 70f), new Vector2(len * 0.3f, len * s.shadowWidthRate * 0.8f), fishColor);
        Image(card, "Fish", circle, new Vector2(0f, 70f), new Vector2(len, len * s.shadowWidthRate), fishColor);
        if (known) Image(card, "Eye", circle, new Vector2(len * 0.32f, 74f), new Vector2(8f, 8f), Color.black);

        Text(card, "No", "No." + number.ToString("00"), new Vector2(-cardSize.x * 0.5f + 50f, cardSize.y * 0.5f - 26f), 22f, new Color(0.6f, 0.65f, 0.75f));
        Text(card, "Name", known ? s.displayName : "？？？", new Vector2(0f, -10f), 36f, Color.white);

        if (known)
        {
            Text(card, "Best", "最大 " + FishingRecord.BestSize(s.id).ToString("0.0") + " cm", new Vector2(0f, -52f), 24f, new Color(1f, 0.85f, 0.4f));
            Text(card, "Count", FishingRecord.Count(s.id) + " 匹", new Vector2(0f, -84f), 22f, new Color(0.75f, 0.8f, 0.9f));
            var desc = Text(card, "Desc", s.description, new Vector2(0f, -128f), 18f, new Color(0.7f, 0.72f, 0.8f));
            desc.rectTransform.sizeDelta = new Vector2(cardSize.x - 30f, 60f);
            desc.textWrappingMode = TextWrappingModes.Normal;
        }
        else
        {
            Text(card, "Hint", "影の大きさ：" + FishSpecies.SizeClass(s.shadowLength), new Vector2(0f, -60f), 22f, new Color(0.5f, 0.55f, 0.65f));
        }
        return known;
    }

    static RectTransform Image(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    TMP_Text Text(RectTransform parent, string name, string text, Vector2 position, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(cardSize.x - 20f, size * 1.5f);
        rt.anchoredPosition = position;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        return t;
    }
}
