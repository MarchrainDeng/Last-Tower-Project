using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Art = WorldMapInkArt;
using WArt = WorldSelectInkArt;

/// <summary>
/// ワールド選択マップのシーンを組み立てるエディタツール
/// メニュー Tools/Story/Build World Select Scene で Assets/Scenes/WorldSelectScene.unity を作り直す
///
/// 仕様書（ワールドマップ仕様書）の例イメージに合わせ、1枚の島を7つの地方に分けたペン画にする
/// - 中央：W1 ラボ（最初はここだけ入れる）
/// - 上：W7 NEO（ボス。台地の上）／右上：W2 ロンドン／右下：W3 地中海／下：W6 アマゾン／左下：W4 中東／左上：W5 日本
/// - 中央から各地方へ道が伸びる。地方の境目は点線
/// 絵はすべてコードで描いて Assets/Story/WorldSelect/Art/ に書き出す（ステージマップと共通の絵は Assets/Story/WorldMap/Art/ のものを使う）
///
/// ワールド情報（Assets/Story/WorldSelect/Worlds/*.asset）は無いときだけ仮データを作る
/// 名前・説明・シーン名・解放条件はアセットを直接編集すれば、作り直しても消えない
/// </summary>
public static class WorldSelectSceneBuilder
{
    const string ScenePath = "Assets/Scenes/WorldSelectScene.unity";
    const string ArtDir = "Assets/Story/WorldSelect/Art";
    const string SharedArtDir = "Assets/Story/WorldMap/Art";
    const string WorldDir = "Assets/Story/WorldSelect/Worlds";
    const string StageDir = "Assets/Story/WorldMap/Stages";
    const string FontPath = "Assets/Sasaki/JA_KO SDF.asset";

    static readonly Rect MapRect = new Rect(-20f, -11f, 40f, 22f);
    const float CameraSize = 11f;
    const int OrderGround = -32000, OrderBanner = 30000, OrderCursor = 31000;

    // ─── ワールドの配置 ─────────────────────────────────────────────
    struct WorldSpec
    {
        public string id, name, description, stageMapScene, clearStageId;
        public int number;
        public bool boss;
        public Vector2 position;
        public string[] stageIds, unlockAfter;
    }

    static readonly WorldSpec[] Worlds =
    {
        new WorldSpec { id = "W1", number = 1, name = "ラボ", position = new Vector2(0f, -1.2f),
            description = "すべてが始まった研究所。\nここから世界に色を取り戻す旅が始まる。",
            stageMapScene = "StageMapScene", clearStageId = "1-5", stageIds = new[] { "1-1", "1-2", "1-3", "1-4", "1-5" } },
        new WorldSpec { id = "W2", number = 2, name = "ロンドン", position = new Vector2(12.5f, 3.2f),
            description = "産業革命時代の工業都市。\n自分のブロックを作れる工房がある。\nちょっと空気が汚い。", unlockAfter = new[] { "W1" } },
        new WorldSpec { id = "W3", number = 3, name = "地中海", position = new Vector2(12.5f, -5.4f),
            description = "交易の拠点として栄える港町。\n白い家並みと灯台が海を見下ろす。", unlockAfter = new[] { "W2" } },
        new WorldSpec { id = "W4", number = 4, name = "中東・ペルシャ", position = new Vector2(-12.5f, -5.4f),
            description = "砂漠の向こうにそびえる宮殿の都。", unlockAfter = new[] { "W2" } },
        new WorldSpec { id = "W5", number = 5, name = "日本", position = new Vector2(-12.5f, 3.2f),
            description = "京都・嵐山。\n五重塔がそびえる山あいの古都。", unlockAfter = new[] { "W2" } },
        new WorldSpec { id = "W6", number = 6, name = "アマゾン", position = new Vector2(0f, -7.8f),
            description = "大河の流れる密林。\n木々の奥に古い遺跡が眠っている。", unlockAfter = new[] { "W2" } },
        new WorldSpec { id = "W7", number = 7, name = "NEO", boss = true, position = new Vector2(0f, 4.8f),
            description = "台地の上に築かれた未来都市。\nすべての元凶が待っている。", unlockAfter = new[] { "W3", "W4", "W5", "W6" } },
    };

    // 道（中央から各地方へ。見た目だけ）
    static readonly Vector2[][] Roads =
    {
        new[] { new Vector2(0f, -1.2f), new Vector2(0.4f, 0.6f), new Vector2(0f, 2.0f), new Vector2(0f, 4.8f) },
        new[] { new Vector2(0f, -1.2f), new Vector2(4f, -0.2f), new Vector2(8f, 1.8f), new Vector2(12.5f, 3.2f) },
        new[] { new Vector2(0f, -1.2f), new Vector2(4f, -2.8f), new Vector2(8.5f, -4.0f), new Vector2(12.5f, -5.4f) },
        new[] { new Vector2(0f, -1.2f), new Vector2(-0.8f, -3.5f), new Vector2(0.6f, -5.6f), new Vector2(0f, -7.8f) },
        new[] { new Vector2(0f, -1.2f), new Vector2(-4f, -2.8f), new Vector2(-8.5f, -4.2f), new Vector2(-12.5f, -5.4f) },
        new[] { new Vector2(0f, -1.2f), new Vector2(-4f, -0.3f), new Vector2(-8f, 1.6f), new Vector2(-12.5f, 3.2f) },
    };

    // 島の形と入り江（地中海）
    static readonly Vector2 LandRadius = new Vector2(19f, 10.3f);
    static readonly Vector2 BayCenter = new Vector2(17.5f, -9f);
    static readonly Vector2 BayRadius = new Vector2(5f, 3.5f);

    // 中央の地方の境目と、NEO の台地
    static readonly Vector2 CenterRegion = new Vector2(6.5f, 3.6f);
    static readonly Vector2[] PlateauRim = { new Vector2(-6.5f, 3.4f), new Vector2(-3.5f, 3.9f), new Vector2(0f, 3.7f), new Vector2(3.5f, 3.9f), new Vector2(6.5f, 3.4f) };
    const float PlateauDepth = 1.4f;

    // 日本の山（麓の中心、幅、高さ）
    static readonly (Vector2 b, float w, float h)[] Mountains =
    {
        (new Vector2(-13.5f, 7.9f), 5.0f, 2.6f), (new Vector2(-17.2f, 7.2f), 4.4f, 2.8f), (new Vector2(-9.4f, 7.5f), 4.0f, 2.4f),
    };

    static readonly Vector2[] AmazonRiver = { new Vector2(-5.2f, -4.4f), new Vector2(-3.8f, -6.4f), new Vector2(-6.2f, -8.4f), new Vector2(-5.0f, -11.5f) };

    static TMP_FontAsset font;
    static List<Vector2> land;
    static List<Vector2> roadSamples;

    static Sprite ground, burst, padlock, house, pointer, flag, fog, banner, ship, torii, star;
    static Sprite[] worldArt;
    static Sprite[] pines, palms, jungleTrees;

    [MenuItem("Tools/Story/Build World Select Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(SharedArtDir);
        land = MakeLand();
        roadSamples = new List<Vector2>();
        foreach (var r in Roads) roadSamples.AddRange(InkCanvas.Smooth(r, 12));

        try
        {
            EditorUtility.DisplayProgressBar("World Select", "絵を描いています…", 0.1f);
            DrawArt();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        var worlds = CreateWorldAssets();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // カメラ（固定。マップ全体を映す）
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Art.Light;

        var light = new GameObject("Global Light 2D", typeof(Light2D)).GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // ── 地面 ──
        var map = new GameObject("Map").transform;
        Place(map, "Ground", ground, MapRect.min, false).GetComponent<SpriteRenderer>().sortingOrder = OrderGround;

        // ── ワールド ──
        var nodesRoot = new GameObject("Worlds").transform;
        var nodes = new WorldSelectNode[Worlds.Length];
        for (int i = 0; i < Worlds.Length; i++)
            nodes[i] = MakeNode(nodesRoot, worlds[i], worldArt[i], Worlds[i].position);

        // ── 飾り ──
        BuildDecorations(map);

        // ── カーソル ──
        var cursorGo = new GameObject("Cursor", typeof(SpriteRenderer));
        var csr = cursorGo.GetComponent<SpriteRenderer>();
        csr.sprite = pointer;
        csr.sortingOrder = OrderCursor;
        cursorGo.transform.position = nodes[0].CursorPosition;

        // ── UI ──
        var canvas = MakeCanvas();
        var detail = MakeDetailPanel(canvas);
        MakeHud(canvas);

        // ── コントローラー ──
        var ctrlGo = new GameObject("WorldSelectController", typeof(AudioSource), typeof(WorldSelectController));
        var ctrl = ctrlGo.GetComponent<WorldSelectController>();
        ctrl.nodes = nodes;
        ctrl.defaultNode = nodes[0];
        ctrl.detailPanel = detail;
        ctrl.cam = cam;
        ctrl.cursor = cursorGo.transform;
        ctrl.audioSource = ctrlGo.GetComponent<AudioSource>();
        ctrl.audioSource.playOnAwake = false;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log("[WorldSelectSceneBuilder] " + ScenePath + " を作成しました");
    }

    // ─── 絵 ───────────────────────────────────────────────────────
    // ステージマップと共通の絵は、あればそれを使う（無ければ描く）
    static Sprite Shared(string file, System.Func<string, Sprite> make)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(SharedArtDir + "/" + file);
        return s != null ? s : make(SharedArtDir);
    }

    static void DrawArt()
    {
        burst = Shared("Burst.png", Art.MakeBurst);
        padlock = Shared("Padlock.png", Art.MakePadlock);
        house = Shared("House.png", Art.MakeHouse);
        star = Shared("Star.png", Art.MakeStar);
        pines = new Sprite[3];
        for (int i = 0; i < pines.Length; i++)
        {
            int v = i;
            pines[i] = Shared("Pine_" + v + ".png", d => Art.MakePine(d, v));
        }

        EditorUtility.DisplayProgressBar("World Select", "名所を描いています…", 0.2f);
        worldArt = new[]
        {
            WArt.MakeLab(ArtDir), WArt.MakeLondon(ArtDir), WArt.MakePort(ArtDir), WArt.MakePalace(ArtDir),
            WArt.MakePagoda(ArtDir), WArt.MakeRuin(ArtDir), WArt.MakeNeo(ArtDir),
        };
        ship = WArt.MakeShip(ArtDir);
        torii = WArt.MakeTorii(ArtDir);
        pointer = WArt.MakePointer(ArtDir);
        flag = WArt.MakeFlag(ArtDir);
        fog = WArt.MakeFog(ArtDir);
        banner = WArt.MakeBanner(ArtDir);
        palms = new Sprite[2];
        for (int i = 0; i < palms.Length; i++) palms[i] = WArt.MakePalm(ArtDir, i);
        jungleTrees = new Sprite[3];
        for (int i = 0; i < jungleTrees.Length; i++) jungleTrees[i] = WArt.MakeJungleTree(ArtDir, i);

        EditorUtility.DisplayProgressBar("World Select", "地面を描いています…", 0.6f);
        ground = DrawGround();
    }

    static List<Vector2> MakeLand()
    {
        var pts = new List<Vector2>();
        var rnd = new System.Random(7);
        const int n = 240;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            float k = 1f + 0.025f * Mathf.Sin(a * 7f) + 0.02f * Mathf.Sin(a * 13f + 1f) + ((float)rnd.NextDouble() - 0.5f) * 0.012f;
            var p = new Vector2(Mathf.Sign(ca) * Mathf.Pow(Mathf.Abs(ca), 0.4f) * LandRadius.x, Mathf.Sign(sa) * Mathf.Pow(Mathf.Abs(sa), 0.4f) * LandRadius.y) * k;
            // 入り江は削る
            var q = (p - BayCenter) / BayRadius;
            if (q.sqrMagnitude < 1f) p = BayCenter + Vector2.Scale(q.normalized, BayRadius);
            pts.Add(p);
        }
        return pts;
    }

    static Sprite DrawGround()
    {
        var c = new InkCanvas(MapRect.width, MapRect.height, 64f, Art.Light);
        Vector2 G(Vector2 p) => p - MapRect.min;
        List<Vector2> GL(IList<Vector2> pts) { var r = new List<Vector2>(); foreach (var p in pts) r.Add(G(p)); return r; }
        var rnd = new System.Random(3);
        var ink = Art.InkC;
        var faint = new Color(ink.r, ink.g, ink.b, 0.22f);

        // 海：波と岩
        for (int i = 0; i < 160; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (InsideLand(p, -0.5f)) continue;
            WArt.DrawWave(c, G(p), 0.8f + (float)rnd.NextDouble() * 0.6f);
        }
        for (int i = 0; i < 14; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (InsideLand(p, -0.8f)) continue;
            Art.DrawStone(c, G(p), rnd);
            Art.DrawStone(c, G(p + new Vector2(0.3f, -0.1f)), rnd);
        }

        // 陸
        var landG = GL(InkCanvas.Smooth(land, 2));
        c.Fill(landG, Art.Paper);
        c.Stroke(InkCanvas.Offset(landG, new Vector2(0.12f, -0.12f)), 0.03f, ink, true);

        // アマゾンの川
        var river = GL(InkCanvas.Smooth(AmazonRiver, 12));
        c.Stroke(river, 0.75f, ink);
        c.Stroke(river, 0.65f, Art.Light);

        // 地方ごとの地面
        for (int i = 0; i < 1600; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (!InsideLand(p, 0.4f) || NearRoad(p, 0.6f) || NearNodeArt(p, 0.3f) || InRiver(p, 0.6f)) continue;
            int region = RegionOf(p);
            if (region == 3) continue; // 砂漠は点描
            if (region == 6 && InPlateau(p)) continue;
            Art.DrawTuft(c, G(p), rnd);
        }
        for (int i = 0; i < 2200; i++) // 砂漠の点描
        {
            var p = RandomPoint(rnd, MapRect);
            if (RegionOf(p) != 3 || !InsideLand(p, 0.3f) || NearRoad(p, 0.5f)) continue;
            c.Fill(InkCanvas.Ellipse(G(p), 0.025f, 0.02f, 6), ink);
        }
        for (int i = 0, placed = 0; i < 400 && placed < 22; i++) // 砂丘
        {
            var p = RandomPoint(rnd, MapRect);
            if (RegionOf(p) != 3 || !InsideLand(p, 1f) || NearRoad(p, 1.2f) || NearNodeArt(p, 0.6f)) continue;
            WArt.DrawDune(c, G(p), 1.6f + (float)rnd.NextDouble() * 1.4f, rnd);
            placed++;
        }
        for (int i = 0; i < 70; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (!InsideLand(p, 0.5f) || NearRoad(p, 0.6f) || NearNodeArt(p, 0.3f) || InRiver(p, 0.6f) || RegionOf(p) == 3) continue;
            Art.DrawStone(c, G(p), rnd);
        }

        // ロンドンのよどんだ空気
        var haze = InkCanvas.Ellipse(G(Worlds[1].position + new Vector2(0f, 2.6f)), 6.5f, 3.4f, 40);
        c.Hatch(haze, 35f, 0.24f, 0.02f, faint, rnd, 0.3f);

        // NEO の台地の格子
        for (float x = -6f; x <= 6f; x += 0.8f) c.Line(G(new Vector2(x, 3.9f)), G(new Vector2(x * 1.15f, 10.3f)), 0.02f, faint);
        for (float y = 4.6f; y < 10.3f; y += 0.9f) c.Line(G(new Vector2(-6.5f - (y - 4f) * 0.15f, y)), G(new Vector2(6.5f + (y - 4f) * 0.15f, y)), 0.02f, faint);

        // 地方の境目（点線）
        Dashed(c, GL(InkCanvas.Ellipse(new Vector2(0f, 0f), CenterRegion.x, CenterRegion.y, 80)), true);
        foreach (var dir in BorderDirections())
        {
            var pts = new List<Vector2>();
            float t0 = 1f / Mathf.Sqrt(Sq(dir.x / CenterRegion.x) + Sq(dir.y / CenterRegion.y));
            for (float t = t0; t < 30f; t += 0.25f)
            {
                var p = dir * t;
                if (!InsideLand(p, 0.6f)) break;
                pts.Add(G(p + new Vector2(Mathf.Sin(t * 0.9f), Mathf.Cos(t * 1.3f)) * 0.25f));
            }
            Dashed(c, pts, false);
        }

        // 道
        var smooth = new List<List<Vector2>>();
        foreach (var r in Roads) smooth.Add(GL(InkCanvas.Smooth(r, 12)));
        foreach (var s in smooth) c.Stroke(s, 0.8f, ink);
        foreach (var s in smooth) c.Stroke(s, 0.7f, Art.Paper);
        for (int i = 0; i < 90; i++)
        {
            var p = roadSamples[rnd.Next(roadSamples.Count)] + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 0.45f;
            c.Fill(InkCanvas.Ellipse(G(p), 0.03f, 0.02f, 8), ink);
        }

        // 茂み（ラボと地中海のまわり）
        for (int i = 0, placed = 0; i < 600 && placed < 30; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            int region = RegionOf(p);
            if ((region != 0 && region != 2) || !InsideLand(p, 0.8f) || NearRoad(p, 1.1f) || NearNodeArt(p, 0.8f)) continue;
            Art.DrawBush(c, G(p), 0.8f + (float)rnd.NextDouble() * 0.4f, rnd);
            placed++;
        }

        // 煙（ロンドン）
        foreach (var p in new[] { new Vector2(8.6f, 8.2f), new Vector2(12.2f, 9.0f), new Vector2(15.6f, 8.0f), new Vector2(17.2f, 5.8f) })
            Art.Puff(c, G(p), 1.4f, rnd);

        foreach (var m in Mountains) Art.DrawMountain(c, G(m.b), m.w, m.h, rnd);

        // NEO の台地と階段
        Art.DrawCliff(c, GL(PlateauRim), PlateauDepth, rnd);
        float rimY = 3.7f;
        var sMin = G(new Vector2(-0.45f, rimY - PlateauDepth - 0.1f));
        var sMax = G(new Vector2(0.45f, rimY + 0.05f));
        c.Shape(InkCanvas.Rect(sMin.x, sMin.y, sMax.x, sMax.y), Art.Paper, 0.04f, ink);
        for (float y = rimY - PlateauDepth; y < rimY; y += 0.16f) c.Line(G(new Vector2(-0.45f, y)), G(new Vector2(0.45f, y)), 0.022f, ink);

        // 海岸線
        c.Stroke(landG, 0.07f, ink, true);

        return c.Save(ArtDir + "/WorldGround.png", Vector2.zero);
    }

    static float Sq(float v) => v * v;

    static void Dashed(InkCanvas c, List<Vector2> pts, bool closed)
    {
        if (pts.Count < 2) return;
        var all = new List<Vector2>(pts);
        if (closed) all.Add(pts[0]);
        const float dash = 0.32f, gap = 0.22f;
        float acc = 0f;
        bool on = true;
        var seg = new List<Vector2> { all[0] };
        for (int i = 1; i < all.Count; i++)
        {
            Vector2 a = all[i - 1], b = all[i];
            float len = Vector2.Distance(a, b), pos = 0f;
            while (len - pos > 0.0001f)
            {
                float limit = on ? dash : gap;
                float step = Mathf.Min(limit - acc, len - pos);
                pos += step;
                acc += step;
                var p = Vector2.Lerp(a, b, pos / len);
                if (on) seg.Add(p);
                if (acc >= limit - 0.0001f)
                {
                    if (on && seg.Count > 1) c.Stroke(seg, 0.035f, Art.InkC);
                    seg = new List<Vector2> { p };
                    on = !on;
                    acc = 0f;
                }
            }
        }
        if (on && seg.Count > 1) c.Stroke(seg, 0.035f, Art.InkC);
    }

    // 中央以外の地方の向き（マップの中心から見た各ワールドの向き）を角度順に並べ、隣どうしの間を境目にする
    static List<Vector2> BorderDirections()
    {
        var angles = new List<float>();
        for (int i = 1; i < Worlds.Length; i++)
        {
            var d = Worlds[i].position;
            angles.Add(Mathf.Atan2(d.y, d.x));
        }
        angles.Sort();
        var result = new List<Vector2>();
        for (int i = 0; i < angles.Count; i++)
        {
            float a = angles[i], b = i + 1 < angles.Count ? angles[i + 1] : angles[0] + Mathf.PI * 2f;
            float m = (a + b) * 0.5f;
            result.Add(new Vector2(Mathf.Cos(m), Mathf.Sin(m)));
        }
        return result;
    }

    /// <summary>その点がどの地方か（Worlds の番号）。中央の楕円の中は 0</summary>
    static int RegionOf(Vector2 p)
    {
        if (Sq(p.x / CenterRegion.x) + Sq(p.y / CenterRegion.y) < 1f) return 0;
        // 境目（BorderDirections）と同じく、マップの中心からの角度で一番近いワールド
        float a = Mathf.Atan2(p.y, p.x);
        int best = 1;
        float bestDot = -2f;
        for (int i = 1; i < Worlds.Length; i++)
        {
            float dot = Mathf.Cos(a - Mathf.Atan2(Worlds[i].position.y, Worlds[i].position.x));
            if (dot > bestDot) { bestDot = dot; best = i; }
        }
        return best;
    }

    static bool InPlateau(Vector2 p) => p.y > 2.2f && Mathf.Abs(p.x) < 6.8f;

    static bool InsideLand(Vector2 p, float margin)
    {
        // margin だけ内側（負なら外側）を陸とみなす
        if (!PointInPoly(land, p)) return margin < 0f && DistanceToPoly(land, p) < -margin;
        return DistanceToPoly(land, p) >= margin;
    }

    static bool PointInPoly(List<Vector2> poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 a = poly[i], b = poly[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }

    static float DistanceToPoly(List<Vector2> poly, Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }
        return best;
    }

    static bool InRiver(Vector2 p, float d)
    {
        foreach (var q in InkCanvas.Smooth(AmazonRiver, 6)) if ((q - p).sqrMagnitude < d * d) return true;
        return false;
    }

    // 名所の絵と名札の範囲（草や木を置かない）
    static bool NearNodeArt(Vector2 p, float margin)
    {
        for (int i = 0; i < Worlds.Length; i++)
        {
            var w = Worlds[i].position;
            var size = worldArt != null && worldArt[i] != null ? (Vector2)worldArt[i].bounds.size : new Vector2(5.6f, 4.8f);
            if (p.x > w.x - size.x * 0.5f - margin && p.x < w.x + size.x * 0.5f + margin && p.y > w.y - 1.5f - margin && p.y < w.y + size.y + margin) return true;
        }
        return false;
    }

    // ─── 飾り（木・家・船・鳥居）────────────────────────────────────
    static void BuildDecorations(Transform parent)
    {
        var root = new GameObject("Decorations").transform;
        root.SetParent(parent, false);

        Place(root, "Ship", ship, new Vector2(15.6f, -8.4f));
        Place(root, "Ship_1", ship, new Vector2(18.4f, -9.6f)).transform.localScale = new Vector3(-0.8f, 0.8f, 1f);
        Place(root, "Torii", torii, new Vector2(-8.6f, 1.2f));
        foreach (var p in new[] { new Vector2(8.4f, 4.4f), new Vector2(9.2f, 5.6f), new Vector2(16.4f, 4.2f), new Vector2(17.0f, 1.6f), new Vector2(7.8f, 2.4f) })
            Place(root, "House", house, p);

        var rnd = new System.Random(12345);
        var points = new List<Vector2>();
        bool Ok(Vector2 p)
        {
            if (!InsideLand(p, 0.7f) || NearRoad(p, 0.9f) || NearNodeArt(p, 0.4f) || InRiver(p, 0.8f) || InPlateau(p)) return false;
            if (p.x > -10f && p.x < -7.2f && p.y > 0.6f && p.y < 3.6f) return false; // 鳥居
            foreach (var q in points) if ((q - p).sqrMagnitude < 0.75f * 0.75f) return false;
            return true;
        }

        void Scatter(int region, Sprite[] sprites, int count, float scaleMin, float scaleMax, string name)
        {
            for (int i = 0, placed = 0; i < 3000 && placed < count; i++)
            {
                var p = RandomPoint(rnd, MapRect);
                if (RegionOf(p) != region || !Ok(p)) continue;
                points.Add(p);
                var go = Place(root, name + "_" + placed, sprites[rnd.Next(sprites.Length)], p);
                float s = scaleMin + (float)rnd.NextDouble() * (scaleMax - scaleMin);
                go.transform.localScale = new Vector3(rnd.NextDouble() < 0.5 ? s : -s, s, 1f);
                placed++;
            }
        }

        Scatter(4, pines, 45, 0.75f, 1.05f, "Pine");          // 日本
        Scatter(5, jungleTrees, 45, 0.75f, 1.05f, "Jungle");  // アマゾン
        Scatter(3, palms, 12, 0.75f, 1.0f, "Palm");           // 中東（オアシスの木）
        Scatter(2, palms, 5, 0.7f, 0.9f, "Palm_Sea");         // 地中海
        Scatter(0, pines, 8, 0.7f, 0.9f, "Pine_Center");      // ラボのまわり
    }

    // ─── ワールド ──────────────────────────────────────────────────
    static WorldSelectNode MakeNode(Transform parent, WorldData world, Sprite art, Vector2 pos)
    {
        var go = new GameObject("World_" + world.worldId, typeof(SortingGroup), typeof(WorldSelectNode));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.GetComponent<SortingGroup>().sortingOrder = WorldMapDepthSort.OrderFor(pos.y);
        var node = go.GetComponent<WorldSelectNode>();
        node.world = world;

        var size = (Vector2)art.bounds.size;
        float top = art.bounds.max.y;

        var glow = Child(go.transform, "Glow", burst, new Vector2(0f, top * 0.45f), -1);
        glow.transform.localScale = Vector3.one * Mathf.Max(size.x / 4.2f, 1f);
        node.glow = glow.GetComponent<SpriteRenderer>();
        node.glowColor = Art.InkC;
        node.glow.enabled = false;

        var artGo = Child(go.transform, "Art", art, Vector2.zero, 0);
        node.artRenderers = new[] { artGo.GetComponent<SpriteRenderer>() };

        var flagGo = Child(go.transform, "ClearedFlag", flag, new Vector2(size.x * 0.42f, 0.05f), 1);
        node.clearedMark = flagGo;
        flagGo.SetActive(false);

        var fogGo = Child(go.transform, "Fog", fog, new Vector2(0f, top * 0.5f), 2);
        fogGo.transform.localScale = Vector3.one * (size.x / 5.0f);
        node.fog = fogGo;

        var lockGo = Child(go.transform, "Lock", padlock, new Vector2(0f, top * 0.32f), 3);
        lockGo.transform.localScale = Vector3.one * 1.4f;
        node.lockIcon = lockGo;

        MakeBanner(parent, node, pos + new Vector2(0f, -0.75f));

        var col = go.AddComponent<BoxCollider2D>();
        float h = top + 1.4f;
        col.size = new Vector2(Mathf.Max(size.x, 4.6f), h);
        col.offset = new Vector2(0f, top - h * 0.5f);
        node.pickArea = col;
        node.cursorOffset = new Vector2(0f, -1.5f); // 名札のすぐ下（上の絵や他の名札と重ならない）
        return node;
    }

    static void MakeBanner(Transform parent, WorldSelectNode node, Vector2 pos)
    {
        var root = new GameObject("Banner_" + node.WorldId, typeof(SortingGroup));
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.GetComponent<SortingGroup>().sortingOrder = OrderBanner;
        Child(root.transform, "Board", banner, Vector2.zero, 0);

        var textGo = new GameObject("Text", typeof(TextMeshPro));
        textGo.transform.SetParent(root.transform, false);
        var tmp = textGo.GetComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = WArt.BannerTextSize;
        if (font != null) tmp.font = font;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 2f;
        tmp.fontSizeMax = 6.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Art.InkC;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.lineSpacing = -20f;
        tmp.sortingOrder = 1;
        tmp.text = "";

        node.labelRoot = root.transform;
        node.label = tmp;
    }

    // ─── UI（紙とインクの色に合わせる） ─────────────────────────────
    static RectTransform MakeCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go.GetComponent<RectTransform>();
    }

    static WorldSelectDetailPanel MakeDetailPanel(RectTransform canvas)
    {
        var root = UIBox(canvas, "DetailPanel", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        var panel = root.gameObject.AddComponent<WorldSelectDetailPanel>();

        var frame = UIBox(root, "Frame", Vector2.zero, new Vector2(1000, 640), Art.InkC);
        UIBox(frame, "Metal", Vector2.zero, new Vector2(988, 628), Art.Shade);
        UIBox(frame, "BoardLine", Vector2.zero, new Vector2(938, 578), Art.InkC);
        UIBox(frame, "Board", Vector2.zero, new Vector2(930, 570), Art.Paper);
        foreach (var b in new[] { new Vector2(-472, -292), new Vector2(472, -292), new Vector2(-472, 292), new Vector2(472, 292) })
            UIBox(frame, "Bolt", b, new Vector2(14, 14), Art.InkC);
        panel.clickArea = frame;

        panel.numberLabel = UIText(frame, "Number", new Vector2(0, 232), new Vector2(880, 44), 32, TextAlignmentOptions.Center);
        panel.titleLabel = UIText(frame, "Title", new Vector2(0, 175), new Vector2(880, 80), 64, TextAlignmentOptions.Center);
        panel.statusLabel = UIText(frame, "Status", new Vector2(0, 112), new Vector2(880, 50), 36, TextAlignmentOptions.Center);
        UIBox(frame, "Line", new Vector2(0, 78), new Vector2(840, 3), Art.InkC);
        panel.descriptionLabel = UIText(frame, "Description", new Vector2(0, -12), new Vector2(840, 160), 34, TextAlignmentOptions.Center);
        panel.progressLabel = UIText(frame, "Progress", new Vector2(0, -128), new Vector2(840, 50), 36, TextAlignmentOptions.Center);
        UIBox(frame, "Line2", new Vector2(0, -180), new Vector2(840, 3), Art.InkC);
        panel.hintLabel = UIText(frame, "Hint", new Vector2(0, -230), new Vector2(880, 50), 32, TextAlignmentOptions.Center);

        panel.clearedColor = new Color(0.20f, 0.40f, 0.22f);
        panel.notClearedColor = Art.Dark;

        root.gameObject.SetActive(false);
        return panel;
    }

    static void MakeHud(RectTransform canvas)
    {
        var title = UIBox(canvas, "Title", Vector2.zero, new Vector2(366, 86), Art.InkC);
        title.anchorMin = title.anchorMax = new Vector2(0f, 1f);
        title.pivot = new Vector2(0f, 1f);
        title.anchoredPosition = new Vector2(24, -24);
        UIBox(title, "Paper", Vector2.zero, new Vector2(360, 80), Art.Paper);
        UIText(title, "Text", Vector2.zero, new Vector2(340, 80), 44, TextAlignmentOptions.Center).text = "ワールドマップ";

        var line = UIBox(canvas, "Controls", Vector2.zero, new Vector2(1206, 66), Art.InkC);
        line.anchorMin = line.anchorMax = new Vector2(0f, 0f);
        line.pivot = new Vector2(0f, 0f);
        line.anchoredPosition = new Vector2(24, 24);
        UIBox(line, "Paper", Vector2.zero, new Vector2(1200, 60), Art.Paper);
        var t = UIText(line, "Text", Vector2.zero, new Vector2(1180, 60), 28, TextAlignmentOptions.Center);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.text = "選択：WASD／十字キー／マウス　　決定：Space／A／クリック　　戻る：Esc／B（マイタウンへ）";
    }

    static RectTransform UIBox(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static TMP_Text UIText(RectTransform parent, string name, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Art.InkC;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    // ─── 部品 ─────────────────────────────────────────────────────
    static GameObject Place(Transform parent, string name, Sprite sprite, Vector2 pos, bool depthSort = true)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (depthSort) sr.sortingOrder = WorldMapDepthSort.OrderFor(pos.y);
        return go;
    }

    static GameObject Child(Transform parent, string name, Sprite sprite, Vector2 localPos, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    static Vector2 RandomPoint(System.Random rnd, Rect r)
    {
        return new Vector2(r.xMin + (float)rnd.NextDouble() * r.width, r.yMin + (float)rnd.NextDouble() * r.height);
    }

    static bool NearRoad(Vector2 p, float distance)
    {
        float d2 = distance * distance;
        foreach (var q in roadSamples) if ((q - p).sqrMagnitude < d2) return true;
        return false;
    }

    // ─── ワールド情報 ─────────────────────────────────────────────
    static WorldData[] CreateWorldAssets()
    {
        Directory.CreateDirectory(WorldDir);
        var result = new WorldData[Worlds.Length];
        for (int i = 0; i < Worlds.Length; i++)
        {
            var spec = Worlds[i];
            string path = WorldDir + "/World_" + spec.id + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<WorldData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WorldData>();
                data.worldId = spec.id;
                data.worldNumber = spec.number;
                data.worldName = spec.name;
                data.description = spec.description;
                data.isBoss = spec.boss;
                data.stageMapScene = spec.stageMapScene ?? "";
                data.clearStageId = spec.clearStageId ?? "";
                data.unlockedByDefault = spec.unlockAfter == null || spec.unlockAfter.Length == 0;
                var stages = new List<WorldMapStageData>();
                if (spec.stageIds != null)
                    foreach (var id in spec.stageIds)
                    {
                        var s = AssetDatabase.LoadAssetAtPath<WorldMapStageData>(StageDir + "/Stage_" + id + ".asset");
                        if (s != null) stages.Add(s);
                    }
                data.stages = stages.ToArray();
                AssetDatabase.CreateAsset(data, path);
            }
            result[i] = data;
        }

        // 解放条件（空のときだけ入れる）
        for (int i = 0; i < Worlds.Length; i++)
        {
            var spec = Worlds[i];
            var data = result[i];
            if (data.unlockedByDefault || spec.unlockAfter == null || (data.unlockAfter != null && data.unlockAfter.Length > 0)) continue;
            var list = new List<WorldData>();
            foreach (var id in spec.unlockAfter)
                for (int k = 0; k < Worlds.Length; k++)
                    if (Worlds[k].id == id) list.Add(result[k]);
            data.unlockAfter = list.ToArray();
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
