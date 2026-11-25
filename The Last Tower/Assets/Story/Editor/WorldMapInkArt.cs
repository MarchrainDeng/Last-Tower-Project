using System.Collections.Generic;
using UnityEngine;
using static InkCanvas;

/// <summary>
/// ワールドマップの仮の絵（白黒のペン画風）をコードで描く
/// 仕様書の例イメージに寄せている：針葉樹、廃墟の金属台座、金属枠の看板、潜水ヘルメットのロボット、風車と城、工場と歯車と門
/// 各関数は PNG を書き出して Sprite を返す。地面に焼き込む小物（草・石・茂み・山・崖・池）は Draw～ で描く
/// </summary>
public static class WorldMapInkArt
{
    public static readonly Color InkC = new Color(0.11f, 0.10f, 0.10f);
    public static readonly Color Paper = new Color(0.97f, 0.96f, 0.93f);
    public static readonly Color Light = new Color(0.90f, 0.89f, 0.86f);
    public static readonly Color Shade = new Color(0.80f, 0.79f, 0.76f);
    public static readonly Color Mid = new Color(0.62f, 0.61f, 0.59f);
    public static readonly Color Dark = new Color(0.30f, 0.29f, 0.28f);
    static readonly Color Clear = new Color(Paper.r, Paper.g, Paper.b, 0f);

    const float LW = 0.04f;   // 基本の線の太さ（ユニット）
    const float Thin = 0.022f;

    static float R(System.Random rnd, float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    // ─── 地面に焼き込む小物 ─────────────────────────────────────────
    public static void DrawTuft(InkCanvas c, Vector2 b, System.Random rnd)
    {
        float s = R(rnd, 0.7f, 1.2f);
        c.Line(b, b + new Vector2(-0.09f, 0.11f) * s, Thin, InkC);
        c.Line(b, b + new Vector2(0.0f, 0.15f) * s, Thin, InkC);
        c.Line(b, b + new Vector2(0.10f, 0.10f) * s, Thin, InkC);
    }

    public static void DrawStone(InkCanvas c, Vector2 p, System.Random rnd)
    {
        float rx = R(rnd, 0.09f, 0.2f), ry = rx * R(rnd, 0.5f, 0.7f);
        c.Shape(Ellipse(p, rx, ry, 16), Paper, Thin * 1.3f, InkC);
        c.Line(p + new Vector2(rx * 0.1f, -ry * 0.4f), p + new Vector2(rx * 0.7f, -ry * 0.1f), Thin, InkC);
    }

    public static void DrawBush(InkCanvas c, Vector2 at, float s, System.Random rnd)
    {
        var blobs = new (float x, float y, float r)[] { (-0.35f, 0.22f, 0.27f), (0.30f, 0.20f, 0.26f), (0.0f, 0.34f, 0.34f), (-0.15f, 0.10f, 0.24f), (0.18f, 0.08f, 0.23f) };
        foreach (var b in blobs)
        {
            var center = at + new Vector2(b.x, b.y) * s;
            float r = b.r * s * R(rnd, 0.9f, 1.1f);
            c.Shape(Ellipse(center, r, r * 0.85f, 24), Paper, LW * 0.9f, InkC);
            // 葉の小さな弧
            c.Stroke(Ellipse(center + new Vector2(r * 0.15f, -r * 0.1f), r * 0.45f, r * 0.3f, 8, 200f, 320f), Thin, InkC);
        }
    }

    public static void DrawPond(InkCanvas c, Vector2 center, float rx, float ry, System.Random rnd)
    {
        c.Shape(Ellipse(center, rx, ry, 48), Light, LW * 1.2f, InkC);
        c.Stroke(Ellipse(center, rx * 0.88f, ry * 0.85f, 30, 200f, 300f), Thin, InkC);
        c.Stroke(Ellipse(center + new Vector2(0.3f, 0.1f), rx * 0.45f, ry * 0.3f, 20, 190f, 350f), Thin, InkC);
        c.Stroke(Ellipse(center + new Vector2(-0.5f, 0.3f), rx * 0.25f, ry * 0.18f, 16, 180f, 360f), Thin, InkC);
        // 岸の草
        for (int i = 0; i < 10; i++)
        {
            float a = R(rnd, 0f, Mathf.PI * 2f);
            var p = center + new Vector2(Mathf.Cos(a) * rx * 1.05f, Mathf.Sin(a) * ry * 1.08f);
            DrawTuft(c, p, rnd);
        }
    }

    /// <summary>山（base は麓の中心）。右側の面に斜線の影</summary>
    public static void DrawMountain(InkCanvas c, Vector2 b, float w, float h, System.Random rnd)
    {
        var peak = b + new Vector2(R(rnd, -0.1f, 0.1f) * w, h);
        var left = new Vector2(b.x - w * 0.5f, b.y);
        var right = new Vector2(b.x + w * 0.5f, b.y);

        var ridge = new List<Vector2> { left };
        for (int i = 1; i <= 3; i++)
        {
            var p = Vector2.Lerp(left, peak, i / 4f);
            ridge.Add(p + new Vector2(R(rnd, -0.08f, 0.08f) * w, R(rnd, -0.05f, 0.06f) * h));
        }
        ridge.Add(peak);
        var rightSide = new List<Vector2>();
        for (int i = 1; i <= 3; i++)
        {
            var p = Vector2.Lerp(peak, right, i / 4f);
            rightSide.Add(p + new Vector2(R(rnd, -0.06f, 0.08f) * w, R(rnd, -0.05f, 0.06f) * h));
        }
        ridge.AddRange(rightSide);
        ridge.Add(right);

        c.Fill(ridge, Paper);

        // 影の面：頂上から麓へ下りる稜線より右側
        var crest = new List<Vector2> { peak };
        for (int i = 1; i <= 4; i++)
        {
            var p = Vector2.Lerp(peak, new Vector2(b.x + w * 0.08f, b.y), i / 4f);
            crest.Add(p + new Vector2(R(rnd, -0.05f, 0.05f) * w, 0f));
        }
        var shadow = new List<Vector2>(crest);
        shadow.Add(right);
        for (int i = rightSide.Count - 1; i >= 0; i--) shadow.Add(rightSide[i]);
        c.Hatch(shadow, -58f, 0.1f, Thin, InkC, rnd, 0.06f);
        c.Stroke(crest, Thin * 1.4f, InkC);

        // 左側の岩の筋
        for (int i = 0; i < 4; i++)
        {
            var p = Vector2.Lerp(left, peak, R(rnd, 0.3f, 0.85f)) + new Vector2(R(rnd, 0.05f, 0.2f) * w, -R(rnd, 0.0f, 0.1f) * h);
            c.Line(p, p + new Vector2(R(rnd, 0.05f, 0.15f) * w, -R(rnd, 0.1f, 0.2f) * h), Thin, InkC);
        }
        c.Stroke(ridge, LW * 1.2f, InkC);
    }

    /// <summary>崖。rim は台地の縁（上辺）、そこから depth 下までが岩肌</summary>
    public static void DrawCliff(InkCanvas c, List<Vector2> rim, float depth, System.Random rnd)
    {
        // 縁に沿って岩の塊（柱）を並べる。奥（上）の塊から描いて、手前の塊を重ねる
        var cols = new List<(Vector2 a, Vector2 b)>();
        for (int i = 0; i < rim.Count - 1; i++)
        {
            float len = Vector2.Distance(rim[i], rim[i + 1]);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.75f));
            float t0 = 0f;
            for (int k = 1; k <= n; k++)
            {
                float t1 = k == n ? 1f : (k + R(rnd, -0.25f, 0.25f)) / n;
                cols.Add((Vector2.Lerp(rim[i], rim[i + 1], t0), Vector2.Lerp(rim[i], rim[i + 1], t1)));
                t0 = t1;
            }
        }
        cols.Sort((p, q) => (q.a.y + q.b.y).CompareTo(p.a.y + p.b.y));

        var bottoms = new List<Vector2>();
        foreach (var col in cols)
        {
            float d = depth * R(rnd, 0.75f, 1.05f);
            Vector2 a = col.a, b = col.b;
            var topMid = (a + b) * 0.5f + new Vector2(0f, R(rnd, 0.04f, 0.16f));
            var bl = a + new Vector2(R(rnd, -0.06f, 0.04f), -d);
            var br = b + new Vector2(R(rnd, -0.04f, 0.06f), -d * R(rnd, 0.9f, 1.05f));
            var poly = new List<Vector2>
            {
                a + new Vector2(-0.03f, -0.12f), a + new Vector2(0.06f, 0.02f), topMid, b + new Vector2(-0.06f, 0.02f),
                b + new Vector2(0.03f, -0.12f), br + new Vector2(0.02f, 0.1f), (bl + br) * 0.5f + new Vector2(0f, -0.04f), bl + new Vector2(-0.02f, 0.1f)
            };
            c.Fill(poly, Paper);
            // 右側を影に
            var shade = new List<Vector2> { Vector2.Lerp(topMid, b, 0.3f), b + new Vector2(0.03f, -0.12f), br + new Vector2(0.02f, 0.1f), Vector2.Lerp(bl, br, 0.6f) };
            c.Hatch(shade, 80f, 0.075f, Thin, InkC, rnd, 0.08f);
            // 横の割れ目
            for (int k = 0; k < 2; k++)
            {
                float t = R(rnd, 0.3f, 0.75f);
                var l = Vector2.Lerp(a, bl, t);
                c.Line(l + new Vector2(0.05f, 0f), Vector2.Lerp(l, Vector2.Lerp(b, br, t), R(rnd, 0.3f, 0.6f)), Thin, InkC);
            }
            c.Stroke(poly, LW, InkC, true);
            bottoms.Add((bl + br) * 0.5f);
        }

        // 崖下の小さな岩と落石
        foreach (var p in bottoms)
        {
            if (rnd.NextDouble() < 0.55)
            {
                var q = p + new Vector2(R(rnd, -0.2f, 0.2f), -0.05f);
                float w = R(rnd, 0.3f, 0.5f), h = R(rnd, 0.3f, 0.45f);
                var rock = new List<Vector2> { q + new Vector2(-w, 0f), q + new Vector2(-w * 0.8f, h * 0.8f), q + new Vector2(-w * 0.1f, h), q + new Vector2(w * 0.7f, h * 0.7f), q + new Vector2(w, 0f) };
                c.Fill(rock, Paper);
                c.Hatch(new List<Vector2> { q + new Vector2(w * 0.1f, h * 0.9f), q + new Vector2(w * 0.7f, h * 0.7f), q + new Vector2(w, 0f), q + new Vector2(w * 0.1f, 0f) }, 70f, 0.07f, Thin, InkC);
                c.Stroke(rock, LW, InkC, true);
            }
            DrawStone(c, p + new Vector2(R(rnd, -0.5f, 0.5f), -R(rnd, 0.15f, 0.45f)), rnd);
        }
        c.Stroke(rim, LW * 1.6f, InkC);
    }

    // ─── 木 ───────────────────────────────────────────────────────
    public static Sprite MakePine(string dir, int variant)
    {
        var rnd = new System.Random(100 + variant * 7);
        var c = new InkCanvas(1.6f, 2.6f, 128f, Clear);
        float cx = 0.8f;
        c.Shape(Rect(cx - 0.07f, 0.05f, cx + 0.07f, 0.45f), Paper, LW, InkC);

        float baseHalf = R(rnd, 0.62f, 0.74f);
        int tiers = 4;
        for (int k = 0; k < tiers; k++)
        {
            float yb = 0.32f + k * 0.44f;
            float yt = k == tiers - 1 ? 2.5f : yb + 0.86f;
            float hw = baseHalf * (1f - k * 0.2f);
            float ym = (yb + yt) * 0.5f;
            var pts = new List<Vector2>
            {
                new Vector2(cx, yt),
                new Vector2(cx + hw * 0.5f, ym),
                new Vector2(cx + hw * 0.4f, ym - 0.03f),
                new Vector2(cx + hw, yb),
            };
            for (int j = 1; j <= 5; j++)
                pts.Add(new Vector2(cx + hw - j * (2f * hw / 6f), yb + (j % 2 == 1 ? 0.11f : 0.02f) * R(rnd, 0.8f, 1.2f)));
            pts.Add(new Vector2(cx - hw, yb));
            pts.Add(new Vector2(cx - hw * 0.4f, ym - 0.03f));
            pts.Add(new Vector2(cx - hw * 0.5f, ym));

            c.Fill(pts, Paper);
            var shade = new List<Vector2> { new Vector2(cx + 0.04f, yt - 0.12f), new Vector2(cx + hw * 0.9f, yb + 0.04f), new Vector2(cx + 0.08f, yb + 0.06f) };
            c.Hatch(shade, 62f, 0.075f, Thin * 0.9f, InkC, rnd, 0.03f);
            c.Stroke(pts, LW, InkC, true);
        }
        return c.Save(dir + "/Pine_" + variant + ".png", new Vector2(0.5f, 0.05f / 2.6f));
    }

    // ─── 台座 ─────────────────────────────────────────────────────
    // 台座の上面の中心（スプライトの足元からの位置）
    public static readonly Vector2 PedestalTopCenter = new Vector2(0f, 1.2f);

    public static Sprite MakePedestal(string dir)
    {
        var rnd = new System.Random(5);
        float w = 3.8f, h = 2.6f;
        var c = new InkCanvas(w, h, 128f, Clear);
        var ctr = new Vector2(w * 0.5f, 1.2f);
        float R0 = 1.5f, sq = 0.5f, thick = 0.38f;

        var top = new List<Vector2>();
        for (int k = 0; k < 8; k++)
        {
            float a = (22.5f + 45f * k) * Mathf.Deg2Rad;
            top.Add(ctr + new Vector2(Mathf.Cos(a) * R0, Mathf.Sin(a) * R0 * sq));
        }
        // がれき（後ろ）
        for (int i = 0; i < 4; i++) DrawStone(c, ctr + new Vector2(R(rnd, -1.6f, 1.6f), R(rnd, 0.35f, 0.75f)), rnd);

        // 側面（手前に見える下半分）
        int[] front = { 3, 4, 5, 6, 7, 0 };
        var side = new List<Vector2>();
        foreach (int k in front) side.Add(top[k]);
        for (int i = front.Length - 1; i >= 0; i--) side.Add(top[front[i]] - new Vector2(0f, thick));
        c.Shape(side, Shade, LW, InkC);
        for (int i = 1; i < front.Length - 1; i++)
            c.Line(top[front[i]], top[front[i]] - new Vector2(0f, thick), Thin * 1.3f, InkC);
        // 側面のボルト
        for (int i = 0; i < front.Length - 1; i++)
        {
            var m = (top[front[i]] + top[front[i + 1]]) * 0.5f - new Vector2(0f, thick * 0.5f);
            c.Shape(Ellipse(m, 0.04f, 0.04f, 10), Paper, Thin, InkC);
        }

        // 上面
        c.Shape(top, Paper, LW * 1.2f, InkC);
        var inner = new List<Vector2>();
        foreach (var p in top) inner.Add(ctr + (p - ctr) * 0.84f);
        c.Stroke(inner, Thin * 1.2f, InkC, true);
        for (int k = 0; k < 8; k++)
        {
            c.Line(top[k], inner[k], Thin, InkC);
            c.Shape(Ellipse(ctr + (top[k] - ctr) * 0.92f, 0.04f, 0.03f, 10), Shade, Thin, InkC);
        }
        // 歯車
        c.Shape(Gear(ctr, 0.66f, 0.54f, 12, sq), Light, LW, InkC);
        c.Shape(Ellipse(ctr, 0.38f, 0.38f * sq, 30), Paper, LW, InkC);
        c.Stroke(Ellipse(ctr, 0.26f, 0.26f * sq, 24), Thin, InkC, true);
        c.Shape(Ellipse(ctr, 0.1f, 0.1f * sq, 12), Dark, Thin, InkC);
        // ひび
        c.Stroke(new[] { ctr + new Vector2(0.75f, -0.18f), ctr + new Vector2(0.95f, -0.3f), ctr + new Vector2(1.05f, -0.28f), ctr + new Vector2(1.2f, -0.4f) }, Thin, InkC);
        c.Stroke(new[] { ctr + new Vector2(-0.9f, 0.25f), ctr + new Vector2(-1.05f, 0.18f), ctr + new Vector2(-1.15f, 0.26f) }, Thin, InkC);

        // 左のパイプ（太い線＋中を白で抜いて管に見せる）
        var pipe = new List<Vector2> { new Vector2(0.38f, 0.98f), new Vector2(0.22f, 1.1f), new Vector2(0.22f, 1.55f), new Vector2(0.42f, 1.7f), new Vector2(0.7f, 1.7f) };
        c.Stroke(Smooth(pipe, 6), 0.2f, InkC);
        c.Stroke(Smooth(pipe, 6), 0.12f, Paper);
        c.Shape(Rect(0.12f, 1.25f, 0.32f, 1.32f), Shade, Thin, InkC);
        c.Shape(Rect(0.66f, 1.6f, 0.74f, 1.8f), Shade, Thin, InkC);
        // 右のメーター箱
        c.Shape(RoundRect(3.25f, 1.25f, 3.6f, 1.75f, 0.05f), Paper, LW, InkC);
        c.Shape(Ellipse(new Vector2(3.425f, 1.52f), 0.12f, 0.12f, 18), Paper, Thin, InkC);
        c.Line(new Vector2(3.425f, 1.52f), new Vector2(3.5f, 1.58f), Thin, InkC);
        c.Line(new Vector2(3.3f, 1.25f), new Vector2(3.15f, 1.05f), 0.08f, InkC);

        // がれき（手前）
        for (int i = 0; i < 3; i++) DrawStone(c, ctr + new Vector2(R(rnd, -1.4f, 1.4f), -thick - R(rnd, 0.35f, 0.5f)), rnd);

        return c.Save(dir + "/Pedestal.png", new Vector2(0.5f, (ctr.y - thick - 0.6f) / h));
    }

    // 台座の足元（スプライトの pivot）から上面中心までの高さ
    public const float PedestalPivotToTop = 0.38f + 0.6f;

    public static Sprite MakeBurst(string dir)
    {
        var rnd = new System.Random(9);
        float w = 5.6f, h = 3.8f;
        var c = new InkCanvas(w, h, 100f, Clear);
        var ctr = new Vector2(w * 0.5f, h * 0.5f);
        float rx = 2.0f, ry = 1.25f;
        for (int i = 0; i < 22; i++)
        {
            if (i % 5 == 2) continue;
            float a = (i * (360f / 22f) + R(rnd, -4f, 4f)) * Mathf.Deg2Rad;
            var dir2 = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            float k0 = R(rnd, 1.02f, 1.1f), k1 = k0 + R(rnd, 0.14f, 0.26f);
            c.Line(ctr + dir2 * k0, ctr + dir2 * k1, 0.05f, InkC);
        }
        c.Fill(Sparkle(ctr + new Vector2(2.35f, 0.9f), 0.26f), InkC);
        c.Fill(Sparkle(ctr + new Vector2(-2.1f, -0.95f), 0.2f), InkC);
        c.Fill(Sparkle(ctr + new Vector2(1.6f, 1.55f), 0.14f), InkC);
        return c.Save(dir + "/Burst.png", new Vector2(0.5f, 0.5f));
    }

    public static Sprite MakePadlock(string dir)
    {
        var c = new InkCanvas(0.9f, 1.15f, 128f, Clear);
        var shackle = Ellipse(new Vector2(0.45f, 0.62f), 0.24f, 0.32f, 20, 0f, 180f);
        c.Stroke(shackle, 0.14f, InkC);
        c.Stroke(shackle, 0.07f, Shade);
        c.Shape(RoundRect(0.1f, 0.05f, 0.8f, 0.66f, 0.06f), Paper, LW, InkC);
        c.Stroke(Rect(0.16f, 0.11f, 0.74f, 0.6f), Thin, InkC, true);
        c.Fill(Ellipse(new Vector2(0.45f, 0.4f), 0.07f, 0.07f, 14), InkC);
        c.Fill(new List<Vector2> { new Vector2(0.41f, 0.38f), new Vector2(0.49f, 0.38f), new Vector2(0.52f, 0.2f), new Vector2(0.38f, 0.2f) }, InkC);
        foreach (var b in new[] { new Vector2(0.2f, 0.15f), new Vector2(0.7f, 0.15f), new Vector2(0.2f, 0.56f), new Vector2(0.7f, 0.56f) })
            c.Fill(Ellipse(b, 0.025f, 0.025f, 8), InkC);
        return c.Save(dir + "/Padlock.png", new Vector2(0.5f, 0.05f / 1.15f));
    }

    public static Sprite MakeCheckPlate(string dir)
    {
        var c = new InkCanvas(1.2f, 0.75f, 128f, Clear);
        c.Shape(RoundRect(0.05f, 0.05f, 1.15f, 0.7f, 0.08f), Paper, LW * 1.2f, InkC);
        c.Stroke(RoundRect(0.12f, 0.12f, 1.08f, 0.63f, 0.05f), Thin, InkC, true);
        foreach (var b in new[] { new Vector2(0.16f, 0.16f), new Vector2(1.04f, 0.16f), new Vector2(0.16f, 0.59f), new Vector2(1.04f, 0.59f) })
            c.Fill(Ellipse(b, 0.025f, 0.025f, 8), InkC);
        c.Stroke(new[] { new Vector2(0.4f, 0.4f), new Vector2(0.55f, 0.24f), new Vector2(0.82f, 0.55f) }, 0.08f, InkC);
        return c.Save(dir + "/CheckPlate.png", new Vector2(0.5f, 0.5f));
    }

    /// <summary>クリア後に台座に建つ簡易タワー（メーター付きの塔）</summary>
    public static Sprite MakeClearTower(string dir)
    {
        var c = new InkCanvas(1.3f, 2.6f, 128f, Clear);
        c.Shape(Rect(0.2f, 0.02f, 1.1f, 0.6f), Shade, LW, InkC);
        c.Hatch(Rect(0.8f, 0.02f, 1.1f, 0.6f), 75f, 0.07f, Thin, InkC);
        c.Line(new Vector2(0.2f, 0.32f), new Vector2(1.1f, 0.32f), Thin, InkC);
        c.Shape(Rect(0.35f, 0.6f, 0.95f, 1.8f), Paper, LW, InkC);
        c.Hatch(Rect(0.78f, 0.6f, 0.95f, 1.8f), 80f, 0.07f, Thin, InkC);
        // メーター
        var dial = new Vector2(0.65f, 1.35f);
        c.Shape(Ellipse(dial, 0.23f, 0.23f, 24), Paper, LW, InkC);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            c.Line(dial + d * 0.16f, dial + d * 0.2f, Thin, InkC);
        }
        c.Line(dial, dial + new Vector2(0.1f, 0.1f), Thin * 1.5f, InkC);
        c.Shape(RoundRect(0.47f, 0.75f, 0.83f, 1.0f, 0.03f), Light, Thin, InkC);
        // 屋根とアンテナ
        c.Shape(Rect(0.25f, 1.8f, 1.05f, 2.0f), Shade, LW, InkC);
        c.Line(new Vector2(0.65f, 2.0f), new Vector2(0.65f, 2.42f), LW, InkC);
        c.Shape(Ellipse(new Vector2(0.65f, 2.47f), 0.07f, 0.07f, 12), Paper, Thin, InkC);
        // 横のパイプ
        var pipe = new List<Vector2> { new Vector2(0.95f, 1.6f), new Vector2(1.15f, 1.6f), new Vector2(1.15f, 0.6f) };
        c.Stroke(pipe, 0.12f, InkC);
        c.Stroke(pipe, 0.06f, Paper);
        return c.Save(dir + "/ClearTower.png", new Vector2(0.5f, 0.02f / 2.6f));
    }

    // ─── 看板 ─────────────────────────────────────────────────────
    public static readonly Vector2 SignBoardCenter = new Vector2(0f, 2.05f); // pivot（柱の根元）からの看板の中心
    public static readonly Vector2 SignBoardSize = new Vector2(3.0f, 1.25f);  // 文字を入れる範囲

    public static Sprite MakeSign(string dir)
    {
        float w = 3.8f, h = 3.3f;
        var c = new InkCanvas(w, h, 128f, Clear);
        float cx = w * 0.5f, by = 2.05f + 0.05f;
        // 柱
        c.Shape(Rect(cx - 0.3f, 0.05f, cx + 0.3f, 0.2f), Shade, LW, InkC);
        c.Stroke(new[] { new Vector2(cx, 0.2f), new Vector2(cx, by - 0.8f) }, 0.22f, InkC);
        c.Stroke(new[] { new Vector2(cx, 0.22f), new Vector2(cx, by - 0.82f) }, 0.13f, Paper);
        c.Shape(Rect(cx - 0.16f, 0.6f, cx + 0.16f, 0.68f), Shade, Thin, InkC);
        // 枠
        var outer = RoundRect(cx - 1.75f, by - 0.85f, cx + 1.75f, by + 0.8f, 0.14f);
        c.Shape(outer, Shade, LW * 1.4f, InkC);
        c.Shape(Rect(cx - 1.58f, by - 0.7f, cx + 1.58f, by + 0.65f), Paper, LW, InkC);
        foreach (var b in new[] { new Vector2(-1.66f, -0.76f), new Vector2(1.66f, -0.76f), new Vector2(-1.66f, 0.71f), new Vector2(1.66f, 0.71f) })
            c.Shape(Ellipse(new Vector2(cx, by) + b, 0.05f, 0.05f, 10), Paper, Thin, InkC);
        // 上の取っ手
        var handle = new List<Vector2> { new Vector2(cx - 0.3f, by + 0.8f), new Vector2(cx - 0.3f, by + 0.98f), new Vector2(cx + 0.3f, by + 0.98f), new Vector2(cx + 0.3f, by + 0.8f) };
        c.Stroke(handle, 0.14f, InkC);
        c.Stroke(handle, 0.07f, Paper);
        return c.Save(dir + "/Sign.png", new Vector2(0.5f, 0.05f / h));
    }

    // ─── プレイヤー（潜水ヘルメットのロボット） ───────────────────────
    public static Sprite MakeRobot(string dir)
    {
        float w = 1.1f, h = 1.35f;
        var c = new InkCanvas(w, h, 160f, Clear);
        float cx = 0.55f;
        c.Fill(Ellipse(new Vector2(cx, 0.06f), 0.34f, 0.07f, 20), new Color(0f, 0f, 0f, 0.18f));
        // ボンベ（背中）
        c.Shape(RoundRect(0.12f, 0.24f, 0.32f, 0.62f, 0.07f), Shade, LW, InkC);
        // 足
        c.Shape(RoundRect(0.3f, 0.03f, 0.52f, 0.16f, 0.04f), Dark, Thin, InkC);
        c.Shape(RoundRect(0.6f, 0.03f, 0.82f, 0.16f, 0.04f), Dark, Thin, InkC);
        c.Shape(Rect(0.36f, 0.14f, 0.48f, 0.26f), Shade, Thin, InkC);
        c.Shape(Rect(0.64f, 0.14f, 0.76f, 0.26f), Shade, Thin, InkC);
        // 胴体
        c.Shape(RoundRect(0.28f, 0.22f, 0.84f, 0.6f, 0.1f), Paper, LW, InkC);
        c.Line(new Vector2(0.3f, 0.33f), new Vector2(0.82f, 0.33f), Thin, InkC);
        c.Shape(RoundRect(0.5f, 0.38f, 0.68f, 0.52f, 0.02f), Light, Thin, InkC);
        // 腕
        c.Shape(RoundRect(0.18f, 0.28f, 0.32f, 0.5f, 0.06f), Paper, LW * 0.9f, InkC);
        c.Shape(RoundRect(0.8f, 0.28f, 0.95f, 0.5f, 0.06f), Paper, LW * 0.9f, InkC);
        // 首の輪
        c.Shape(Ellipse(new Vector2(cx, 0.6f), 0.3f, 0.07f, 24), Shade, LW, InkC);
        // ヘルメット
        var head = new Vector2(cx, 0.92f);
        c.Shape(Ellipse(head, 0.36f, 0.34f, 40), Paper, LW * 1.2f, InkC);
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f + 10f) * Mathf.Deg2Rad;
            c.Fill(Ellipse(head + new Vector2(Mathf.Cos(a) * 0.31f, Mathf.Sin(a) * 0.29f), 0.022f, 0.022f, 8), InkC);
        }
        // 窓（右向き）
        var visor = head + new Vector2(0.07f, -0.01f);
        c.Shape(RoundRect(visor.x - 0.2f, visor.y - 0.17f, visor.x + 0.2f, visor.y + 0.17f, 0.12f), Shade, LW, InkC);
        c.Shape(RoundRect(visor.x - 0.15f, visor.y - 0.12f, visor.x + 0.15f, visor.y + 0.12f, 0.09f), Dark, Thin, InkC);
        c.Stroke(Ellipse(visor + new Vector2(-0.03f, 0.02f), 0.08f, 0.06f, 10, 100f, 170f), 0.03f, Paper);
        c.Line(visor + new Vector2(-0.12f, 0f), visor + new Vector2(0.12f, 0f), Thin, InkC);
        c.Line(visor + new Vector2(0f, -0.1f), visor + new Vector2(0f, 0.1f), Thin, InkC);
        return c.Save(dir + "/Robot.png", new Vector2(0.5f, 0.06f / h));
    }

    // ─── 建物 ─────────────────────────────────────────────────────
    public static void ConeTower(InkCanvas c, float cx, float y0, float w, float body, float roof, System.Random rnd)
    {
        c.Shape(Rect(cx - w * 0.5f, y0, cx + w * 0.5f, y0 + body), Paper, LW, InkC);
        c.Hatch(Rect(cx + w * 0.18f, y0, cx + w * 0.5f, y0 + body), 80f, 0.08f, Thin, InkC);
        c.Shape(RoundRect(cx - 0.07f, y0 + body * 0.55f, cx + 0.07f, y0 + body * 0.75f, 0.06f), Dark, Thin, InkC);
        var cone = new List<Vector2> { new Vector2(cx - w * 0.5f - 0.1f, y0 + body), new Vector2(cx + w * 0.5f + 0.1f, y0 + body), new Vector2(cx, y0 + body + roof) };
        c.Fill(cone, Paper);
        c.Hatch(new List<Vector2> { new Vector2(cx, y0 + body + roof), new Vector2(cx + w * 0.5f + 0.1f, y0 + body), new Vector2(cx + 0.05f, y0 + body) }, 70f, 0.07f, Thin, InkC);
        c.Stroke(cone, LW, InkC, true);
        c.Line(new Vector2(cx, y0 + body + roof), new Vector2(cx, y0 + body + roof + 0.3f), Thin, InkC);
        c.Fill(new List<Vector2> { new Vector2(cx, y0 + body + roof + 0.3f), new Vector2(cx + 0.22f, y0 + body + roof + 0.24f), new Vector2(cx, y0 + body + roof + 0.17f) }, InkC);
    }

    public static void Merlons(InkCanvas c, float x0, float x1, float y)
    {
        for (float x = x0; x + 0.2f <= x1; x += 0.36f)
            c.Shape(Rect(x, y, x + 0.2f, y + 0.18f), Paper, Thin * 1.3f, InkC);
    }

    public static Sprite MakeCastle(string dir)
    {
        var rnd = new System.Random(21);
        float w = 7.6f, h = 5.6f, y0 = 0.25f;
        var c = new InkCanvas(w, h, 96f, Clear);
        // 奥の塔
        ConeTower(c, 2.3f, y0 + 1.2f, 0.75f, 2.0f, 1.0f, rnd);
        ConeTower(c, 5.3f, y0 + 1.2f, 0.75f, 2.0f, 1.0f, rnd);
        // 本丸
        c.Shape(Rect(2.9f, y0 + 0.8f, 4.7f, y0 + 3.1f), Paper, LW, InkC);
        c.Hatch(Rect(4.2f, y0 + 0.8f, 4.7f, y0 + 3.1f), 80f, 0.08f, Thin, InkC);
        Merlons(c, 2.9f, 4.7f, y0 + 3.1f);
        for (int i = 0; i < 3; i++)
            c.Shape(RoundRect(3.2f + i * 0.5f, y0 + 2.2f, 3.38f + i * 0.5f, y0 + 2.6f, 0.08f), Dark, Thin, InkC);
        ConeTower(c, 3.8f, y0 + 3.28f, 0.6f, 0.6f, 1.1f, rnd);
        // 城壁
        c.Shape(Rect(0.6f, y0, 7.0f, y0 + 1.5f), Paper, LW, InkC);
        Merlons(c, 0.6f, 7.0f, y0 + 1.5f);
        for (int i = 0; i < 26; i++)
        {
            float x = R(rnd, 0.8f, 6.6f), y = R(rnd, y0 + 0.2f, y0 + 1.3f);
            c.Line(new Vector2(x, y), new Vector2(x + R(rnd, 0.15f, 0.3f), y), Thin, InkC);
        }
        // 門
        var gate = new List<Vector2>(Ellipse(new Vector2(3.8f, y0 + 0.55f), 0.35f, 0.35f, 16, 0f, 180f));
        gate.Add(new Vector2(3.45f, y0)); gate.Insert(0, new Vector2(4.15f, y0));
        c.Shape(gate, Dark, LW, InkC);
        // 手前の塔
        ConeTower(c, 0.75f, y0, 0.8f, 2.3f, 1.1f, rnd);
        ConeTower(c, 6.85f, y0, 0.8f, 2.3f, 1.1f, rnd);
        return c.Save(dir + "/Castle.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeWindmill(string dir)
    {
        float w = 2.4f, h = 4.0f, y0 = 0.2f;
        var c = new InkCanvas(w, h, 96f, Clear);
        var body = new List<Vector2> { new Vector2(0.55f, y0), new Vector2(1.85f, y0), new Vector2(1.55f, 3.0f), new Vector2(0.85f, 3.0f) };
        c.Fill(body, Paper);
        for (float y = y0 + 0.3f; y < 3.0f; y += 0.28f)
        {
            float k = (y - y0) / (3.0f - y0);
            c.Line(new Vector2(Mathf.Lerp(0.55f, 0.85f, k), y), new Vector2(Mathf.Lerp(1.85f, 1.55f, k), y), Thin * 0.8f, InkC);
        }
        c.Hatch(new List<Vector2> { new Vector2(1.45f, y0), new Vector2(1.85f, y0), new Vector2(1.55f, 3.0f), new Vector2(1.35f, 3.0f) }, 78f, 0.07f, Thin, InkC);
        c.Stroke(body, LW, InkC, true);
        var door = new List<Vector2>(Ellipse(new Vector2(1.2f, y0 + 0.45f), 0.2f, 0.2f, 12, 0f, 180f));
        door.Add(new Vector2(1.0f, y0)); door.Insert(0, new Vector2(1.4f, y0));
        c.Shape(door, Dark, Thin, InkC);
        c.Shape(RoundRect(1.1f, 1.6f, 1.3f, 1.9f, 0.08f), Dark, Thin, InkC);
        var cap = new List<Vector2>(Ellipse(new Vector2(1.2f, 3.0f), 0.48f, 0.5f, 20, 0f, 180f));
        c.Shape(cap, Paper, LW, InkC);
        c.Hatch(new List<Vector2>(Ellipse(new Vector2(1.2f, 3.0f), 0.48f, 0.5f, 20, 0f, 70f)) { new Vector2(1.2f, 3.0f) }, 70f, 0.07f, Thin, InkC);
        return c.Save(dir + "/Windmill.png", new Vector2(0.5f, y0 / h));
    }
    public const float WindmillHubHeight = 3.05f - 0.2f;

    public static Sprite MakeBlades(string dir)
    {
        float s = 4.6f;
        var c = new InkCanvas(s, s, 96f, Clear);
        var ctr = new Vector2(s * 0.5f, s * 0.5f);
        for (int k = 0; k < 4; k++)
        {
            float a = (45f + 90f * k) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var n = new Vector2(-d.y, d.x);
            c.Line(ctr, ctr + d * 2.15f, 0.07f, InkC);
            var sail = new List<Vector2> { ctr + d * 0.4f, ctr + d * 2.1f, ctr + d * 2.1f + n * 0.48f, ctr + d * 0.4f + n * 0.48f };
            c.Shape(sail, Paper, LW, InkC);
            for (float t = 0.6f; t < 2.1f; t += 0.25f) c.Line(ctr + d * t, ctr + d * t + n * 0.48f, Thin, InkC);
            c.Line(ctr + d * 0.4f + n * 0.24f, ctr + d * 2.1f + n * 0.24f, Thin, InkC);
        }
        c.Shape(Ellipse(ctr, 0.14f, 0.14f, 14), Shade, LW, InkC);
        return c.Save(dir + "/Blades.png", new Vector2(0.5f, 0.5f));
    }

    public static Sprite MakeChurch(string dir)
    {
        var rnd = new System.Random(4);
        float w = 2.4f, h = 3.2f, y0 = 0.15f;
        var c = new InkCanvas(w, h, 96f, Clear);
        c.Shape(Rect(0.25f, y0, 1.55f, y0 + 1.0f), Paper, LW, InkC);
        var roof = new List<Vector2> { new Vector2(0.12f, y0 + 1.0f), new Vector2(1.68f, y0 + 1.0f), new Vector2(0.9f, y0 + 1.75f) };
        c.Fill(roof, Paper);
        c.Hatch(roof, 30f, 0.09f, Thin, InkC);
        c.Stroke(roof, LW, InkC, true);
        c.Shape(RoundRect(0.75f, y0, 1.05f, y0 + 0.5f, 0.14f), Dark, Thin, InkC);
        c.Shape(Ellipse(new Vector2(0.9f, y0 + 0.78f), 0.1f, 0.1f, 12), Dark, Thin, InkC);
        ConeTower(c, 1.85f, y0, 0.5f, 1.8f, 0.9f, rnd);
        c.Line(new Vector2(1.85f, y0 + 2.7f), new Vector2(1.85f, y0 + 3.0f), LW, InkC);
        c.Line(new Vector2(1.75f, y0 + 2.9f), new Vector2(1.95f, y0 + 2.9f), LW, InkC);
        return c.Save(dir + "/Church.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeHouse(string dir)
    {
        float w = 1.8f, h = 1.7f, y0 = 0.1f;
        var c = new InkCanvas(w, h, 96f, Clear);
        c.Shape(Rect(0.25f, y0, 1.55f, y0 + 0.7f), Paper, LW, InkC);
        var roof = new List<Vector2> { new Vector2(0.1f, y0 + 0.7f), new Vector2(1.7f, y0 + 0.7f), new Vector2(1.45f, y0 + 1.3f), new Vector2(0.35f, y0 + 1.3f) };
        c.Fill(roof, Paper);
        c.Hatch(roof, 85f, 0.08f, Thin, InkC);
        c.Stroke(roof, LW, InkC, true);
        c.Shape(Rect(0.45f, y0, 0.7f, y0 + 0.45f), Dark, Thin, InkC);
        c.Shape(Rect(1.0f, y0 + 0.3f, 1.3f, y0 + 0.55f), Light, Thin, InkC);
        c.Shape(Rect(1.2f, y0 + 1.1f, 1.35f, y0 + 1.5f), Paper, Thin, InkC);
        return c.Save(dir + "/House.png", new Vector2(0.5f, y0 / h));
    }

    public static void Puff(InkCanvas c, Vector2 at, float s, System.Random rnd)
    {
        var blobs = new (float x, float y, float r)[] { (0f, 0f, 0.28f), (0.3f, 0.12f, 0.32f), (0.62f, 0.3f, 0.36f), (0.25f, 0.4f, 0.26f), (0.95f, 0.55f, 0.3f) };
        foreach (var b in blobs)
            c.Shape(Ellipse(at + new Vector2(b.x, b.y) * s, b.r * s, b.r * s * 0.85f, 20), Paper, Thin * 1.4f, InkC);
    }

    public static void Tube(InkCanvas c, IList<Vector2> pts, float width)
    {
        c.Stroke(pts, width, InkC);
        c.Stroke(pts, width - LW * 2f, Paper);
    }

    public static Sprite MakeFactory(string dir)
    {
        var rnd = new System.Random(33);
        float w = 11f, h = 7.2f, y0 = 0.3f;
        var c = new InkCanvas(w, h, 80f, Clear);
        // 煙突（奥）
        float[] chimneys = { 4.9f, 5.9f, 8.7f, 9.7f };
        float[] tops = { 5.4f, 5.9f, 5.6f, 6.1f };
        for (int i = 0; i < chimneys.Length; i++)
        {
            float x = chimneys[i];
            c.Shape(Rect(x - 0.2f, y0 + 2.0f, x + 0.2f, tops[i]), Paper, LW, InkC);
            c.Hatch(Rect(x + 0.05f, y0 + 2.0f, x + 0.2f, tops[i]), 80f, 0.07f, Thin, InkC);
            c.Shape(Rect(x - 0.26f, tops[i] - 0.15f, x + 0.26f, tops[i]), Shade, Thin, InkC);
            c.Line(new Vector2(x - 0.2f, tops[i] - 0.7f), new Vector2(x + 0.2f, tops[i] - 0.7f), Thin, InkC);
        }
        // 丸屋根の建物
        c.Shape(Rect(0.4f, y0, 2.5f, y0 + 1.9f), Paper, LW, InkC);
        var dome = new List<Vector2>(Ellipse(new Vector2(1.45f, y0 + 1.9f), 0.85f, 0.75f, 24, 0f, 180f));
        c.Shape(dome, Paper, LW, InkC);
        for (int i = 1; i < 4; i++)
            c.Stroke(Ellipse(new Vector2(1.45f, y0 + 1.9f), 0.85f * i / 4f, 0.75f, 12, 0f, 180f), Thin, InkC);
        for (int i = 0; i < 3; i++)
            c.Shape(RoundRect(0.65f + i * 0.6f, y0 + 0.6f, 0.9f + i * 0.6f, y0 + 1.3f, 0.12f), Dark, Thin, InkC);
        // 本館
        c.Shape(Rect(2.4f, y0, 7.6f, y0 + 2.5f), Paper, LW, InkC);
        c.Hatch(Rect(6.9f, y0, 7.6f, y0 + 2.5f), 80f, 0.08f, Thin, InkC);
        for (int i = 0; i < 7; i++)
            c.Shape(RoundRect(2.7f + i * 0.62f, y0 + 0.5f, 3.0f + i * 0.62f, y0 + 1.5f, 0.14f), Dark, Thin, InkC);
        c.Shape(Rect(3.3f, y0 + 2.5f, 4.6f, y0 + 3.5f), Paper, LW, InkC);
        for (int i = 0; i < 2; i++)
            c.Shape(RoundRect(3.55f + i * 0.6f, y0 + 2.75f, 3.8f + i * 0.6f, y0 + 3.2f, 0.1f), Dark, Thin, InkC);
        // 右の橋脚（アーチ）
        c.Shape(Rect(7.6f, y0, 10.8f, y0 + 1.7f), Paper, LW, InkC);
        for (int i = 0; i < 4; i++)
        {
            float x = 8.0f + i * 0.75f;
            var arch = new List<Vector2>(Ellipse(new Vector2(x, y0 + 0.9f), 0.25f, 0.3f, 12, 0f, 180f));
            arch.Add(new Vector2(x - 0.25f, y0)); arch.Insert(0, new Vector2(x + 0.25f, y0));
            c.Shape(arch, Dark, Thin, InkC);
        }
        // 配管
        Tube(c, new[] { new Vector2(1.9f, y0 + 2.2f), new Vector2(10.4f, y0 + 2.2f) }, 0.2f);
        Tube(c, new[] { new Vector2(10.4f, y0 + 2.2f), new Vector2(10.4f, y0 + 1.7f) }, 0.2f);
        Tube(c, new[] { new Vector2(2.2f, y0 + 2.2f), new Vector2(2.2f, y0 + 3.0f), new Vector2(3.3f, y0 + 3.0f) }, 0.16f);
        for (float x = 2.6f; x < 10.2f; x += 1.1f) c.Shape(Rect(x, y0 + 2.08f, x + 0.08f, y0 + 2.32f), Shade, Thin, InkC);
        // 煙
        for (int i = 0; i < chimneys.Length; i++) Puff(c, new Vector2(chimneys[i] - 0.2f, tops[i] + 0.25f), 0.75f, rnd);
        return c.Save(dir + "/Factory.png", new Vector2(0.5f, y0 / h));
    }
    public static readonly Vector2 FactoryGearOffset = new Vector2(2.6f, 2.9f); // 足元中心からの大歯車の位置

    public static Sprite MakeBigGear(string dir)
    {
        float s = 3.0f;
        var c = new InkCanvas(s, s, 96f, Clear);
        var ctr = new Vector2(s * 0.5f, s * 0.5f);
        c.Shape(Gear(ctr, 1.4f, 1.2f, 16), Paper, LW * 1.3f, InkC);
        c.Stroke(Ellipse(ctr, 1.05f, 1.05f, 40), LW, InkC, true);
        c.Stroke(Ellipse(ctr, 0.9f, 0.9f, 40), Thin, InkC, true);
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var n = new Vector2(-d.y, d.x) * 0.08f;
            c.Shape(new List<Vector2> { ctr + d * 0.28f - n, ctr + d * 0.92f - n, ctr + d * 0.92f + n, ctr + d * 0.28f + n }, Shade, Thin, InkC);
        }
        c.Shape(Ellipse(ctr, 0.3f, 0.3f, 20), Paper, LW, InkC);
        c.Shape(Ellipse(ctr, 0.1f, 0.1f, 12), Dark, Thin, InkC);
        return c.Save(dir + "/BigGear.png", new Vector2(0.5f, 0.5f));
    }

    public static Sprite MakeGate(string dir)
    {
        float w = 6.8f, h = 4.2f, y0 = 0.2f;
        var c = new InkCanvas(w, h, 96f, Clear);
        // 扉
        c.Shape(Rect(1.2f, y0, 5.6f, y0 + 2.4f), Light, LW * 1.3f, InkC);
        c.Line(new Vector2(3.4f, y0), new Vector2(3.4f, y0 + 2.4f), LW, InkC);
        foreach (float x0 in new[] { 1.2f, 3.4f })
        {
            c.Stroke(Rect(x0 + 0.18f, y0 + 0.18f, x0 + 2.02f, y0 + 2.22f), Thin * 1.3f, InkC, true);
            c.Hatch(Rect(x0 + 0.18f, y0 + 0.18f, x0 + 2.02f, y0 + 2.22f), 60f, 0.35f, Thin, InkC);
            for (float x = x0 + 0.1f; x < x0 + 2.2f; x += 0.3f)
            {
                c.Fill(Ellipse(new Vector2(x, y0 + 0.08f), 0.025f, 0.025f, 8), InkC);
                c.Fill(Ellipse(new Vector2(x, y0 + 2.32f), 0.025f, 0.025f, 8), InkC);
            }
        }
        c.Shape(Gear(new Vector2(3.4f, y0 + 1.25f), 0.45f, 0.36f, 10), Paper, LW, InkC);
        c.Shape(Ellipse(new Vector2(3.4f, y0 + 1.25f), 0.18f, 0.18f, 16), Shade, Thin, InkC);
        // 上の管
        Tube(c, Smooth(new[] { new Vector2(1.0f, y0 + 2.7f), new Vector2(3.4f, y0 + 2.9f), new Vector2(5.8f, y0 + 2.7f) }, 8), 0.24f);
        // 柱
        foreach (float px in new[] { 0.65f, 6.15f })
        {
            c.Shape(Rect(px - 0.5f, y0, px + 0.5f, y0 + 2.9f), Paper, LW * 1.2f, InkC);
            c.Hatch(Rect(px + 0.15f, y0, px + 0.5f, y0 + 2.9f), 80f, 0.08f, Thin, InkC);
            for (int i = 0; i < 3; i++)
                c.Shape(Rect(px - 0.56f, y0 + 0.6f + i * 0.9f, px + 0.56f, y0 + 0.72f + i * 0.9f), Shade, Thin, InkC);
            c.Shape(Rect(px - 0.6f, y0 + 2.9f, px + 0.6f, y0 + 3.08f), Shade, LW, InkC);
            c.Shape(new List<Vector2>(Ellipse(new Vector2(px, y0 + 3.08f), 0.45f, 0.45f, 20, 0f, 180f)), Paper, LW, InkC);
            c.Hatch(new List<Vector2>(Ellipse(new Vector2(px, y0 + 3.08f), 0.45f, 0.45f, 12, 0f, 60f)) { new Vector2(px, y0 + 3.08f) }, 70f, 0.07f, Thin, InkC);
        }
        return c.Save(dir + "/Gate.png", new Vector2(0.5f, y0 / h));
    }

    public static Sprite MakeTownArch(string dir)
    {
        float w = 2.8f, h = 2.8f, y0 = 0.1f;
        var c = new InkCanvas(w, h, 96f, Clear);
        foreach (float px in new[] { 0.45f, 2.35f })
        {
            c.Shape(Rect(px - 0.13f, y0, px + 0.13f, y0 + 2.0f), Paper, LW, InkC);
            c.Hatch(Rect(px + 0.03f, y0, px + 0.13f, y0 + 2.0f), 80f, 0.06f, Thin, InkC);
        }
        var roof = new List<Vector2> { new Vector2(0.1f, y0 + 1.95f), new Vector2(2.7f, y0 + 1.95f), new Vector2(2.4f, y0 + 2.5f), new Vector2(0.4f, y0 + 2.5f) };
        c.Fill(roof, Paper);
        c.Hatch(roof, 85f, 0.08f, Thin, InkC);
        c.Stroke(roof, LW, InkC, true);
        c.Shape(Rect(0.3f, y0 + 1.7f, 2.5f, y0 + 1.85f), Shade, Thin, InkC);
        c.Line(new Vector2(1.4f, y0 + 1.7f), new Vector2(1.4f, y0 + 1.45f), Thin, InkC);
        c.Shape(RoundRect(1.28f, y0 + 1.15f, 1.52f, y0 + 1.45f, 0.05f), Paper, Thin, InkC);
        c.Fill(Ellipse(new Vector2(1.4f, y0 + 1.3f), 0.05f, 0.07f, 10), InkC);
        return c.Save(dir + "/TownArch.png", new Vector2(0.5f, y0 / h));
    }

    /// <summary>UIの星（白で塗りつぶし、色は Image で付ける）</summary>
    public static Sprite MakeStar(string dir)
    {
        var c = new InkCanvas(1f, 1f, 128f, new Color(1f, 1f, 1f, 0f));
        var pts = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            float a = (90f + i * 36f) * Mathf.Deg2Rad;
            float r = i % 2 == 0 ? 0.47f : 0.2f;
            pts.Add(new Vector2(0.5f + Mathf.Cos(a) * r, 0.47f + Mathf.Sin(a) * r));
        }
        c.Fill(pts, Color.white);
        return c.Save(dir + "/Star.png", new Vector2(0.5f, 0.5f));
    }
}
