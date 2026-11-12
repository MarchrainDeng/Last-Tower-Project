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

/// <summary>
/// ワールドマップのシーンを組み立てるエディタツール
/// メニュー Tools/Story/Build World Map Scene で Assets/Scenes/WorldMapScene.unity を作り直す
///
/// 見た目は仕様書の例イメージ（白黒のペン画）に合わせ、絵はすべてコードで描いて Assets/Story/WorldMap/Art/ に書き出す
/// - 左上：風車と城、左：教会と池、右上：崖の上の工場と大歯車と門、右下：山脈、あちこちに針葉樹
/// - 台座5つを曲がりくねった道でつなぎ、左下にマイタウンへの出入口
/// 山・池・崖・建物・木には Collider2D を付けて通れなくしている。前後関係は足元のYで決める
///
/// ステージ情報（Assets/Story/WorldMap/Stages/*.asset）は無いときだけ仮データを作る
/// 名前・報酬・シーン名はアセットを直接編集すれば、作り直しても消えない
/// </summary>
public static class WorldMapSceneBuilder
{
    const string ScenePath = "Assets/Scenes/WorldMapScene.unity";
    const string ArtDir = "Assets/Story/WorldMap/Art";
    const string StageDir = "Assets/Story/WorldMap/Stages";
    const string FontPath = "Assets/Sasaki/JA_KO SDF.asset";
    const string FirstStageScenePath = "Assets/Scenes/Stage/Stage_1/Stage_01SceneTmp.unity";

    static readonly Rect MapRect = new Rect(-24f, -15f, 48f, 30f);
    const float CameraSize = 5.5f;
    const int OrderGround = -32000, OrderSign = 30000;

    // ─── 配置 ─────────────────────────────────────────────────────
    struct StageSpec
    {
        public string id, name, scene;
        public string[] rewards;
        public Vector2 position;
        public StageSpec(string id, string name, string scene, Vector2 position, params string[] rewards)
        {
            this.id = id; this.name = name; this.scene = scene; this.position = position; this.rewards = rewards;
        }
    }

    // 並び順＝解放順（1つ前をクリアすると次が解放される）。position は台座の足元
    static readonly StageSpec[] Stages =
    {
        new StageSpec("1-1", "はじまりの荒野", "Stage_01SceneTmp", new Vector2(-14f, -9.5f), "コイン ×100", "ブロック：キャノン"),
        new StageSpec("1-2", "工場跡地",       "",                 new Vector2(-1f,  -4.5f), "コイン ×150", "ブロック：レーザー"),
        new StageSpec("1-3", "水辺の廃村",     "",                 new Vector2(-8f,   5.0f), "コイン ×200", "パーツ：歯車"),
        new StageSpec("1-4", "崖の監視塔",     "",                 new Vector2(11.5f, -3.0f), "コイン ×250", "ブロック：ねばねば"),
        new StageSpec("1-5", "歯車の城門",     "",                 new Vector2(12.5f,  5.2f), "コイン ×400", "称号：門番殺し"),
    };

    static readonly Vector2 TownGatePosition = new Vector2(-21f, -12.5f);

    // 道（見た目だけ。なめらかな曲線にして描く）
    static readonly Vector2[][] Roads =
    {
        new[] { new Vector2(-21f, -12.2f), new Vector2(-18f, -11f), new Vector2(-14f, -8.6f), new Vector2(-10f, -8.4f), new Vector2(-5f, -5.5f), new Vector2(-1f, -3.6f) },
        new[] { new Vector2(-1f, -3.6f), new Vector2(-3f, -0.5f), new Vector2(-6.5f, 2.5f), new Vector2(-8f, 5.9f), new Vector2(-10.5f, 7.8f), new Vector2(-14f, 8.6f) },
        new[] { new Vector2(-1f, -3.6f), new Vector2(3f, -2.2f), new Vector2(6.5f, 0.5f), new Vector2(9.5f, 3.2f), new Vector2(12.5f, 6.1f), new Vector2(12.5f, 8.2f) },
        new[] { new Vector2(-1f, -3.6f), new Vector2(3.5f, -5.2f), new Vector2(7.5f, -4.4f), new Vector2(11.5f, -2.1f) },
        new[] { new Vector2(-14f, 8.6f), new Vector2(-11.5f, 11.5f), new Vector2(-9.5f, 15.5f) },
        new[] { new Vector2(-10f, -8.4f), new Vector2(-14f, -4.5f), new Vector2(-17f, -2.2f) },
    };

    // 崖：台地の縁（rim）。岩肌はそこから下へ CliffDepth
    const float CliffDepth = 1.8f;
    static readonly Vector2[][] Cliffs =
    {
        new[] { new Vector2(2.2f, 15.6f), new Vector2(3.6f, 13.0f), new Vector2(6.0f, 10.6f), new Vector2(9.3f, 9.4f) },
        new[] { new Vector2(15.7f, 9.4f), new Vector2(18.2f, 8.4f), new Vector2(21.0f, 8.0f), new Vector2(24.6f, 7.4f) },
        new[] { new Vector2(24.6f, 3.4f), new Vector2(22.0f, 2.0f), new Vector2(20.2f, -0.6f), new Vector2(20.6f, -4.2f) },
    };

    // 山（麓の中心、幅、高さ）。奥（上）から順に描く
    static readonly (Vector2 b, float w, float h)[] Mountains =
    {
        (new Vector2(12.5f, -7.5f), 4.2f, 3.0f), (new Vector2(16.5f, -7.2f), 4.8f, 3.8f), (new Vector2(21f, -7.6f), 4.6f, 3.4f), (new Vector2(24.5f, -7.8f), 3.6f, 3.0f),
        (new Vector2(10.5f, -10.6f), 4.4f, 3.4f), (new Vector2(14.8f, -10.8f), 5.2f, 4.4f), (new Vector2(19.5f, -11.2f), 5.2f, 4.6f), (new Vector2(23.6f, -11.4f), 4.2f, 3.6f),
        (new Vector2(8.6f, -14.4f), 4.6f, 3.4f), (new Vector2(13f, -14.8f), 5.2f, 4.2f), (new Vector2(17.8f, -15.2f), 5.6f, 4.6f), (new Vector2(22.4f, -15.2f), 5.2f, 4.2f),
    };

    static readonly Vector2 PondCenter = new Vector2(-19.5f, 3.2f);
    static readonly Vector2 PondRadius = new Vector2(2.2f, 1.3f);

    static TMP_FontAsset font;
    static readonly List<Rect> blocked = new List<Rect>();
    static List<Vector2> roadSamples;

    // 描いた絵
    static Sprite ground, pedestal, burst, padlock, checkPlate, clearTower, sign, robot, castle, windmill, blades,
                  church, house, factory, bigGear, gate, townArch, star;
    static Sprite[] pines;

    [MenuItem("Tools/Story/Build World Map Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(ArtDir);
        RemoveOldPlaceholderArt();
        blocked.Clear();
        roadSamples = new List<Vector2>();
        foreach (var r in Roads) roadSamples.AddRange(InkCanvas.Smooth(r, 12));

        try
        {
            EditorUtility.DisplayProgressBar("World Map", "絵を描いています…", 0.1f);
            DrawArt();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        var stages = CreateStageAssets();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // カメラ
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(WorldMapCameraFollow));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Art.Paper;
        var follow = camGo.GetComponent<WorldMapCameraFollow>();
        follow.bounds = MapRect;

        var light = new GameObject("Global Light 2D", typeof(Light2D)).GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // ── 地面（道・崖・山・池・草を焼き込んだ1枚絵）──
        var map = new GameObject("Map").transform;
        var groundGo = Place(map, "Ground", ground, MapRect.min, false);
        groundGo.GetComponent<SpriteRenderer>().sortingOrder = OrderGround;
        BuildBounds(map);
        BuildTerrainColliders(map);

        // ── 建物 ──
        BuildBuildings(map);

        // ── 台座 ──
        var spotsRoot = new GameObject("Spots").transform;
        var spots = new List<WorldMapSpot>();
        for (int i = 0; i < Stages.Length; i++)
            spots.Add(MakePedestal(spotsRoot, stages[i], Stages[i].position));
        var townGate = MakeTownGate(spotsRoot, TownGatePosition);
        spots.Add(townGate);

        // ── 木（最後に、他の物を避けて置く）──
        BuildTrees(map);

        // ── プレイヤー ──
        var vehicle = MakeRobot(townGate.FrontPosition);
        follow.target = vehicle.transform;

        // ── UI ──
        var canvas = MakeCanvas();
        var detail = MakeDetailPanel(canvas);
        MakeHud(canvas);

        // ── コントローラー ──
        var ctrlGo = new GameObject("WorldMapController", typeof(AudioSource), typeof(WorldMapController));
        var ctrl = ctrlGo.GetComponent<WorldMapController>();
        ctrl.vehicle = vehicle;
        ctrl.cameraFollow = follow;
        ctrl.detailPanel = detail;
        ctrl.spots = spots.ToArray();
        ctrl.defaultSpot = townGate;
        ctrl.audioSource = ctrlGo.GetComponent<AudioSource>();
        ctrl.audioSource.playOnAwake = false;

        camGo.transform.position = new Vector3(townGate.FrontPosition.x, townGate.FrontPosition.y, -10f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        if (File.Exists(FirstStageScenePath)) AddToBuildSettings(FirstStageScenePath);
        Debug.Log("[WorldMapSceneBuilder] " + ScenePath + " を作成しました");
    }

    // 以前の仮の絵（四角・丸）を消す
    static void RemoveOldPlaceholderArt()
    {
        foreach (var name in new[] { "Square", "Circle", "Ring", "Triangle" })
        {
            string p = ArtDir + "/" + name + ".png";
            if (File.Exists(p)) AssetDatabase.DeleteAsset(p);
        }
    }

    // ─── 絵 ───────────────────────────────────────────────────────
    static void DrawArt()
    {
        pines = new Sprite[3];
        for (int i = 0; i < pines.Length; i++) pines[i] = Art.MakePine(ArtDir, i);
        pedestal = Art.MakePedestal(ArtDir);
        burst = Art.MakeBurst(ArtDir);
        padlock = Art.MakePadlock(ArtDir);
        checkPlate = Art.MakeCheckPlate(ArtDir);
        clearTower = Art.MakeClearTower(ArtDir);
        sign = Art.MakeSign(ArtDir);
        robot = Art.MakeRobot(ArtDir);
        castle = Art.MakeCastle(ArtDir);
        windmill = Art.MakeWindmill(ArtDir);
        blades = Art.MakeBlades(ArtDir);
        church = Art.MakeChurch(ArtDir);
        house = Art.MakeHouse(ArtDir);
        factory = Art.MakeFactory(ArtDir);
        bigGear = Art.MakeBigGear(ArtDir);
        gate = Art.MakeGate(ArtDir);
        townArch = Art.MakeTownArch(ArtDir);
        star = Art.MakeStar(ArtDir);
        EditorUtility.DisplayProgressBar("World Map", "地面を描いています…", 0.6f);
        ground = DrawGround();
    }

    static Sprite DrawGround()
    {
        var c = new InkCanvas(MapRect.width, MapRect.height, 64f, Art.Paper);
        Vector2 G(Vector2 p) => p - MapRect.min;
        List<Vector2> GL(IList<Vector2> pts) { var r = new List<Vector2>(); foreach (var p in pts) r.Add(G(p)); return r; }
        var rnd = new System.Random(3);

        // 草と石（道・山・池・崖の上は避ける）
        for (int i = 0; i < 1100; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (NearRoad(p, 0.75f) || InTerrain(p)) continue;
            Art.DrawTuft(c, G(p), rnd);
        }
        for (int i = 0; i < 90; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (NearRoad(p, 0.7f) || InTerrain(p)) continue;
            Art.DrawStone(c, G(p), rnd);
        }

        // 城の前の畑
        for (int i = 0; i < 6; i++)
        {
            float y = 7.3f - i * 0.42f;
            c.Line(G(new Vector2(-20.5f + i * 0.15f, y)), G(new Vector2(-15.2f - i * 0.1f, y - 0.25f)), 0.03f, Art.InkC);
        }

        Art.DrawPond(c, G(PondCenter), PondRadius.x, PondRadius.y, rnd);

        // 道：まず全部の縁（黒い太線）、次に全部の中（紙の色）を塗って、交わりをきれいにする
        var smooth = new List<List<Vector2>>();
        foreach (var r in Roads) smooth.Add(GL(InkCanvas.Smooth(r, 12)));
        foreach (var s in smooth) c.Stroke(s, 1.0f, Art.InkC);
        foreach (var s in smooth) c.Stroke(s, 0.9f, Art.Paper);
        // 道の中の小石
        for (int i = 0; i < 70; i++)
        {
            var p = roadSamples[rnd.Next(roadSamples.Count)] + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 0.6f;
            c.Fill(InkCanvas.Ellipse(G(p), 0.03f, 0.02f, 8), Art.InkC);
        }

        // 茂み
        for (int i = 0, placed = 0; i < 400 && placed < 45; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (NearRoad(p, 1.2f) || InTerrain(p) || NearSpot(p, 3f) || NearBuilding(p)) continue;
            Art.DrawBush(c, G(p), 0.9f + (float)rnd.NextDouble() * 0.5f, rnd);
            placed++;
        }

        foreach (var rim in Cliffs) Art.DrawCliff(c, GL(rim), CliffDepth, rnd);
        foreach (var m in Mountains) Art.DrawMountain(c, G(m.b), m.w, m.h, rnd);

        return c.Save(ArtDir + "/Ground.png", Vector2.zero);
    }

    // 山・崖・池の範囲（草や木を置かない）
    static bool InTerrain(Vector2 p)
    {
        if (p.x > 7f && p.y < -6f) return true; // 山脈
        if (((p - PondCenter) / (PondRadius + Vector2.one * 0.4f)).sqrMagnitude < 1f) return true;
        foreach (var rim in Cliffs)
            for (int i = 0; i < rim.Length - 1; i++)
            {
                var a = rim[i]; var b = rim[i + 1];
                float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                var top = Vector2.Lerp(a, b, t);
                if (Mathf.Abs(p.x - top.x) < 0.6f && p.y <= top.y + 0.3f && p.y >= top.y - CliffDepth - 0.6f) return true;
            }
        return false;
    }

    static bool NearSpot(Vector2 p, float d)
    {
        foreach (var s in Stages) if (Vector2.Distance(p, s.position) < d) return true;
        return Vector2.Distance(p, TownGatePosition) < d;
    }

    static bool NearBuilding(Vector2 p)
    {
        foreach (var r in blocked) if (r.Contains(p)) return true;
        return false;
    }

    // ─── 当たり判定（地形） ────────────────────────────────────────
    static void BuildBounds(Transform parent)
    {
        var root = new GameObject("Bounds").transform;
        root.SetParent(parent, false);
        const float t = 2f;
        Box(root, "Top", new Vector2(MapRect.center.x, MapRect.yMax + t * 0.5f), new Vector2(MapRect.width + t * 2f, t));
        Box(root, "Bottom", new Vector2(MapRect.center.x, MapRect.yMin - t * 0.5f), new Vector2(MapRect.width + t * 2f, t));
        Box(root, "Left", new Vector2(MapRect.xMin - t * 0.5f, MapRect.center.y), new Vector2(t, MapRect.height));
        Box(root, "Right", new Vector2(MapRect.xMax + t * 0.5f, MapRect.center.y), new Vector2(t, MapRect.height));
    }

    static void BuildTerrainColliders(Transform parent)
    {
        var root = new GameObject("TerrainColliders").transform;
        root.SetParent(parent, false);

        // 崖：岩肌の下の縁に沿った線。門の両脇までつないで台地を囲う
        var c0 = new List<Vector2>();
        foreach (var p in Cliffs[0]) c0.Add(p - new Vector2(0f, CliffDepth - 0.3f));
        c0.Add(new Vector2(9.7f, 9.2f));
        Edge(root, "Cliff_0", c0);
        var c1 = new List<Vector2> { new Vector2(15.3f, 9.2f) };
        foreach (var p in Cliffs[1]) c1.Add(p - new Vector2(0f, CliffDepth - 0.3f));
        Edge(root, "Cliff_1", c1);
        var c2 = new List<Vector2>();
        foreach (var p in Cliffs[2]) c2.Add(p - new Vector2(0f, CliffDepth - 0.3f));
        Edge(root, "Cliff_2", c2);

        // 山：麓の三角形
        for (int i = 0; i < Mountains.Length; i++)
        {
            var m = Mountains[i];
            var go = new GameObject("Mountain_" + i, typeof(PolygonCollider2D));
            go.transform.SetParent(root, false);
            go.GetComponent<PolygonCollider2D>().points = new[]
            {
                m.b + new Vector2(-m.w * 0.45f, 0.1f), m.b + new Vector2(m.w * 0.45f, 0.1f), m.b + new Vector2(0f, m.h * 0.75f)
            };
        }

        // 池
        var pond = new GameObject("Pond", typeof(CapsuleCollider2D));
        pond.transform.SetParent(root, false);
        pond.transform.position = PondCenter;
        var cap = pond.GetComponent<CapsuleCollider2D>();
        cap.direction = CapsuleDirection2D.Horizontal;
        cap.size = PondRadius * 2f * 0.95f;
    }

    static void Edge(Transform parent, string name, List<Vector2> pts)
    {
        var go = new GameObject(name, typeof(EdgeCollider2D));
        go.transform.SetParent(parent, false);
        var e = go.GetComponent<EdgeCollider2D>();
        e.points = pts.ToArray();
        e.edgeRadius = 0.25f;
    }

    static void Box(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.GetComponent<BoxCollider2D>().size = size;
    }

    // ─── 建物 ─────────────────────────────────────────────────────
    static void BuildBuildings(Transform parent)
    {
        var root = new GameObject("Buildings").transform;
        root.SetParent(parent, false);

        // 左上：城と風車（風車は城の後ろ）
        var mill = Place(root, "Windmill", windmill, new Vector2(-21.6f, 10.0f));
        var bladeGo = new GameObject("Blades", typeof(SpriteRenderer), typeof(ConstantRotate));
        bladeGo.transform.SetParent(mill.transform, false);
        bladeGo.transform.localPosition = new Vector3(0f, Art.WindmillHubHeight, 0f);
        var bsr = bladeGo.GetComponent<SpriteRenderer>();
        bsr.sprite = blades;
        bsr.sortingOrder = mill.GetComponent<SpriteRenderer>().sortingOrder + 1;
        bladeGo.GetComponent<ConstantRotate>().rotateSpeed = 18f;
        Solid(mill, new Vector2(1.4f, 0.8f));
        Solid(Place(root, "Castle", castle, new Vector2(-16.2f, 8.8f)), new Vector2(6.6f, 1.4f));

        // 左：教会と家
        Solid(Place(root, "Church", church, new Vector2(-18.2f, -2.4f)), new Vector2(1.9f, 0.9f));
        Solid(Place(root, "House_0", house, new Vector2(-20.8f, -4.2f)), new Vector2(1.5f, 0.7f));
        Solid(Place(root, "House_1", house, new Vector2(-15.6f, 0.2f)), new Vector2(1.5f, 0.7f));

        // 右上：崖の上の工場と大歯車、崖の切れ目の門
        var fac = Place(root, "Factory", factory, new Vector2(18.2f, 10.4f));
        var gearGo = new GameObject("BigGear", typeof(SpriteRenderer), typeof(ConstantRotate));
        gearGo.transform.SetParent(fac.transform, false);
        gearGo.transform.localPosition = Art.FactoryGearOffset;
        var gsr = gearGo.GetComponent<SpriteRenderer>();
        gsr.sprite = bigGear;
        gsr.sortingOrder = fac.GetComponent<SpriteRenderer>().sortingOrder + 1;
        gearGo.GetComponent<ConstantRotate>().rotateSpeed = -12f;
        Solid(fac, new Vector2(10f, 1.4f));
        Solid(Place(root, "Gate", gate, new Vector2(12.5f, 8.7f)), new Vector2(6.4f, 1.0f));
    }

    // 足元の当たり判定と、木を置かない範囲
    static void Solid(GameObject go, Vector2 footprint)
    {
        var b = go.AddComponent<BoxCollider2D>();
        b.size = footprint;
        b.offset = new Vector2(0f, footprint.y * 0.5f);
        Vector2 p = go.transform.position;
        var sr = go.GetComponent<SpriteRenderer>();
        var size = sr != null && sr.sprite != null ? (Vector2)sr.sprite.bounds.size : footprint;
        blocked.Add(new Rect(p.x - size.x * 0.5f - 0.3f, p.y - 0.6f, size.x + 0.6f, Mathf.Max(size.y * 0.7f, 1.5f)));
    }

    static void BuildTrees(Transform parent)
    {
        var root = new GameObject("Trees").transform;
        root.SetParent(parent, false);
        var rnd = new System.Random(12345);
        var points = new List<Vector2>();

        bool Ok(Vector2 p)
        {
            if (!MapRect.Contains(p)) return false;
            if (NearRoad(p, 1.1f) || InTerrain(p) || NearSpot(p, 3.2f) || NearBuilding(p)) return false;
            foreach (var q in points) if ((q - p).sqrMagnitude < 0.8f * 0.8f) return false;
            return true;
        }

        // 林（まとまり）と、ばらばらの木
        for (int k = 0; k < 22; k++)
        {
            var center = RandomPoint(rnd, MapRect);
            int n = 3 + rnd.Next(6);
            for (int i = 0; i < n * 4 && n > 0; i++)
            {
                var p = center + new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 4f;
                if (!Ok(p)) continue;
                points.Add(p);
                n--;
            }
        }
        for (int i = 0; i < 400 && points.Count < 150; i++)
        {
            var p = RandomPoint(rnd, MapRect);
            if (Ok(p)) points.Add(p);
        }

        for (int i = 0; i < points.Count; i++)
        {
            var go = Place(root, "Pine_" + i, pines[rnd.Next(pines.Length)], points[i]);
            float s = 0.85f + (float)rnd.NextDouble() * 0.35f;
            go.transform.localScale = new Vector3(rnd.NextDouble() < 0.5 ? s : -s, s, 1f);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.22f;
            col.offset = new Vector2(0f, 0.15f);
        }
    }

    // ─── 台座 ─────────────────────────────────────────────────────
    static WorldMapPedestal MakePedestal(Transform parent, WorldMapStageData stage, Vector2 pos)
    {
        var go = new GameObject("Pedestal_" + stage.stageId, typeof(SortingGroup), typeof(WorldMapPedestal));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.GetComponent<SortingGroup>().sortingOrder = WorldMapDepthSort.OrderFor(pos.y);
        var p = go.GetComponent<WorldMapPedestal>();
        p.stage = stage;
        float topY = Art.PedestalPivotToTop;

        // 近づいたときの集中線とキラキラ
        var glow = Child(go.transform, "Glow", burst, new Vector2(0f, topY), -1);
        p.glow = glow.GetComponent<SpriteRenderer>();
        p.glowColor = Art.InkC;
        p.glow.enabled = false;

        var slab = Child(go.transform, "Slab", pedestal, Vector2.zero, 0);
        p.baseRenderers = new[] { slab.GetComponent<SpriteRenderer>() };
        p.lockedTint = new Color(0.82f, 0.82f, 0.82f, 1f);

        // クリア後の塔（台座の左奥）。足元から伸びる
        var tower = Child(go.transform, "Tower", clearTower, new Vector2(-0.95f, topY + 0.2f), 1);
        tower.transform.localScale = Vector3.one * 0.75f;
        p.tower = tower.transform;
        tower.SetActive(false);

        var lockGo = Child(go.transform, "Lock", padlock, new Vector2(0f, topY + 0.35f), 2);
        p.lockIcon = lockGo;

        var mark = Child(go.transform, "ClearedMark", checkPlate, new Vector2(-0.95f, 0.15f), 3);
        p.clearedMark = mark;
        mark.SetActive(false);

        // 看板（台座の奥に立てる。前後関係とは別に一番上に描く）
        MakeSign(parent, p, pos + new Vector2(0.45f, topY + 0.6f)); // 柱が台座の奥の縁に立つ位置

        // 台座の上には乗れない
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = new Vector2(2.9f, 1.3f);
        col.offset = new Vector2(0f, topY - 0.15f);
        p.approachRadius = 2.7f;
        p.frontOffset = new Vector2(-2.4f, 0.1f);

        blocked.Add(new Rect(pos.x - 2.5f, pos.y - 1f, 5f, 4f));
        return p;
    }

    static WorldMapTownGate MakeTownGate(Transform parent, Vector2 pos)
    {
        var go = new GameObject("TownGate", typeof(SortingGroup), typeof(WorldMapTownGate));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.GetComponent<SortingGroup>().sortingOrder = WorldMapDepthSort.OrderFor(pos.y);
        var g = go.GetComponent<WorldMapTownGate>();

        var glow = Child(go.transform, "Glow", burst, new Vector2(0f, 0.6f), -1);
        glow.transform.localScale = Vector3.one * 0.7f;
        g.glow = glow.GetComponent<SpriteRenderer>();
        g.glowColor = Art.InkC;
        g.glow.enabled = false;
        Child(go.transform, "Arch", townArch, Vector2.zero, 0);

        MakeSign(parent, g, pos + new Vector2(2.2f, 0.4f));

        // 柱だけ当たり判定
        Box(go.transform, "PostL", pos + new Vector2(-0.95f, 0.15f), new Vector2(0.3f, 0.3f));
        Box(go.transform, "PostR", pos + new Vector2(0.95f, 0.15f), new Vector2(0.3f, 0.3f));

        g.approachRadius = 2.2f;
        g.frontOffset = new Vector2(1.6f, 0.2f);
        blocked.Add(new Rect(pos.x - 1.8f, pos.y - 1f, 5.5f, 4f));
        return g;
    }

    static void MakeSign(Transform parent, WorldMapSpot spot, Vector2 pos)
    {
        var root = new GameObject("Sign_" + spot.name, typeof(SortingGroup));
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.GetComponent<SortingGroup>().sortingOrder = OrderSign;
        Child(root.transform, "Board", sign, Vector2.zero, 0);

        var textGo = new GameObject("Text", typeof(TextMeshPro));
        textGo.transform.SetParent(root.transform, false);
        textGo.transform.localPosition = Art.SignBoardCenter;
        var tmp = textGo.GetComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = Art.SignBoardSize;
        if (font != null) tmp.font = font;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 3.6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Art.InkC;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.sortingOrder = 1;
        tmp.text = "";

        spot.signRoot = root;
        spot.signText = tmp;
        root.SetActive(false);
    }

    // ─── プレイヤー ───────────────────────────────────────────────
    static WorldMapVehicle MakeRobot(Vector2 pos)
    {
        var go = new GameObject("Player", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SortingGroup), typeof(WorldMapDepthSort), typeof(WorldMapVehicle));
        go.transform.position = pos;
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.GetComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.offset = new Vector2(0f, 0.2f);
        go.GetComponent<SortingGroup>().sortingOrder = WorldMapDepthSort.OrderFor(pos.y);

        var body = Child(go.transform, "Body", robot, Vector2.zero, 0);
        var v = go.GetComponent<WorldMapVehicle>();
        v.body = body.transform;
        v.rotateToFace = false;
        v.moveSpeed = 4.5f;
        v.bobHeight = 0.05f;
        return v;
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

    static WorldMapDetailPanel MakeDetailPanel(RectTransform canvas)
    {
        var root = UIBox(canvas, "DetailPanel", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        var panel = root.gameObject.AddComponent<WorldMapDetailPanel>();

        // 看板と同じ、金属枠＋紙
        var frame = UIBox(root, "Frame", Vector2.zero, new Vector2(1000, 640), Art.InkC);
        UIBox(frame, "Metal", Vector2.zero, new Vector2(988, 628), Art.Shade);
        UIBox(frame, "BoardLine", Vector2.zero, new Vector2(938, 578), Art.InkC);
        UIBox(frame, "Board", Vector2.zero, new Vector2(930, 570), Art.Paper);
        foreach (var b in new[] { new Vector2(-472, -292), new Vector2(472, -292), new Vector2(-472, 292), new Vector2(472, 292) })
            UIBox(frame, "Bolt", b, new Vector2(14, 14), Art.InkC);

        panel.titleLabel = UIText(frame, "Title", new Vector2(0, 220), new Vector2(880, 80), 60, TextAlignmentOptions.Center);
        panel.statusLabel = UIText(frame, "Status", new Vector2(0, 155), new Vector2(880, 50), 36, TextAlignmentOptions.Center);
        UIBox(frame, "Line", new Vector2(0, 118), new Vector2(840, 3), Art.InkC);

        UIText(frame, "RewardHead", new Vector2(-220, 72), new Vector2(380, 44), 30, TextAlignmentOptions.Left).text = "主な報酬";
        panel.rewardLabel = UIText(frame, "Rewards", new Vector2(-220, -18), new Vector2(380, 140), 34, TextAlignmentOptions.TopLeft);

        UIText(frame, "TimeHead", new Vector2(220, 72), new Vector2(380, 44), 30, TextAlignmentOptions.Left).text = "クリアタイム";
        panel.timeLabel = UIText(frame, "Time", new Vector2(220, 22), new Vector2(380, 56), 44, TextAlignmentOptions.Left);

        UIText(frame, "StarHead", new Vector2(220, -46), new Vector2(380, 44), 30, TextAlignmentOptions.Left).text = "評価";
        panel.stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var s = UIBox(frame, "Star_" + i, new Vector2(70 + i * 88, -112), new Vector2(76, 76), Color.white);
            var img = s.GetComponent<Image>();
            img.sprite = star;
            panel.stars[i] = img;
        }
        panel.starOnColor = Art.InkC;
        panel.starOffColor = new Color(0f, 0f, 0f, 0.15f);
        panel.clearedColor = new Color(0.20f, 0.40f, 0.22f);
        panel.notClearedColor = Art.Dark;

        UIBox(frame, "Line2", new Vector2(0, -180), new Vector2(840, 3), Art.InkC);
        panel.hintLabel = UIText(frame, "Hint", new Vector2(0, -230), new Vector2(880, 50), 32, TextAlignmentOptions.Center);

        root.gameObject.SetActive(false);
        return panel;
    }

    static void MakeHud(RectTransform canvas)
    {
        var line = UIBox(canvas, "Controls", Vector2.zero, new Vector2(906, 66), Art.InkC);
        line.anchorMin = line.anchorMax = new Vector2(0f, 0f);
        line.pivot = new Vector2(0f, 0f);
        line.anchoredPosition = new Vector2(24, 24);
        UIBox(line, "Paper", Vector2.zero, new Vector2(900, 60), Art.Paper);
        var t = UIText(line, "Text", Vector2.zero, new Vector2(880, 60), 28, TextAlignmentOptions.Center);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.text = "移動：WASD／十字キー　　決定：Space／A　　戻る：Esc／B";
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
    // 足元の位置に置くスプライト。前後関係は足元のYで決める
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

    // グループの中の部品
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

    // ─── ステージ情報 ─────────────────────────────────────────────
    static WorldMapStageData[] CreateStageAssets()
    {
        Directory.CreateDirectory(StageDir);
        var result = new WorldMapStageData[Stages.Length];
        for (int i = 0; i < Stages.Length; i++)
        {
            var spec = Stages[i];
            string path = StageDir + "/Stage_" + spec.id + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<WorldMapStageData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WorldMapStageData>();
                data.stageId = spec.id;
                data.stageNumber = spec.id;
                data.stageName = spec.name;
                data.sceneName = spec.scene;
                data.rewards = spec.rewards;
                data.unlockedByDefault = i == 0;
                AssetDatabase.CreateAsset(data, path);
            }
            result[i] = data;
        }
        for (int i = 1; i < result.Length; i++)
        {
            if (result[i].unlockAfter == null && !result[i].unlockedByDefault)
            {
                result[i].unlockAfter = result[i - 1];
                EditorUtility.SetDirty(result[i]);
            }
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
