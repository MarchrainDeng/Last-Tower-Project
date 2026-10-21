using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// マイタウンのシーンを組み立てるエディタツール
/// メニュー Tools/Story/Build MyTown Scene で Assets/Scenes/MyTownScene.unity を作り直す
/// （仕様書の配置：①ロードアウト ②工場 ③会館 ④倉庫 ⑤ラボ）
///
/// 画像はまだ無いので、四角と丸だけで遠近感のある町を作る
/// - 奥（上）ほど建物が小さく、霧で青白くかすむ
/// - 地平線の奥に城と屋根のシルエット、手前ほど暗い地面、奥へ細くなる道
/// </summary>
public static class MyTownSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MyTownScene.unity";
    const string FontPath = "Assets/Sasaki/JA_KO SDF.asset";

    // 地平線の高さ（Canvas中心からのY）
    const float HorizonY = 110f;

    // 場所の配置。x, y は足元の位置（Canvas中心が原点、1920x1080基準）
    struct Spot
    {
        public string id, name;
        public float x, y, width, height;
        public bool tower;
        public Spot(string id, string name, float x, float y, float width, float height, bool tower = false)
        {
            this.id = id; this.name = name; this.x = x; this.y = y;
            this.width = width; this.height = height; this.tower = tower;
        }
    }

    static readonly Spot[] Spots =
    {
        new Spot("loadout", "ロードアウト", -700f, -330f, 300f, 200f),
        new Spot("factory", "工場",         -440f,  -40f, 320f, 210f),
        new Spot("hall",    "会館",            0f,   70f, 520f, 360f, tower: true),
        new Spot("storage", "倉庫",          450f,  -60f, 320f, 190f),
        new Spot("lab",     "ラボ",          700f, -330f, 300f, 220f, tower: true),
    };

    static Sprite circle;

    [MenuItem("Tools/Story/Build MyTown Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // カメラ
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
        cam.orthographic = true;

        // EventSystem（Input System）
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Canvas
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var canvasRect = canvasGo.GetComponent<RectTransform>();

        // 背景（カメラ固定なので動かない）
        var backdrop = Group(canvasRect, "Backdrop");
        BuildSky(backdrop);
        BuildFarTown(backdrop);
        BuildGround(backdrop);

        // 奥行きのある世界（建物とプレイヤー。足元のYで前後が並び替わる）
        var worldGroup = Group(canvasRect, "World");
        var world = worldGroup.gameObject.AddComponent<MyTownWorld>();
        BuildRoad(backdrop, world);

        var buttons = new MyTownLocationButton[Spots.Length];
        for (int i = 0; i < Spots.Length; i++)
            buttons[i] = MakeLocation(worldGroup, Spots[i], font, world);

        var player = MakePlayer(worldGroup, world);
        world.SortByDepth();

        // 手前を暗くして、画面の端を締める
        BuildVignette(canvasRect);

        // 場所UI（半透明の黒）
        var panelGo = new GameObject("LocationPanel", typeof(RectTransform), typeof(Image), typeof(MyTownLocationPanel));
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.SetParent(canvasRect, false);
        Stretch(panelRect);
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        var title = MakeText(panelRect, "Title", "", font, 96, new Vector2(0.5f, 0.6f), new Vector2(1200, 160));
        var backButton = MakeBackButton(panelRect, font);
        panelGo.GetComponent<MyTownLocationPanel>().titleLabel = title;
        panelGo.SetActive(false);

        // コントローラー
        var ctrlGo = new GameObject("MyTownController", typeof(AudioSource), typeof(MyTownController));
        var ctrl = ctrlGo.GetComponent<MyTownController>();
        ctrl.locations = buttons;
        ctrl.panel = panelGo.GetComponent<MyTownLocationPanel>();
        ctrl.player = player;
        ctrl.audioSource = ctrlGo.GetComponent<AudioSource>();
        ctrl.audioSource.playOnAwake = false;
        UnityEventTools.AddPersistentListener(backButton.onClick, ctrl.ClosePanel);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Debug.Log("[MyTownSceneBuilder] " + ScenePath + " を作成しました");
    }

    // ─── 背景 ─────────────────────────────────────────────────────
    static void BuildSky(RectTransform parent)
    {
        var sky = Box(parent, "Sky", Vector2.zero, new Vector2(1920, 1080), new Color(0.06f, 0.07f, 0.13f));
        sky.pivot = new Vector2(0.5f, 0.5f);
        sky.anchoredPosition = Vector2.zero;

        // 地平線に近いほど明るくなる帯（グラデーションの代わり）
        float[] heights = { 330f, 210f, 120f, 60f };
        Color[] colors =
        {
            new Color(0.12f, 0.14f, 0.25f),
            new Color(0.18f, 0.20f, 0.33f),
            new Color(0.25f, 0.27f, 0.40f),
            new Color(0.32f, 0.34f, 0.47f),
        };
        for (int i = 0; i < heights.Length; i++)
            Box(parent, "SkyBand_" + i, new Vector2(0f, HorizonY), new Vector2(1920, heights[i]), colors[i]);

        // 月
        var glow = Circle(parent, "MoonGlow", new Vector2(560f, 300f), new Vector2(320, 320), new Color(0.75f, 0.80f, 0.85f, 0.10f));
        glow.pivot = new Vector2(0.5f, 0.5f);
        var moon = Circle(parent, "Moon", new Vector2(560f, 300f), new Vector2(120, 120), new Color(0.86f, 0.88f, 0.80f));
        moon.pivot = new Vector2(0.5f, 0.5f);
    }

    static void BuildFarTown(RectTransform parent)
    {
        // 一番奥：丘の上の城のシルエット
        var castle = new Color(0.16f, 0.17f, 0.27f);
        Box(parent, "Castle_Hill", new Vector2(220f, HorizonY), new Vector2(760, 70), castle);
        Tower(parent, "Castle_Keep", new Vector2(220f, HorizonY + 60f), new Vector2(200, 170), castle);
        Tower(parent, "Castle_TowerL", new Vector2(70f, HorizonY + 60f), new Vector2(70, 260), castle);
        Tower(parent, "Castle_TowerR", new Vector2(380f, HorizonY + 60f), new Vector2(80, 300), castle);
        Tower(parent, "Castle_Spire", new Vector2(250f, HorizonY + 220f), new Vector2(40, 180), castle);

        // その手前：町の屋根の並び
        var roofs = new Color(0.11f, 0.12f, 0.20f);
        float x = -960f;
        int n = 0;
        while (x < 960f)
        {
            float w = 90f + (n * 37 % 70);
            float h = 50f + (n * 53 % 80);
            Tower(parent, "FarHouse_" + n, new Vector2(x + w * 0.5f, HorizonY - 10f), new Vector2(w, h), roofs);
            x += w + 10f + (n * 29 % 40);
            n++;
        }
    }

    static void BuildGround(RectTransform parent)
    {
        // 地面：地平線の霧から手前の暗がりへ
        Box(parent, "Ground", new Vector2(0f, -540f), new Vector2(1920, 540f + HorizonY), new Color(0.09f, 0.09f, 0.14f));
        Box(parent, "GroundFog_0", new Vector2(0f, HorizonY - 140f), new Vector2(1920, 140f), new Color(0.18f, 0.19f, 0.28f, 0.6f));
        Box(parent, "GroundFog_1", new Vector2(0f, HorizonY - 60f), new Vector2(1920, 60f), new Color(0.30f, 0.32f, 0.44f, 0.6f));
    }

    // 奥に向かって細くなる道（帯を重ねて台形にする）
    static void BuildRoad(RectTransform parent, MyTownWorld world)
    {
        const int steps = 60;
        float bottom = -540f, top = HorizonY - 30f;
        var roadColor = new Color(0.17f, 0.17f, 0.22f);
        for (int i = 0; i < steps; i++)
        {
            float t0 = i / (float)steps, t1 = (i + 1) / (float)steps;
            float y0 = Mathf.Lerp(bottom, top, t0);
            float y1 = Mathf.Lerp(bottom, top, t1);
            float width = Mathf.Lerp(1500f, 300f, Mathf.Pow(t0, 0.8f));
            Box(parent, "Road_" + i, new Vector2(0f, y0), new Vector2(width, y1 - y0 + 1f), world.ApplyFog(roadColor, y0));
        }
    }

    static void BuildVignette(RectTransform parent)
    {
        // 手前の足元を少しずつ暗くする（細かい帯を重ねてグラデーションに見せる）
        const int steps = 12;
        for (int i = 0; i < steps; i++)
        {
            float h = 220f * (1f - i / (float)steps);
            Box(parent, "Vignette_Bottom_" + i, new Vector2(0f, -540f), new Vector2(1920, h), new Color(0f, 0f, 0f, 0.06f));
        }
    }

    // ─── 建物 ─────────────────────────────────────────────────────
    static MyTownLocationButton MakeLocation(RectTransform parent, Spot spot, TMP_FontAsset font, MyTownWorld world)
    {
        var go = new GameObject("Location_" + spot.id, typeof(RectTransform), typeof(MyTownLocationButton));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(spot.width, spot.height);
        rt.anchoredPosition = new Vector2(spot.x, spot.y);
        rt.localScale = Vector3.one * world.ScaleAt(spot.y);

        var btn = go.GetComponent<MyTownLocationButton>();
        Color Fog(Color c) => world.ApplyFog(c, spot.y);

        float w = spot.width, h = spot.height;
        var roofColor = Fog(new Color(0.12f, 0.11f, 0.17f));

        // 足元の影
        var shadow = Circle(rt, "Shadow", new Vector2(0f, 0f), new Vector2(w * 1.25f, 46f), new Color(0f, 0f, 0f, 0.4f));
        shadow.pivot = new Vector2(0.5f, 0.5f);

        // 塔（会館・ラボ）
        float roofTop = h + w * 0.36f;
        if (spot.tower)
        {
            var towerRoot = Tower(rt, "Tower", new Vector2(-w * 0.32f, 0f), new Vector2(w * 0.24f, h * 1.6f), Fog(new Color(0.17f, 0.18f, 0.25f)));
            roofTop = Mathf.Max(roofTop, h * 1.6f + w * 0.12f);
            towerRoot.GetComponent<Image>().raycastTarget = false;
        }

        // 屋根（45度回した四角の上半分を三角屋根に見せる）
        var roof = Box(rt, "Roof", new Vector2(0f, h), new Vector2(w * 0.72f, w * 0.72f), roofColor);
        roof.pivot = new Vector2(0.5f, 0.5f);
        roof.anchoredPosition = new Vector2(0f, h);
        roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
        roof.GetComponent<Image>().raycastTarget = false;

        // 壁（クリック判定もここ）
        var body = Box(rt, "Body", Vector2.zero, new Vector2(w, h), Fog(btn.normalColor));
        btn.background = body.GetComponent<Image>();
        btn.background.raycastTarget = true;

        // 軒
        Box(rt, "Eave", new Vector2(0f, h - 14f), new Vector2(w + 30f, 18f), roofColor).GetComponent<Image>().raycastTarget = false;

        // 扉
        Box(rt, "Door", Vector2.zero, new Vector2(w * 0.18f, h * 0.38f), Fog(new Color(0.05f, 0.04f, 0.06f)))
            .GetComponent<Image>().raycastTarget = false;

        // 窓
        int windowCount = w > 380f ? 4 : 2;
        btn.windows = new Image[windowCount];
        for (int i = 0; i < windowCount; i++)
        {
            float t = windowCount == 1 ? 0.5f : i / (float)(windowCount - 1);
            float x = Mathf.Lerp(-w * 0.34f, w * 0.34f, t);
            if (Mathf.Abs(x) < w * 0.14f) x += Mathf.Sign(x == 0f ? 1f : x) * w * 0.1f;
            var win = Box(rt, "Window_" + i, new Vector2(x, h * 0.5f), new Vector2(w * 0.11f, w * 0.13f), Fog(btn.windowColor));
            win.GetComponent<Image>().raycastTarget = false;
            btn.windows[i] = win.GetComponent<Image>();
        }

        // 看板（屋根の上）
        // 奥の建物は小さくなるので、看板の文字だけは読める大きさに補正する
        float labelSize = 44f / Mathf.Sqrt(world.ScaleAt(spot.y));
        var label = MakeText(rt, "Label", spot.name, font, labelSize, new Vector2(0.5f, 0f), new Vector2(w + 240f, labelSize * 1.6f));
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, roofTop + 18f);
        label.raycastTarget = false;
        label.color = Fog(btn.labelColor);

        btn.locationId = spot.id;
        btn.displayName = spot.name;
        btn.label = label;
        return btn;
    }

    // ─── プレイヤー ───────────────────────────────────────────────
    static MyTownPlayer MakePlayer(RectTransform parent, MyTownWorld world)
    {
        var go = new GameObject("Player", typeof(RectTransform), typeof(MyTownPlayer));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(60, 120);
        var player = go.GetComponent<MyTownPlayer>();
        rt.anchoredPosition = player.startPosition;
        rt.localScale = Vector3.one * world.ScaleAt(player.startPosition.y);

        var purple = new Color(0.62f, 0.30f, 0.85f);
        var shadow = Circle(rt, "Shadow", Vector2.zero, new Vector2(80, 22), new Color(0f, 0f, 0f, 0.45f));
        shadow.pivot = new Vector2(0.5f, 0.5f);
        Box(rt, "Body", new Vector2(0f, 4f), new Vector2(48, 72), purple).GetComponent<Image>().raycastTarget = false;
        Circle(rt, "Head", new Vector2(0f, 76f), new Vector2(42, 42), new Color(0.75f, 0.50f, 0.95f));
        return player;
    }

    // ─── 部品 ─────────────────────────────────────────────────────
    static RectTransform Group(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Stretch(rt);
        return rt;
    }

    // 下端中央をpivotにした四角。position は下端中央の位置
    static RectTransform Box(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static RectTransform Circle(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var rt = Box(parent, name, position, size, color);
        var img = rt.GetComponent<Image>();
        img.sprite = circle;
        img.raycastTarget = false;
        return rt;
    }

    // 三角屋根つきの細長い建物（シルエット用）。position は下端中央
    static RectTransform Tower(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var body = Box(parent, name, position, size, color);
        float side = size.x * 0.72f;
        var roof = Box(body, "Roof", Vector2.zero, new Vector2(side, side), color);
        roof.anchorMin = roof.anchorMax = new Vector2(0.5f, 1f);
        roof.pivot = new Vector2(0.5f, 0.5f);
        roof.anchoredPosition = Vector2.zero;
        roof.localRotation = Quaternion.Euler(0f, 0f, 45f);
        return body;
    }

    static Button MakeBackButton(RectTransform parent, TMP_FontAsset font)
    {
        var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.3f);
        rt.sizeDelta = new Vector2(320, 90);
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.30f, 1f);
        var text = MakeText(rt, "Label", "戻る", font, 44, new Vector2(0.5f, 0.5f), new Vector2(300, 80));
        text.color = Color.black;
        text.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    static TMP_Text MakeText(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Vector2 anchor, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        if (font != null) t.font = font;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AddToBuildSettings()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == ScenePath) return;
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
