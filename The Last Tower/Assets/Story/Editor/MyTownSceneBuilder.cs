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
/// </summary>
public static class MyTownSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MyTownScene.unity";
    const string FontPath = "Assets/Sasaki/JA_KO SDF.asset";

    // 仕様書のレイアウト配置（画面に対する割合。y は上が1）
    struct Spot
    {
        public string id, name; public float x, y;
        public Spot(string id, string name, float x, float y) { this.id = id; this.name = name; this.x = x; this.y = y; }
    }

    static readonly Spot[] Spots =
    {
        new Spot("loadout", "ロードアウト", 0.12f, 0.24f),
        new Spot("factory", "工場",         0.28f, 0.58f),
        new Spot("hall",    "会館",         0.50f, 0.70f),
        new Spot("storage", "倉庫",         0.72f, 0.54f),
        new Spot("lab",     "ラボ",         0.88f, 0.24f),
    };

    [MenuItem("Tools/Story/Build MyTown Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

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

        // 2.5D 背景（奥ほど動きが小さい）。画像なしなので色の帯で奥行きを出す
        var backdrop = Layer(canvasRect, "Layer_Sky", new Color(0.10f, 0.12f, 0.22f), 0f, 1f, 2400f);
        var hills = Layer(canvasRect, "Layer_Hills", new Color(0.07f, 0.09f, 0.16f), 0f, 0.45f, 2400f);
        var town = Layer(canvasRect, "Layer_Town", new Color(0.04f, 0.05f, 0.09f), 0f, 0.28f, 2400f);
        var ground = Layer(canvasRect, "Layer_Ground", new Color(0.02f, 0.02f, 0.04f), 0f, 0.12f, 2400f);

        // 場所ボタン
        var buttons = new MyTownLocationButton[Spots.Length];
        for (int i = 0; i < Spots.Length; i++)
            buttons[i] = MakeLocation(canvasRect, Spots[i], font);

        // 起動時と同じく、左端の場所を選択状態にしてシーン上に見せる
        buttons[0].background.color = buttons[0].selectedColor;
        buttons[0].transform.localScale = Vector3.one * buttons[0].selectedScale;

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
        ctrl.parallaxLayers = new[] { backdrop, hills, town, ground };
        ctrl.parallaxStrength = new[] { 20f, 45f, 80f, 130f };
        ctrl.audioSource = ctrlGo.GetComponent<AudioSource>();
        ctrl.audioSource.playOnAwake = false;
        UnityEventTools.AddPersistentListener(backButton.onClick, ctrl.ClosePanel);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Debug.Log("[MyTownSceneBuilder] " + ScenePath + " を作成しました");
    }

    static RectTransform Layer(RectTransform parent, string name, Color color, float anchorY0, float anchorY1, float width)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, anchorY0);
        rt.anchorMax = new Vector2(0.5f, anchorY1);
        rt.sizeDelta = new Vector2(width, 0f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    static MyTownLocationButton MakeLocation(RectTransform parent, Spot spot, TMP_FontAsset font)
    {
        var go = new GameObject("Location_" + spot.id, typeof(RectTransform), typeof(Image), typeof(MyTownLocationButton));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(spot.x, spot.y);
        rt.sizeDelta = new Vector2(280, 150);
        rt.anchoredPosition = Vector2.zero;

        var label = MakeText(rt, "Label", spot.name, font, 44, new Vector2(0.5f, 0.5f), new Vector2(270, 140));
        label.transform.SetParent(rt, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        label.rectTransform.anchoredPosition = Vector2.zero;
        label.raycastTarget = false;

        var btn = go.GetComponent<MyTownLocationButton>();
        btn.locationId = spot.id;
        btn.displayName = spot.name;
        btn.background = go.GetComponent<Image>();
        btn.label = label;
        // 実行しなくてもシーン上で見えるよう、通常時の色を焼き込む
        btn.background.color = btn.normalColor;
        return btn;
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
