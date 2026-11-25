using System.Collections.Generic;
using UnityEngine;
using static InkCanvas;
using Art = WorldMapInkArt;

/// <summary>
/// ワールド選択マップの絵（白黒のペン画風）をコードで描く。ステージマップ（WorldMapInkArt）と同じ線と色
/// 各ワールドの名所：ラボ／ロンドン（時計塔と工場）／地中海（港町と灯台）／中東（宮殿）／日本（五重塔）／アマゾン（密林の遺跡）／NEO（未来都市）
/// 各関数は PNG を書き出して Sprite を返す（pivot は足元）
/// </summary>
public static class WorldSelectInkArt
{
    static Color InkC => Art.InkC;
    static Color Paper => Art.Paper;
    static Color Light => Art.Light;
    static Color Shade => Art.Shade;
    static Color Dark => Art.Dark;
    static readonly Color Clear = new Color(0.97f, 0.96f, 0.93f, 0f);

    const float LW = 0.04f;
    const float Thin = 0.022f;
    const int Ppu = 96;

    static float R(System.Random rnd, float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    static void Windows(InkCanvas c, float x0, float y, float step, int count, float w, float h)
    {
        for (int i = 0; i < count; i++)
            c.Shape(RoundRect(x0 + i * step, y, x0 + i * step + w, y + h, Mathf.Min(w, h) * 0.45f), Dark, Thin, InkC);
    }

    static void ArchDoor(InkCanvas c, float cx, float y0, float hw, float h, Color fill)
    {
        var door = new List<Vector2>(Ellipse(new Vector2(cx, y0 + h - hw), hw, hw, 14, 0f, 180f));
        door.Add(new Vector2(cx - hw, y0));
        door.Insert(0, new Vector2(cx + hw, y0));
        c.Shape(door, fill, Thin * 1.3f, InkC);
    }

    static void Box(InkCanvas c, float x0, float y0, float x1, float y1, float shadeFrom = 0.75f)
    {
        c.Shape(Rect(x0, y0, x1, y1), Paper, LW, InkC);
        c.Hatch(Rect(Mathf.Lerp(x0, x1, shadeFrom), y0, x1, y1), 80f, 0.08f, Thin, InkC);
        c.Stroke(Rect(x0, y0, x1, y1), LW, InkC, true);
    }

    // ─── W1 ラボ ─────────────────────────────────────────────────
    public static Sprite MakeLab(string dir)
    {
        var rnd = new System.Random(41);
        float w = 5.2f, h = 4.4f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);

        // 右の塔とアンテナ
        Box(c, 3.7f, y0, 4.5f, y0 + 2.9f, 0.6f);
        Windows(c, 3.92f, y0 + 1.9f, 0.3f, 2, 0.16f, 0.4f);
        c.Line(new Vector2(4.1f, y0 + 2.9f), new Vector2(4.1f, y0 + 4.0f), LW, InkC);
        for (int i = 0; i < 3; i++)
            c.Stroke(Ellipse(new Vector2(4.1f, y0 + 3.2f + i * 0.28f), 0.34f - i * 0.08f, 0.08f, 20), Thin * 1.2f, InkC, true);
        c.Fill(Sparkle(new Vector2(4.1f, y0 + 4.1f), 0.2f), InkC);

        // 左のタンク
        c.Shape(Rect(0.2f, y0, 0.85f, y0 + 1.4f), Paper, LW, InkC);
        c.Shape(Ellipse(new Vector2(0.525f, y0 + 1.4f), 0.325f, 0.1f, 20), Shade, Thin, InkC);
        for (int i = 1; i < 4; i++) c.Line(new Vector2(0.2f, y0 + i * 0.33f), new Vector2(0.85f, y0 + i * 0.33f), Thin, InkC);
        Art.Tube(c, new[] { new Vector2(0.85f, y0 + 1.0f), new Vector2(1.3f, y0 + 1.0f) }, 0.14f);

        // 本館と観測ドーム
        Box(c, 1.1f, y0, 3.8f, y0 + 1.7f);
        var dome = new List<Vector2>(Ellipse(new Vector2(2.1f, y0 + 1.7f), 0.85f, 0.85f, 28, 0f, 180f));
        c.Shape(dome, Paper, LW, InkC);
        c.Hatch(new List<Vector2>(Ellipse(new Vector2(2.1f, y0 + 1.7f), 0.85f, 0.85f, 16, 0f, 60f)) { new Vector2(2.1f, y0 + 1.7f) }, 70f, 0.07f, Thin, InkC);
        c.Shape(Rect(2.0f, y0 + 1.9f, 2.2f, y0 + 2.5f), Dark, Thin, InkC);
        c.Stroke(Ellipse(new Vector2(2.1f, y0 + 1.7f), 0.85f, 0.85f, 28, 0f, 180f), LW, InkC);
        Windows(c, 1.35f, y0 + 0.85f, 0.45f, 5, 0.22f, 0.45f);
        ArchDoor(c, 3.35f, y0, 0.2f, 0.6f, Dark);

        // フラスコの看板
        var flask = new List<Vector2> { new Vector2(1.55f, y0 + 0.15f), new Vector2(2.05f, y0 + 0.15f), new Vector2(1.88f, y0 + 0.45f), new Vector2(1.88f, y0 + 0.62f), new Vector2(1.72f, y0 + 0.62f), new Vector2(1.72f, y0 + 0.45f) };
        c.Shape(flask, Light, Thin * 1.3f, InkC);
        c.Fill(new List<Vector2> { new Vector2(1.6f, y0 + 0.19f), new Vector2(2.0f, y0 + 0.19f), new Vector2(1.92f, y0 + 0.32f), new Vector2(1.68f, y0 + 0.32f) }, InkC);
        Art.Puff(c, new Vector2(4.3f, y0 + 1.2f), 0.35f, rnd);
        return c.Save(dir + "/W1_Lab.png", new Vector2(0.5f, y0 / h));
    }

    // ─── W2 ロンドン ─────────────────────────────────────────────
    public static Sprite MakeLondon(string dir)
    {
        var rnd = new System.Random(42);
        float w = 5.6f, h = 5.0f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);

        // 工場の煙突（奥）と煙
        float[] xs = { 2.9f, 3.8f, 4.7f };
        float[] tops = { 3.1f, 3.5f, 3.0f };
        for (int i = 0; i < xs.Length; i++)
        {
            Box(c, xs[i] - 0.17f, y0 + 1.2f, xs[i] + 0.17f, y0 + tops[i], 0.5f);
            c.Shape(Rect(xs[i] - 0.23f, y0 + tops[i] - 0.12f, xs[i] + 0.23f, y0 + tops[i]), Shade, Thin, InkC);
        }
        for (int i = 0; i < xs.Length; i++) Art.Puff(c, new Vector2(xs[i] - 0.15f, y0 + tops[i] + 0.2f), 0.6f, rnd);

        // のこぎり屋根の工場
        Box(c, 1.9f, y0, 5.4f, y0 + 1.3f, 0.85f);
        var saw = new List<Vector2> { new Vector2(1.9f, y0 + 1.3f) };
        for (float x = 1.9f; x < 5.39f; x += 0.7f)
        {
            saw.Add(new Vector2(x, y0 + 1.75f));
            saw.Add(new Vector2(Mathf.Min(x + 0.7f, 5.4f), y0 + 1.3f));
        }
        c.Shape(saw, Paper, LW, InkC);
        c.Hatch(saw, 30f, 0.1f, Thin, InkC);
        c.Stroke(saw, LW, InkC, true);
        Windows(c, 2.15f, y0 + 0.55f, 0.45f, 7, 0.22f, 0.4f);

        // 時計塔
        float cx = 1.1f;
        Box(c, cx - 0.38f, y0, cx + 0.38f, y0 + 3.0f, 0.6f);
        for (int i = 0; i < 4; i++) c.Shape(RoundRect(cx - 0.08f, y0 + 0.4f + i * 0.6f, cx + 0.08f, y0 + 0.75f + i * 0.6f, 0.06f), Dark, Thin, InkC);
        Box(c, cx - 0.5f, y0 + 3.0f, cx + 0.5f, y0 + 3.85f, 0.85f);
        c.Shape(Ellipse(new Vector2(cx, y0 + 3.42f), 0.32f, 0.32f, 28), Paper, LW, InkC);
        c.Line(new Vector2(cx, y0 + 3.42f), new Vector2(cx, y0 + 3.66f), Thin * 1.5f, InkC);
        c.Line(new Vector2(cx, y0 + 3.42f), new Vector2(cx + 0.15f, y0 + 3.36f), Thin * 1.5f, InkC);
        var spire = new List<Vector2> { new Vector2(cx - 0.55f, y0 + 3.85f), new Vector2(cx + 0.55f, y0 + 3.85f), new Vector2(cx, y0 + 4.7f) };
        c.Fill(spire, Paper);
        c.Hatch(new List<Vector2> { new Vector2(cx, y0 + 4.7f), new Vector2(cx + 0.55f, y0 + 3.85f), new Vector2(cx + 0.05f, y0 + 3.85f) }, 70f, 0.07f, Thin, InkC);
        c.Stroke(spire, LW, InkC, true);
        return c.Save(dir + "/W2_London.png", new Vector2(0.5f, y0 / h));
    }

    // ─── W3 地中海 ───────────────────────────────────────────────
    public static Sprite MakePort(string dir)
    {
        var rnd = new System.Random(43);
        float w = 5.6f, h = 4.4f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);

        // 丘
        var hill = new List<Vector2>(Ellipse(new Vector2(2.2f, y0), 2.1f, 1.5f, 30, 0f, 180f));
        c.Shape(hill, Paper, LW, InkC);

        // 白い箱の家（奥から）
        var houses = new (float x, float y, float w, float h)[]
        {
            (1.6f, 1.15f, 0.8f, 0.6f), (2.5f, 1.05f, 0.7f, 0.55f), (0.9f, 0.6f, 0.75f, 0.55f),
            (1.75f, 0.45f, 0.9f, 0.65f), (2.75f, 0.4f, 0.75f, 0.55f), (0.5f, 0.0f, 0.85f, 0.6f),
            (1.45f, 0.0f, 0.7f, 0.5f), (2.3f, 0.0f, 0.9f, 0.55f), (3.25f, 0.0f, 0.6f, 0.45f),
        };
        foreach (var hs in houses)
        {
            float x0 = hs.x, yb = y0 + hs.y;
            c.Shape(Rect(x0, yb, x0 + hs.w, yb + hs.h), Paper, LW, InkC);
            c.Hatch(Rect(x0 + hs.w * 0.75f, yb, x0 + hs.w, yb + hs.h), 80f, 0.07f, Thin, InkC);
            c.Stroke(Rect(x0, yb, x0 + hs.w, yb + hs.h), LW, InkC, true);
            c.Shape(RoundRect(x0 + hs.w * 0.25f, yb + hs.h * 0.35f, x0 + hs.w * 0.4f, yb + hs.h * 0.7f, 0.05f), Dark, Thin, InkC);
        }
        // 丸屋根
        var dome = new List<Vector2>(Ellipse(new Vector2(2.0f, y0 + 1.75f), 0.32f, 0.36f, 18, 0f, 180f));
        c.Shape(dome, Shade, LW, InkC);
        c.Line(new Vector2(2.0f, y0 + 2.11f), new Vector2(2.0f, y0 + 2.4f), Thin * 1.4f, InkC);
        c.Line(new Vector2(1.9f, y0 + 2.3f), new Vector2(2.1f, y0 + 2.3f), Thin * 1.4f, InkC);

        // 灯台
        float lx = 4.75f;
        var body = new List<Vector2> { new Vector2(lx - 0.42f, y0), new Vector2(lx + 0.42f, y0), new Vector2(lx + 0.26f, y0 + 2.9f), new Vector2(lx - 0.26f, y0 + 2.9f) };
        c.Fill(body, Paper);
        for (int i = 0; i < 3; i++)
        {
            float a = y0 + 0.5f + i * 0.9f, b = a + 0.45f;
            float ka = (a - y0) / 2.9f, kb = (b - y0) / 2.9f;
            var band = new List<Vector2>
            {
                new Vector2(Mathf.Lerp(lx - 0.42f, lx - 0.26f, ka), a), new Vector2(Mathf.Lerp(lx + 0.42f, lx + 0.26f, ka), a),
                new Vector2(Mathf.Lerp(lx + 0.42f, lx + 0.26f, kb), b), new Vector2(Mathf.Lerp(lx - 0.42f, lx - 0.26f, kb), b),
            };
            c.Hatch(band, 30f, 0.06f, Thin, InkC);
        }
        c.Stroke(body, LW, InkC, true);
        c.Shape(Rect(lx - 0.4f, y0 + 2.9f, lx + 0.4f, y0 + 3.0f), Shade, Thin, InkC);
        c.Shape(Rect(lx - 0.22f, y0 + 3.0f, lx + 0.22f, y0 + 3.4f), Light, LW, InkC);
        c.Line(new Vector2(lx, y0 + 3.0f), new Vector2(lx, y0 + 3.4f), Thin, InkC);
        var cap = new List<Vector2> { new Vector2(lx - 0.32f, y0 + 3.4f), new Vector2(lx + 0.32f, y0 + 3.4f), new Vector2(lx, y0 + 3.75f) };
        c.Shape(cap, Dark, LW, InkC);
        foreach (float a in new[] { 10f, 25f, 155f, 170f })
        {
            var d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            c.Line(new Vector2(lx, y0 + 3.2f) + d * 0.4f, new Vector2(lx, y0 + 3.2f) + d * R(rnd, 0.7f, 0.85f), Thin * 1.3f, InkC);
        }
        return c.Save(dir + "/W3_Port.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeShip(string dir)
    {
        float w = 2.0f, h = 2.0f, y0 = 0.25f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        var hull = new List<Vector2> { new Vector2(0.15f, y0 + 0.35f), new Vector2(1.85f, y0 + 0.35f), new Vector2(1.5f, y0), new Vector2(0.45f, y0) };
        c.Shape(hull, Paper, LW, InkC);
        c.Hatch(hull, 10f, 0.07f, Thin, InkC);
        c.Stroke(hull, LW, InkC, true);
        c.Line(new Vector2(1.0f, y0 + 0.35f), new Vector2(1.0f, y0 + 1.6f), LW, InkC);
        var sail = new List<Vector2> { new Vector2(1.05f, y0 + 1.5f), new Vector2(1.65f, y0 + 0.5f), new Vector2(1.05f, y0 + 0.5f) };
        c.Shape(sail, Paper, LW, InkC);
        var sail2 = new List<Vector2> { new Vector2(0.95f, y0 + 1.3f), new Vector2(0.95f, y0 + 0.55f), new Vector2(0.45f, y0 + 0.55f) };
        c.Shape(sail2, Paper, LW, InkC);
        c.Fill(new List<Vector2> { new Vector2(1.0f, y0 + 1.6f), new Vector2(1.3f, y0 + 1.52f), new Vector2(1.0f, y0 + 1.44f) }, InkC);
        // 波
        c.Stroke(Ellipse(new Vector2(0.3f, y0 - 0.02f), 0.15f, 0.06f, 8, 0f, 180f), Thin, InkC);
        c.Stroke(Ellipse(new Vector2(1.75f, y0 - 0.02f), 0.15f, 0.06f, 8, 0f, 180f), Thin, InkC);
        return c.Save(dir + "/W3_Ship.png", new Vector2(0.5f, y0 / h));
    }

    // ─── W4 中東・ペルシャ ─────────────────────────────────────────
    static List<Vector2> Onion(float cx, float b, float hw, float hgt)
    {
        var right = new List<Vector2>
        {
            new Vector2(cx + hw * 0.8f, b), new Vector2(cx + hw * 1.05f, b + hgt * 0.3f), new Vector2(cx + hw * 0.85f, b + hgt * 0.6f),
            new Vector2(cx + hw * 0.35f, b + hgt * 0.82f), new Vector2(cx, b + hgt),
        };
        var pts = new List<Vector2>(Smooth(right, 6));
        var left = new List<Vector2>();
        for (int i = right.Count - 1; i >= 0; i--) left.Add(new Vector2(2f * cx - right[i].x, right[i].y));
        var sm = Smooth(left, 6);
        for (int i = 1; i < sm.Count; i++) pts.Add(sm[i]);
        return pts;
    }

    static void Minaret(InkCanvas c, float cx, float y0, float body)
    {
        Box(c, cx - 0.17f, y0, cx + 0.17f, y0 + body, 0.55f);
        c.Shape(Rect(cx - 0.27f, y0 + body * 0.7f, cx + 0.27f, y0 + body * 0.7f + 0.1f), Shade, Thin, InkC);
        c.Shape(Rect(cx - 0.25f, y0 + body, cx + 0.25f, y0 + body + 0.1f), Shade, Thin, InkC);
        var top = Onion(cx, y0 + body + 0.1f, 0.22f, 0.5f);
        c.Shape(top, Paper, LW, InkC);
        c.Line(new Vector2(cx, y0 + body + 0.6f), new Vector2(cx, y0 + body + 0.8f), Thin * 1.3f, InkC);
    }

    public static Sprite MakePalace(string dir)
    {
        float w = 5.6f, h = 4.8f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float cx = 2.8f;

        Minaret(c, 0.55f, y0, 3.0f);
        Minaret(c, 5.05f, y0, 3.0f);

        // 中央の玉ねぎ屋根
        Box(c, cx - 0.8f, y0 + 1.3f, cx + 0.8f, y0 + 1.65f, 0.8f);
        var dome = Onion(cx, y0 + 1.65f, 0.95f, 2.0f);
        c.Fill(dome, Paper);
        var half = new List<Vector2> { new Vector2(cx + 0.1f, y0 + 3.6f) };
        foreach (var p in dome) if (p.x > cx + 0.1f) half.Add(p);
        if (half.Count > 2) c.Hatch(half, 75f, 0.08f, Thin, InkC);
        c.Stroke(dome, LW, InkC, true);
        c.Line(new Vector2(cx, y0 + 3.65f), new Vector2(cx, y0 + 4.2f), LW, InkC);
        c.Stroke(Ellipse(new Vector2(cx + 0.05f, y0 + 4.25f), 0.12f, 0.12f, 14, -60f, 240f), Thin * 1.5f, InkC);

        // 城壁とアーチ
        Box(c, 0.9f, y0, 4.7f, y0 + 1.35f, 0.9f);
        for (int i = 0; i < 5; i++)
        {
            float x = 1.3f + i * 0.75f;
            ArchDoor(c, x, y0 + (i == 2 ? 0f : 0.2f), i == 2 ? 0.32f : 0.2f, i == 2 ? 0.95f : 0.75f, Dark);
        }
        for (float x = 0.95f; x < 4.65f; x += 0.3f)
            c.Shape(new List<Vector2> { new Vector2(x, y0 + 1.35f), new Vector2(x + 0.2f, y0 + 1.35f), new Vector2(x + 0.1f, y0 + 1.5f) }, Paper, Thin, InkC);
        return c.Save(dir + "/W4_Palace.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakePalm(string dir, int variant)
    {
        var rnd = new System.Random(200 + variant * 11);
        float w = 2.4f, h = 3.0f, y0 = 0.1f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float lean = R(rnd, -0.35f, 0.35f);
        var trunk = Smooth(new[] { new Vector2(1.2f, y0), new Vector2(1.2f + lean * 0.4f, y0 + 1.1f), new Vector2(1.2f + lean, y0 + 2.1f) }, 10);
        c.Stroke(trunk, 0.2f, InkC);
        c.Stroke(trunk, 0.12f, Paper);
        for (int i = 2; i < trunk.Count - 1; i += 3)
            c.Line(trunk[i] + new Vector2(-0.07f, 0f), trunk[i] + new Vector2(0.07f, 0.03f), Thin, InkC);
        var top = trunk[trunk.Count - 1];
        foreach (float a in new[] { 15f, 50f, 95f, 135f, 170f, 205f, -25f })
        {
            float ang = (a + R(rnd, -8f, 8f)) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            var n = new Vector2(-d.y, d.x);
            float len = R(rnd, 0.75f, 1.0f);
            var tip = top + d * len + Vector2.down * 0.25f;
            var mid = top + d * len * 0.5f + Vector2.up * 0.08f;
            var leaf = new List<Vector2> { top, mid + n * 0.13f, tip, mid - n * 0.06f };
            c.Shape(leaf, Paper, LW, InkC);
            c.Line(top, tip, Thin, InkC);
        }
        return c.Save(dir + "/Palm_" + variant + ".png", new Vector2(0.5f, y0 / h));
    }

    // ─── W5 日本 ─────────────────────────────────────────────────
    public static Sprite MakePagoda(string dir)
    {
        float w = 3.8f, h = 5.4f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float cx = w * 0.5f;

        c.Shape(Rect(cx - 1.1f, y0, cx + 1.1f, y0 + 0.2f), Shade, LW, InkC);
        float y = y0 + 0.2f;
        for (int k = 0; k < 5; k++)
        {
            float bw = 0.75f - k * 0.08f, bh = 0.5f - k * 0.03f;
            Box(c, cx - bw, y, cx + bw, y + bh, 0.7f);
            c.Shape(RoundRect(cx - 0.12f, y + 0.08f, cx + 0.12f, y + bh - 0.08f, 0.04f), Dark, Thin, InkC);
            y += bh;
            float rw = bw + 0.55f, rt = 0.32f;
            var roof = new List<Vector2>
            {
                new Vector2(cx - rw - 0.12f, y + 0.12f), new Vector2(cx - rw, y), new Vector2(cx + rw, y), new Vector2(cx + rw + 0.12f, y + 0.12f),
                new Vector2(cx + bw * 0.55f, y + rt), new Vector2(cx - bw * 0.55f, y + rt),
            };
            c.Fill(roof, Paper);
            c.Hatch(new List<Vector2> { new Vector2(cx + 0.1f, y + rt), new Vector2(cx + bw * 0.55f, y + rt), new Vector2(cx + rw + 0.12f, y + 0.12f), new Vector2(cx + rw, y), new Vector2(cx + 0.1f, y) }, 20f, 0.07f, Thin, InkC);
            c.Stroke(roof, LW, InkC, true);
            y += rt;
        }
        // 相輪
        c.Line(new Vector2(cx, y), new Vector2(cx, y + 0.95f), LW * 1.2f, InkC);
        for (int i = 0; i < 5; i++) c.Line(new Vector2(cx - 0.1f, y + 0.15f + i * 0.13f), new Vector2(cx + 0.1f, y + 0.15f + i * 0.13f), Thin * 1.3f, InkC);
        c.Shape(Ellipse(new Vector2(cx, y + 1.0f), 0.07f, 0.09f, 10), Paper, Thin, InkC);
        return c.Save(dir + "/W5_Pagoda.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeTorii(string dir)
    {
        float w = 2.6f, h = 2.4f, y0 = 0.1f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        foreach (float px in new[] { 0.65f, 1.95f })
        {
            c.Shape(Rect(px - 0.1f, y0, px + 0.1f, y0 + 1.85f), Shade, LW, InkC);
            c.Shape(Rect(px - 0.16f, y0, px + 0.16f, y0 + 0.15f), Dark, Thin, InkC);
        }
        c.Shape(Rect(0.35f, y0 + 1.35f, 2.25f, y0 + 1.5f), Shade, LW, InkC);
        var top = new List<Vector2> { new Vector2(0.05f, y0 + 2.05f), new Vector2(0.25f, y0 + 1.82f), new Vector2(2.35f, y0 + 1.82f), new Vector2(2.55f, y0 + 2.05f), new Vector2(1.3f, y0 + 1.98f) };
        c.Shape(top, Dark, LW, InkC);
        c.Shape(Rect(1.2f, y0 + 1.5f, 1.4f, y0 + 1.82f), Shade, Thin, InkC);
        return c.Save(dir + "/W5_Torii.png", new Vector2(0.5f, y0 / h));
    }

    // ─── W6 アマゾン ─────────────────────────────────────────────
    public static Sprite MakeRuin(string dir)
    {
        var rnd = new System.Random(46);
        float w = 5.4f, h = 3.9f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float cx = w * 0.5f;
        for (int k = 0; k < 4; k++)
        {
            float hw = 2.4f - k * 0.5f, yb = y0 + k * 0.6f;
            Box(c, cx - hw, yb, cx + hw, yb + 0.6f, 0.82f);
            for (int i = 0; i < 6; i++)
            {
                float x = R(rnd, cx - hw + 0.1f, cx + hw - 0.4f), yy = R(rnd, yb + 0.12f, yb + 0.5f);
                c.Line(new Vector2(x, yy), new Vector2(x + R(rnd, 0.15f, 0.3f), yy), Thin, InkC);
            }
        }
        // 階段
        c.Shape(Rect(cx - 0.35f, y0, cx + 0.35f, y0 + 2.4f), Light, LW, InkC);
        for (float y = y0 + 0.15f; y < y0 + 2.4f; y += 0.15f) c.Line(new Vector2(cx - 0.35f, y), new Vector2(cx + 0.35f, y), Thin, InkC);
        // 頂上の祠
        Box(c, cx - 0.6f, y0 + 2.4f, cx + 0.6f, y0 + 3.15f, 0.75f);
        c.Shape(Rect(cx - 0.75f, y0 + 3.15f, cx + 0.75f, y0 + 3.3f), Shade, LW, InkC);
        c.Shape(Rect(cx - 0.2f, y0 + 2.4f, cx + 0.2f, y0 + 2.9f), Dark, Thin, InkC);
        // つた
        for (int i = 0; i < 9; i++)
        {
            float x = R(rnd, cx - 2.2f, cx + 2.2f);
            float top = y0 + 2.4f - Mathf.Abs(x - cx) * 0.9f;
            top = Mathf.Max(top, y0 + 0.6f);
            var vine = new List<Vector2>();
            float len = R(rnd, 0.5f, 1.2f);
            for (int k = 0; k <= 6; k++) vine.Add(new Vector2(x + Mathf.Sin(k * 1.3f + i) * 0.06f, top - len * k / 6f));
            c.Stroke(vine, Thin * 1.3f, InkC);
            for (int k = 1; k < 6; k += 2) c.Shape(Ellipse(vine[k] + new Vector2(0.06f, 0f), 0.06f, 0.035f, 8), Paper, Thin, InkC);
        }
        return c.Save(dir + "/W6_Ruin.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeJungleTree(string dir, int variant)
    {
        var rnd = new System.Random(300 + variant * 13);
        float w = 2.4f, h = 3.0f, y0 = 0.1f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float cx = 1.2f;
        var trunk = new List<Vector2> { new Vector2(cx - 0.14f, y0), new Vector2(cx + 0.14f, y0), new Vector2(cx + 0.1f, y0 + 1.4f), new Vector2(cx - 0.1f, y0 + 1.4f) };
        c.Shape(trunk, Paper, LW, InkC);
        c.Hatch(trunk, 80f, 0.06f, Thin, InkC);
        c.Line(new Vector2(cx - 0.14f, y0), new Vector2(cx - 0.35f, y0 - 0.02f), Thin * 1.5f, InkC);
        c.Line(new Vector2(cx + 0.14f, y0), new Vector2(cx + 0.35f, y0 + 0.02f), Thin * 1.5f, InkC);
        var blobs = new (float x, float y, float r)[] { (-0.55f, 1.55f, 0.45f), (0.55f, 1.6f, 0.45f), (0f, 1.85f, 0.55f), (-0.35f, 2.3f, 0.45f), (0.35f, 2.35f, 0.45f), (0f, 2.6f, 0.38f) };
        foreach (var b in blobs)
        {
            var center = new Vector2(cx + b.x * R(rnd, 0.9f, 1.1f), y0 + b.y);
            float r = b.r * R(rnd, 0.9f, 1.1f);
            c.Shape(Ellipse(center, r, r * 0.8f, 24), Paper, LW, InkC);
            c.Hatch(new List<Vector2>(Ellipse(center, r * 0.92f, r * 0.72f, 14, 270f, 380f)) { center }, 60f, 0.08f, Thin, InkC);
            c.Stroke(Ellipse(center + new Vector2(-r * 0.2f, r * 0.1f), r * 0.4f, r * 0.25f, 8, 20f, 160f), Thin, InkC);
        }
        return c.Save(dir + "/JungleTree_" + variant + ".png", new Vector2(0.5f, y0 / h));
    }

    // ─── W7 NEO ──────────────────────────────────────────────────
    public static Sprite MakeNeo(string dir)
    {
        var rnd = new System.Random(47);
        float w = 5.6f, h = 4.8f, y0 = 0.2f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        float cx = w * 0.5f;

        void Tower(float x0, float x1, float top, float spike)
        {
            var poly = new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y0 + top), new Vector2((x0 + x1) * 0.5f, y0 + top + spike), new Vector2(x0, y0 + top) };
            c.Shape(poly, Dark, LW, InkC);
            for (float y = y0 + 0.25f; y < y0 + top - 0.1f; y += 0.22f)
                for (float x = x0 + 0.08f; x < x1 - 0.1f; x += 0.16f)
                    if (rnd.NextDouble() < 0.55) c.Fill(Rect(x, y, x + 0.06f, y + 0.08f), Paper);
            c.Line(new Vector2(x1 - 0.05f, y0), new Vector2(x1 - 0.05f, y0 + top), Thin, Light);
        }

        // 奥の塔
        Tower(0.5f, 1.2f, 2.2f, 0.4f);
        Tower(4.4f, 5.1f, 2.5f, 0.5f);
        Tower(1.2f, 1.9f, 2.9f, 0.6f);
        Tower(3.7f, 4.4f, 3.1f, 0.4f);
        // 中央の尖塔
        Tower(cx - 0.45f, cx + 0.45f, 3.3f, 1.1f);
        c.Line(new Vector2(cx, y0 + 4.4f), new Vector2(cx, y0 + 4.55f), LW, InkC);
        // 浮かぶ輪
        c.Stroke(Ellipse(new Vector2(cx, y0 + 3.0f), 1.25f, 0.28f, 40, 180f, 360f), LW * 1.5f, InkC);
        c.Stroke(Ellipse(new Vector2(cx, y0 + 3.0f), 1.25f, 0.28f, 40, 0f, 180f), LW, InkC);
        c.Stroke(Ellipse(new Vector2(cx, y0 + 3.0f), 1.12f, 0.22f, 40, 180f, 360f), Thin, InkC);
        c.Fill(Sparkle(new Vector2(cx - 1.25f, y0 + 3.0f), 0.18f), InkC);
        c.Fill(Sparkle(new Vector2(cx + 1.25f, y0 + 3.0f), 0.18f), InkC);
        // 足元の基部
        c.Shape(new List<Vector2> { new Vector2(0.2f, y0), new Vector2(5.4f, y0), new Vector2(5.0f, y0 + 0.35f), new Vector2(0.6f, y0 + 0.35f) }, Shade, LW, InkC);
        for (float x = 0.7f; x < 5.0f; x += 0.4f) c.Line(new Vector2(x, y0 + 0.05f), new Vector2(x + 0.1f, y0 + 0.3f), Thin, InkC);
        return c.Save(dir + "/W7_Neo.png", new Vector2(0.5f, y0 / h));
    }

    // ─── UI 的な部品 ──────────────────────────────────────────────
    /// <summary>選択中のワールドの名札の下で跳ねる上向きの矢印（pivot は上の先端）</summary>
    public static Sprite MakePointer(string dir)
    {
        float w = 1.0f, h = 1.2f;
        var c = new InkCanvas(w, h, 128f, Clear);
        var arrow = new List<Vector2>
        {
            new Vector2(0.5f, 1.14f), new Vector2(0.92f, 0.65f), new Vector2(0.68f, 0.65f), new Vector2(0.68f, 0.08f),
            new Vector2(0.32f, 0.08f), new Vector2(0.32f, 0.65f), new Vector2(0.08f, 0.65f),
        };
        c.Fill(arrow, Paper);
        c.Hatch(new List<Vector2> { new Vector2(0.5f, 1.14f), new Vector2(0.92f, 0.65f), new Vector2(0.68f, 0.65f), new Vector2(0.68f, 0.08f), new Vector2(0.5f, 0.08f) }, 60f, 0.07f, Thin, InkC);
        c.Stroke(arrow, 0.07f, InkC, true);
        return c.Save(dir + "/Pointer.png", new Vector2(0.5f, 1.15f / h));
    }

    /// <summary>クリア済みの旗（pivot は柱の根元）</summary>
    public static Sprite MakeFlag(string dir)
    {
        float w = 1.2f, h = 1.6f;
        var c = new InkCanvas(w, h, 128f, Clear);
        c.Shape(Ellipse(new Vector2(0.2f, 0.08f), 0.14f, 0.05f, 12), Shade, Thin, InkC);
        c.Line(new Vector2(0.2f, 0.08f), new Vector2(0.2f, 1.5f), 0.06f, InkC);
        var cloth = Smooth(new[] { new Vector2(0.22f, 1.48f), new Vector2(0.65f, 1.4f), new Vector2(1.12f, 1.25f), new Vector2(0.7f, 1.12f), new Vector2(0.22f, 0.98f) }, 5);
        c.Shape(cloth, Paper, LW, InkC);
        c.Stroke(new[] { new Vector2(0.4f, 1.24f), new Vector2(0.52f, 1.12f), new Vector2(0.75f, 1.35f) }, 0.06f, InkC);
        return c.Save(dir + "/Flag.png", new Vector2(0.2f / w, 0.08f / h));
    }

    /// <summary>未解放のワールドにかぶせる雲（pivot は中央）</summary>
    public static Sprite MakeFog(string dir)
    {
        var rnd = new System.Random(48);
        float w = 6.0f, h = 3.4f;
        var c = new InkCanvas(w, h, Ppu, Clear);
        var blobs = new List<(Vector2 p, float r)>();
        for (int i = 0; i < 16; i++)
        {
            float t = i / 15f;
            float x = Mathf.Lerp(0.8f, w - 0.8f, t) + R(rnd, -0.2f, 0.2f);
            float y = h * 0.5f + Mathf.Sin(t * Mathf.PI) * 0.5f * (i % 2 == 0 ? 1f : -1f) + R(rnd, -0.2f, 0.2f);
            blobs.Add((new Vector2(x, y), R(rnd, 0.55f, 0.85f) * (0.65f + Mathf.Sin(t * Mathf.PI) * 0.45f)));
        }
        blobs.Sort((a, b) => b.p.y.CompareTo(a.p.y));
        foreach (var b in blobs)
        {
            c.Shape(Ellipse(b.p, b.r, b.r * 0.8f, 28), Paper, LW, InkC);
            c.Stroke(Ellipse(b.p + new Vector2(b.r * 0.15f, -b.r * 0.15f), b.r * 0.55f, b.r * 0.35f, 10, 200f, 320f), Thin, InkC);
        }
        return c.Save(dir + "/Fog.png", new Vector2(0.5f, 0.5f));
    }

    public static readonly Vector2 BannerTextSize = new Vector2(3.2f, 1.0f);

    /// <summary>ワールド名の名札（巻物のリボン。pivot は中央）</summary>
    public static Sprite MakeBanner(string dir)
    {
        float w = 4.8f, h = 1.6f;
        var c = new InkCanvas(w, h, 128f, Clear);
        // 両端の折り返し
        foreach (int s in new[] { -1, 1 })
        {
            float x0 = w * 0.5f + s * 1.75f, x1 = w * 0.5f + s * 2.35f;
            var tail = new List<Vector2> { new Vector2(x0, 0.25f), new Vector2(x1, 0.25f), new Vector2(x1 - s * 0.22f, 0.55f), new Vector2(x1, 0.85f), new Vector2(x0, 0.85f) };
            c.Shape(tail, Shade, LW, InkC);
            c.Shape(new List<Vector2> { new Vector2(x0, 0.25f), new Vector2(x0 + s * 0.12f, 0.12f), new Vector2(x0 + s * 0.12f, 0.4f) }, Art.Mid, Thin, InkC);
        }
        c.Shape(RoundRect(w * 0.5f - 1.85f, 0.4f, w * 0.5f + 1.85f, 1.5f, 0.08f), Paper, LW * 1.3f, InkC);
        c.Stroke(RoundRect(w * 0.5f - 1.75f, 0.48f, w * 0.5f + 1.75f, 1.42f, 0.05f), Thin, InkC, true);
        return c.Save(dir + "/Banner.png", new Vector2(0.5f, 0.95f / h));
    }

    // ─── 地面に焼き込む小物 ─────────────────────────────────────────
    public static void DrawDune(InkCanvas c, Vector2 at, float w, System.Random rnd)
    {
        var arc = Ellipse(at, w * 0.5f, w * 0.18f, 16, 15f, 165f);
        c.Stroke(arc, Thin * 1.2f, InkC);
        c.Hatch(new List<Vector2>(Ellipse(at, w * 0.5f, w * 0.18f, 10, 15f, 80f)) { at }, 70f, 0.09f, Thin * 0.8f, InkC, rnd, 0.03f);
    }

    public static void DrawWave(InkCanvas c, Vector2 at, float s)
    {
        c.Stroke(Ellipse(at, 0.16f * s, 0.07f * s, 8, 0f, 180f), Thin, InkC);
        c.Stroke(Ellipse(at + new Vector2(0.32f * s, 0f), 0.16f * s, 0.07f * s, 8, 0f, 180f), Thin, InkC);
    }
}
