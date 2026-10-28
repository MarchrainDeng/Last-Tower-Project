using System;
using UnityEngine;

/// <summary>
/// 魚1種類分のデータ
/// 見た目（影の大きさ）、釣れやすさ、食いつき方、引きの強さをまとめて持つ
/// 初期値は FishSpecies.Defaults()。インスペクターで調整できる
/// </summary>
[Serializable]
public class FishSpecies
{
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("── 見た目 ──────────────────────")]
    [Tooltip("水中の影の長さ（釣り画面の座標）")]
    public float shadowLength = 70f;
    [Tooltip("影の太さ（長さに対する割合）")]
    public float shadowWidthRate = 0.38f;
    public Color color = new Color(0.6f, 0.65f, 0.7f);
    [Tooltip("影がうっすら光る（幻の魚）")]
    public bool glows;

    [Header("── 出現 ──────────────────────")]
    public float minSize = 10f;        // cm
    public float maxSize = 25f;        // cm
    [Tooltip("出現のしやすさ（重み）")]
    public float rarity = 10f;

    [Header("── 食いつき ────────────────────")]
    public float swimSpeed = 90f;
    [Tooltip("警戒心 0〜1。高いほど餌を速く動かすと逃げ、近くに落とすと驚く")]
    [Range(0f, 1f)] public float wariness = 0.3f;
    public int minNibbles = 1;         // 本アタリの前につつく回数
    public int maxNibbles = 3;
    [Tooltip("本アタリから合わせられるまでの猶予（秒）")]
    public float biteWindow = 0.7f;

    [Header("── 引き ──────────────────────")]
    [Tooltip("暴れた時に逃げる速さ")]
    public float strength = 180f;
    [Tooltip("体力（秒）。暴れるたびに減り、減るほどおとなしくなる")]
    public float stamina = 8f;
    [Tooltip("暴れやすさ 0〜1")]
    [Range(0f, 1f)] public float aggression = 0.4f;

    public static FishSpecies[] Defaults()
    {
        return new[]
        {
            Make("funa", "フナ", "どこにでもいる小さな魚。おとなしいので練習にぴったり。",
                 60f, 0.38f, new Color(0.62f, 0.60f, 0.45f), 10f, 25f, 30f,
                 90f, 0.2f, 1, 2, 0.9f, 120f, 5f, 0.2f),
            Make("koi", "コイ", "つついてばかりでなかなか食べない。じっと待つのがコツ。",
                 90f, 0.40f, new Color(0.85f, 0.45f, 0.25f), 30f, 70f, 20f,
                 70f, 0.35f, 2, 4, 0.7f, 180f, 8f, 0.3f),
            Make("namazu", "ナマズ", "あまり暴れないが、引きはずっしり重い。",
                 110f, 0.36f, new Color(0.35f, 0.33f, 0.28f), 40f, 90f, 12f,
                 60f, 0.15f, 1, 2, 0.8f, 220f, 10f, 0.35f),
            Make("bass", "ブラックバス", "餌を見つけると一気に食いつく。合わせは素早く。",
                 85f, 0.40f, new Color(0.35f, 0.45f, 0.25f), 25f, 55f, 15f,
                 140f, 0.4f, 0, 1, 0.5f, 230f, 7f, 0.6f),
            Make("trout", "ニジマス", "警戒心が強い。餌はそっと動かして。",
                 75f, 0.34f, new Color(0.75f, 0.55f, 0.65f), 25f, 50f, 14f,
                 160f, 0.55f, 1, 3, 0.55f, 200f, 6f, 0.55f),
            Make("unagi", "ウナギ", "細長い影。左右にくねくね暴れ続ける。",
                 120f, 0.16f, new Color(0.30f, 0.32f, 0.38f), 40f, 100f, 8f,
                 100f, 0.5f, 2, 5, 0.5f, 210f, 9f, 0.75f),
            Make("raigyo", "ライギョ", "池の暴れん坊。暴れている時に巻くとすぐ糸が切れる。",
                 120f, 0.32f, new Color(0.30f, 0.38f, 0.30f), 50f, 90f, 6f,
                 120f, 0.6f, 1, 3, 0.45f, 300f, 11f, 0.65f),
            Make("chouzame", "チョウザメ", "大きな影がゆっくり泳ぐ。長期戦になる。",
                 160f, 0.30f, new Color(0.50f, 0.52f, 0.58f), 80f, 180f, 3f,
                 70f, 0.5f, 2, 4, 0.6f, 340f, 16f, 0.45f),
            Make("tsukimi", "ツキミウオ", "月夜にだけ姿を見せる、うっすら光る幻の魚。",
                 70f, 0.36f, new Color(0.80f, 0.90f, 1.00f), 15f, 30f, 2f,
                 200f, 0.85f, 3, 5, 0.3f, 260f, 7f, 0.8f, glows: true),
            Make("nushi", "池のヌシ", "誰も釣り上げたことがないという、池の主。",
                 220f, 0.36f, new Color(0.45f, 0.30f, 0.30f), 150f, 250f, 0.8f,
                 80f, 0.7f, 3, 6, 0.4f, 420f, 22f, 0.7f),
        };
    }

    static FishSpecies Make(string id, string name, string description,
                            float length, float widthRate, Color color, float minSize, float maxSize, float rarity,
                            float swimSpeed, float wariness, int minNibbles, int maxNibbles, float biteWindow,
                            float strength, float stamina, float aggression, bool glows = false)
    {
        return new FishSpecies
        {
            id = id, displayName = name, description = description,
            shadowLength = length, shadowWidthRate = widthRate, color = color, glows = glows,
            minSize = minSize, maxSize = maxSize, rarity = rarity,
            swimSpeed = swimSpeed, wariness = wariness,
            minNibbles = minNibbles, maxNibbles = maxNibbles, biteWindow = biteWindow,
            strength = strength, stamina = stamina, aggression = aggression,
        };
    }

    /// <summary>
    /// 大きさの段階（スキャンで見える目安）
    /// </summary>
    public static string SizeClass(float shadowLength)
    {
        if (shadowLength >= 180f) return "特大";
        if (shadowLength >= 110f) return "大";
        if (shadowLength >= 80f) return "中";
        return "小";
    }
}
