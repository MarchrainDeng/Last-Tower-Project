using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 線画（白黒のペン画）風の絵をコードで描くための小さなキャンバス
/// 座標はユニット（左下が原点）。描いたものは PNG に書き出して Sprite として読み込む
/// </summary>
public class InkCanvas
{
    public readonly int W, H;
    public readonly float Ppu;
    readonly Color[] buf;

    public InkCanvas(float widthUnits, float heightUnits, float ppu, Color background)
    {
        Ppu = ppu;
        W = Mathf.CeilToInt(widthUnits * ppu);
        H = Mathf.CeilToInt(heightUnits * ppu);
        buf = new Color[W * H];
        for (int i = 0; i < buf.Length; i++) buf[i] = background;
    }

    // ─── 基本 ─────────────────────────────────────────────────────
    void Blend(int i, Color c, float cov)
    {
        float sa = c.a * cov;
        if (sa <= 0f) return;
        Color d = buf[i];
        float oa = sa + d.a * (1f - sa);
        if (oa <= 0f) return;
        float k = d.a * (1f - sa);
        buf[i] = new Color((c.r * sa + d.r * k) / oa, (c.g * sa + d.g * k) / oa, (c.b * sa + d.b * k) / oa, oa);
    }

    void Bounds(Vector2 min, Vector2 max, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = Mathf.Max(0, Mathf.FloorToInt(min.x * Ppu) - 1);
        y0 = Mathf.Max(0, Mathf.FloorToInt(min.y * Ppu) - 1);
        x1 = Mathf.Min(W - 1, Mathf.CeilToInt(max.x * Ppu) + 1);
        y1 = Mathf.Min(H - 1, Mathf.CeilToInt(max.y * Ppu) + 1);
    }

    /// <summary>折れ線（closed なら閉じる）を太さ width で描く。つなぎ目が二重に濃くならないよう一度にまとめて塗る</summary>
    public void Stroke(IList<Vector2> pts, float width, Color color, bool closed = false)
    {
        if (pts == null || pts.Count < 2) return;
        float half = width * 0.5f;
        Vector2 min = pts[0], max = pts[0];
        foreach (var p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Bounds(min - Vector2.one * half, max + Vector2.one * half, out int bx0, out int by0, out int bx1, out int by1);
        if (bx1 < bx0 || by1 < by0) return;
        int bw = bx1 - bx0 + 1, bh = by1 - by0 + 1;
        var cov = new float[bw * bh];

        int segs = closed ? pts.Count : pts.Count - 1;
        for (int s = 0; s < segs; s++)
        {
            Vector2 a = pts[s], b = pts[(s + 1) % pts.Count];
            Bounds(Vector2.Min(a, b) - Vector2.one * half, Vector2.Max(a, b) + Vector2.one * half, out int x0, out int y0, out int x1, out int y1);
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            for (int y = y0; y <= y1; y++)
            {
                float py = (y + 0.5f) / Ppu;
                for (int x = x0; x <= x1; x++)
                {
                    var p = new Vector2((x + 0.5f) / Ppu, py);
                    float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                    float d = Vector2.Distance(p, a + ab * t);
                    float c = Mathf.Clamp01((half - d) * Ppu + 0.5f);
                    if (c <= 0f) continue;
                    int idx = (y - by0) * bw + (x - bx0);
                    if (c > cov[idx]) cov[idx] = c;
                }
            }
        }

        for (int y = 0; y < bh; y++)
            for (int x = 0; x < bw; x++)
            {
                float c = cov[y * bw + x];
                if (c > 0f) Blend((y + by0) * W + (x + bx0), color, c);
            }
    }

    public void Line(Vector2 a, Vector2 b, float width, Color color)
    {
        Stroke(new[] { a, b }, width, color);
    }

    /// <summary>多角形を塗る（1ピクセルを4点で調べて縁をなめらかにする）</summary>
    public void Fill(IList<Vector2> poly, Color color)
    {
        if (poly == null || poly.Count < 3) return;
        Vector2 min = poly[0], max = poly[0];
        foreach (var p in poly) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Bounds(min, max, out int x0, out int y0, out int x1, out int y1);
        if (x1 < x0 || y1 < y0) return;
        float q = 0.25f / Ppu;
        int bw = x1 - x0 + 1;
        var count = new int[bw];
        var cross = new List<float>();

        // 1ピクセルを4点（上下2行×左右2列）で調べる。行ごとに辺との交点を求めて左から数える（大きな多角形でも速い）
        for (int y = y0; y <= y1; y++)
        {
            System.Array.Clear(count, 0, bw);
            float py = (y + 0.5f) / Ppu;
            for (int row = 0; row < 2; row++)
            {
                float sy = row == 0 ? py - q : py + q;
                cross.Clear();
                for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                {
                    Vector2 a = poly[i], b = poly[j];
                    if ((a.y > sy) != (b.y > sy)) cross.Add((b.x - a.x) * (sy - a.y) / (b.y - a.y) + a.x);
                }
                if (cross.Count == 0) continue;
                cross.Sort();
                for (int col = 0; col < 2; col++)
                {
                    int k = 0;
                    for (int x = x0; x <= x1; x++)
                    {
                        float sx = (x + 0.5f) / Ppu + (col == 0 ? -q : q);
                        while (k < cross.Count && cross[k] <= sx) k++;
                        // 右側に残っている交点の数が奇数なら内側
                        if (((cross.Count - k) & 1) == 1) count[x - x0]++;
                    }
                }
            }
            for (int x = 0; x < bw; x++)
                if (count[x] > 0) Blend(y * W + x + x0, color, count[x] * 0.25f);
        }
    }

    /// <summary>塗り＋輪郭</summary>
    public void Shape(IList<Vector2> poly, Color fill, float lineWidth, Color line)
    {
        Fill(poly, fill);
        Stroke(poly, lineWidth, line, true);
    }

    /// <summary>多角形の中に平行な斜線を引く（影の表現）</summary>
    public void Hatch(IList<Vector2> poly, float angleDeg, float spacing, float width, Color color, System.Random rnd = null, float shrink = 0f)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        var d = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        var n = new Vector2(-d.y, d.x);
        float min = float.MaxValue, max = float.MinValue;
        foreach (var p in poly) { float s = Vector2.Dot(p, n); min = Mathf.Min(min, s); max = Mathf.Max(max, s); }

        var ts = new List<float>();
        for (float s = min + spacing * 0.5f; s < max; s += spacing)
        {
            ts.Clear();
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                float sa = Vector2.Dot(a, n) - s, sb = Vector2.Dot(b, n) - s;
                if ((sa < 0f) == (sb < 0f)) continue;
                float k = sa / (sa - sb);
                ts.Add(Vector2.Dot(a + (b - a) * k, d));
            }
            ts.Sort();
            for (int i = 0; i + 1 < ts.Count; i += 2)
            {
                float t0 = ts[i], t1 = ts[i + 1];
                if (rnd != null && shrink > 0f)
                {
                    t0 += (float)rnd.NextDouble() * shrink;
                    t1 -= (float)rnd.NextDouble() * shrink;
                }
                if (t1 - t0 < width) continue;
                Line(n * s + d * t0, n * s + d * t1, width, color);
            }
        }
    }

    static bool Inside(IList<Vector2> poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i], b = poly[j];
            if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }

    // ─── 形 ───────────────────────────────────────────────────────
    public static List<Vector2> Rect(float x0, float y0, float x1, float y1)
    {
        return new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };
    }

    public static List<Vector2> RoundRect(float x0, float y0, float x1, float y1, float r)
    {
        r = Mathf.Min(r, (x1 - x0) * 0.5f, (y1 - y0) * 0.5f);
        var pts = new List<Vector2>();
        void Corner(float cx, float cy, float a0)
        {
            for (int i = 0; i <= 6; i++)
            {
                float a = (a0 + i * 15f) * Mathf.Deg2Rad;
                pts.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
        }
        Corner(x1 - r, y0 + r, 270f);
        Corner(x1 - r, y1 - r, 0f);
        Corner(x0 + r, y1 - r, 90f);
        Corner(x0 + r, y0 + r, 180f);
        return pts;
    }

    public static List<Vector2> Ellipse(Vector2 c, float rx, float ry, int n = 40, float fromDeg = 0f, float toDeg = 360f)
    {
        var pts = new List<Vector2>();
        bool full = Mathf.Approximately(toDeg - fromDeg, 360f);
        int count = full ? n : n + 1;
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, i / (float)n) * Mathf.Deg2Rad;
            pts.Add(new Vector2(c.x + Mathf.Cos(a) * rx, c.y + Mathf.Sin(a) * ry));
        }
        return pts;
    }

    /// <summary>歯車の輪郭。squash で縦につぶす（斜め上から見た台座の上面用）</summary>
    public static List<Vector2> Gear(Vector2 c, float rOut, float rIn, int teeth, float squash = 1f)
    {
        var pts = new List<Vector2>();
        float step = 360f / teeth;
        for (int i = 0; i < teeth; i++)
        {
            float a = i * step;
            float[] angles = { a, a + step * 0.2f, a + step * 0.5f, a + step * 0.7f };
            float[] radii = { rIn, rOut, rOut, rIn };
            for (int k = 0; k < 4; k++)
            {
                float rad = angles[k] * Mathf.Deg2Rad;
                pts.Add(new Vector2(c.x + Mathf.Cos(rad) * radii[k], c.y + Mathf.Sin(rad) * radii[k] * squash));
            }
        }
        return pts;
    }

    /// <summary>4つ角のキラキラ</summary>
    public static List<Vector2> Sparkle(Vector2 c, float r)
    {
        var pts = new List<Vector2>();
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            float rr = i % 2 == 0 ? r : r * 0.22f;
            pts.Add(new Vector2(c.x + Mathf.Sin(a) * rr, c.y + Mathf.Cos(a) * rr));
        }
        return pts;
    }

    /// <summary>なめらかな曲線（Catmull-Rom）</summary>
    public static List<Vector2> Smooth(IList<Vector2> pts, int perSegment = 10)
    {
        var result = new List<Vector2>();
        int n = pts.Count;
        for (int i = 0; i < n - 1; i++)
        {
            Vector2 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(i + 2, n - 1)];
            for (int s = 0; s < perSegment; s++)
            {
                float t = s / (float)perSegment, t2 = t * t, t3 = t2 * t;
                result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        result.Add(pts[n - 1]);
        return result;
    }

    public static List<Vector2> Offset(IList<Vector2> pts, Vector2 by)
    {
        var r = new List<Vector2>(pts.Count);
        foreach (var p in pts) r.Add(p + by);
        return r;
    }

    // ─── 書き出し ─────────────────────────────────────────────────
    /// <summary>PNG に書き出して Sprite として読み込む。pivot は 0〜1</summary>
    public Sprite Save(string path, Vector2 pivot)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color32[buf.Length];
        for (int i = 0; i < buf.Length; i++) px[i] = buf[i];
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Ppu;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
