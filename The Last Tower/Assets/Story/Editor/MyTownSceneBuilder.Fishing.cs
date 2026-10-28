using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マイタウンの釣り部分（町の手前の池と、釣り画面）を組み立てる
/// MyTownSceneBuilder.Build から呼ばれる
///
/// 釣り画面は池を真上から見た図。下に桟橋とプレイヤー・お助けロボ、上が池の奥
/// 中身はすべて「Camera」の子にして、FishingCameraRig がそれを拡大・移動して寄り引きを作る
/// </summary>
public static partial class MyTownSceneBuilder
{
    const string SoundFolder = "Assets/Sound/";

    // ─── 町の池 ───────────────────────────────────────────────────
    // 道の突き当たり（画面の一番手前）にある池と桟橋。プレイヤーは岸に立って釣りを始める
    static MyTownPond BuildPond(RectTransform parent)
    {
        Circle(parent, "Pond_Shore", new Vector2(0f, -700f), new Vector2(1180f, 300f), new Color(0.13f, 0.12f, 0.12f));
        var water = Circle(parent, "Pond", new Vector2(0f, -690f), new Vector2(1100f, 260f), new Color(0.10f, 0.17f, 0.28f));
        var waterImage = water.GetComponent<Image>();
        waterImage.raycastTarget = true;
        var pond = water.gameObject.AddComponent<MyTownPond>();

        // 水面に映る月
        Circle(parent, "Pond_Moon", new Vector2(180f, -520f), new Vector2(180f, 26f), new Color(0.85f, 0.88f, 0.80f, 0.35f));
        Circle(parent, "Pond_Moon2", new Vector2(200f, -545f), new Vector2(110f, 14f), new Color(0.85f, 0.88f, 0.80f, 0.2f));

        // 桟橋（岸から手前へ伸びる）
        var wood = new Color(0.27f, 0.20f, 0.14f);
        Box(parent, "Pier", new Vector2(0f, -570f), new Vector2(120f, 135f), wood);
        for (int i = 0; i < 4; i++)
            Box(parent, "Pier_Plank_" + i, new Vector2(0f, -560f + i * 32f), new Vector2(120f, 4f), new Color(0.17f, 0.12f, 0.09f));
        return pond;
    }

    // 池の前に来ると頭の上に出る案内
    static GameObject MakePondPrompt(RectTransform player, TMP_FontAsset font)
    {
        var bg = Box(player, "PondPrompt", new Vector2(0f, 140f), new Vector2(330f, 56f), new Color(0f, 0f, 0f, 0.6f));
        var text = MakeText(bg, "Label", "A / Enter  釣りをする", font, 26f, new Vector2(0.5f, 0.5f), new Vector2(330f, 56f));
        text.color = new Color(1f, 0.88f, 0.5f);
        text.raycastTarget = false;
        bg.gameObject.SetActive(false);
        return bg.gameObject;
    }

    // ─── 釣り画面 ─────────────────────────────────────────────────
    static MyTownFishing BuildFishing(RectTransform canvas, RectTransform townView, TMP_FontAsset font)
    {
        var fishingGo = new GameObject("MyTownFishing", typeof(AudioSource), typeof(MyTownFishing));
        var fishing = fishingGo.GetComponent<MyTownFishing>();
        fishing.audioSource = fishingGo.GetComponent<AudioSource>();
        fishing.audioSource.playOnAwake = false;
        fishing.townView = townView;
        fishing.circle = circle;
        fishing.font = font;

        var viewGo = new GameObject("FishingView", typeof(RectTransform), typeof(CanvasGroup), typeof(FishingCameraRig));
        var view = viewGo.GetComponent<RectTransform>();
        view.SetParent(canvas, false);
        Stretch(view);
        fishing.view = viewGo.GetComponent<CanvasGroup>();
        fishing.rig = viewGo.GetComponent<FishingCameraRig>();

        // カメラ外が見えた時用の地面色
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bg.SetParent(view, false);
        Stretch(bg);
        bg.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f);

        var content = Center(view, "Camera", Vector2.zero, Vector2.zero, Color.clear, null);
        Object.DestroyImmediate(content.GetComponent<Image>());
        fishing.content = content;
        fishing.rig.root = content;

        BuildPondView(content, fishing);
        fishing.rippleLayer = Layer(content, "Ripples");
        fishing.fishLayer = Layer(content, "Fish");
        BuildPierView(content, fishing, font);

        // 釣り糸（原点に置き、頂点はカメラ座標で描く）
        var lineGo = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(FishingLineGraphic));
        var lineRect = lineGo.GetComponent<RectTransform>();
        lineRect.SetParent(content, false);
        lineRect.sizeDelta = Vector2.zero;
        fishing.line = lineGo.GetComponent<FishingLineGraphic>();
        fishing.line.raycastTarget = false;

        // ウキ
        fishing.bobber = Center(content, "Bobber", Vector2.zero, new Vector2(28f, 28f), new Color(0.92f, 0.25f, 0.2f), circle);
        fishing.bobberImage = fishing.bobber.GetComponent<Image>();
        Center(fishing.bobber, "Top", new Vector2(0f, 3f), new Vector2(12f, 12f), Color.white, circle);

        // 狙いの印
        fishing.reticle = Center(content, "Reticle", new Vector2(0f, 350f), new Vector2(80f, 80f), new Color(1f, 1f, 1f, 0.15f), circle);
        Center(fishing.reticle, "Dot", Vector2.zero, new Vector2(14f, 14f), new Color(1f, 1f, 1f, 0.85f), circle);

        // サーチの輪
        var scan = Center(content, "ScanRing", Vector2.zero, Vector2.zero, new Color(0.4f, 0.9f, 1f, 0f), circle);
        fishing.robot.scanRing = scan.GetComponent<Image>();
        scan.gameObject.SetActive(false);

        // 釣り上げた魚（跳ねる）
        var catchFish = Center(content, "CatchFish", fishing.landingPoint, Vector2.zero, Color.clear, null);
        Object.DestroyImmediate(catchFish.GetComponent<Image>());
        fishing.catchFish = catchFish;
        fishing.catchFishTail = Center(catchFish, "Tail", new Vector2(-50f, 0f), new Vector2(40f, 30f), Color.white, circle).GetComponent<Image>();
        fishing.catchFishBody = Center(catchFish, "Body", Vector2.zero, new Vector2(120f, 45f), Color.white, circle).GetComponent<Image>();
        var eye = Center(fishing.catchFishBody.rectTransform, "Eye", Vector2.zero, new Vector2(10f, 10f), Color.black, circle);
        eye.anchorMin = eye.anchorMax = new Vector2(0.8f, 0.58f);
        catchFish.gameObject.SetActive(false);

        // ─── 画面に固定のUI ───
        var flash = new GameObject("Flash", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        flash.SetParent(view, false);
        Stretch(flash);
        fishing.flash = flash.GetComponent<Image>();
        fishing.flash.color = new Color(1f, 1f, 1f, 0f);
        fishing.flash.raycastTarget = false;

        fishing.bigText = MakeText(view, "BigText", "", font, 130f, new Vector2(0.5f, 0.66f), new Vector2(1600f, 220f));
        fishing.bigText.raycastTarget = false;
        fishing.bigText.fontStyle = FontStyles.Bold;

        // 図鑑ボタン（右上）
        var logGo = new GameObject("LogButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var logRect = logGo.GetComponent<RectTransform>();
        logRect.SetParent(view, false);
        logRect.anchorMin = logRect.anchorMax = new Vector2(1f, 1f);
        logRect.pivot = new Vector2(1f, 1f);
        logRect.anchoredPosition = new Vector2(-40f, -40f);
        logRect.sizeDelta = new Vector2(240f, 70f);
        logGo.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 0.9f);
        var logLabel = MakeText(logRect, "Label", "図鑑  Tab", font, 30f, new Vector2(0.5f, 0.5f), new Vector2(230f, 60f));
        logLabel.raycastTarget = false;
        UnityEventTools.AddPersistentListener(logGo.GetComponent<Button>().onClick, fishing.OpenLog);
        fishing.logButton = logRect;

        BuildResultPanel(view, fishing, font);
        BuildLogPanel(view, fishing, font);

        // 操作の案内（下）
        var hintBg = new GameObject("HintBar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        hintBg.SetParent(view, false);
        hintBg.anchorMin = new Vector2(0f, 0f);
        hintBg.anchorMax = new Vector2(1f, 0f);
        hintBg.pivot = new Vector2(0.5f, 0f);
        hintBg.sizeDelta = new Vector2(0f, 110f);
        hintBg.anchoredPosition = Vector2.zero;
        var hintImage = hintBg.GetComponent<Image>();
        hintImage.color = new Color(0f, 0f, 0f, 0.45f);
        hintImage.raycastTarget = false;
        fishing.hintText = MakeText(hintBg, "Hint", "", font, 28f, new Vector2(0.5f, 0.5f), new Vector2(1800f, 100f));
        fishing.hintText.color = new Color(0.85f, 0.88f, 0.95f);
        fishing.hintText.raycastTarget = false;

        // SE（無いものは空のまま）
        fishing.castSE = LoadSound("UI Move.mp3");
        fishing.splashSE = LoadSound("drop.mp3");
        fishing.nibbleSE = LoadSound("UI Move.mp3");
        fishing.biteSE = LoadSound("UI Decis.mp3");
        fishing.hookSE = LoadSound("UI Decis.mp3");

        viewGo.SetActive(false);
        return fishing;
    }

    // 池（真上から見た水面）
    static void BuildPondView(RectTransform content, MyTownFishing fishing)
    {
        Rect w = fishing.water;
        Center(content, "Ground", new Vector2(0f, 200f), new Vector2(4200f, 3200f), new Color(0.09f, 0.10f, 0.11f), null);
        Center(content, "Bank", w.center, w.size + new Vector2(90f, 90f), new Color(0.17f, 0.15f, 0.12f), null);
        Center(content, "Water", w.center, w.size, new Color(0.09f, 0.18f, 0.29f), null);
        // 中央ほど深く暗い
        Center(content, "Deep", w.center + new Vector2(0f, 120f), new Vector2(w.width * 0.85f, w.height * 0.8f), new Color(0.05f, 0.11f, 0.20f, 0.7f), circle);
        Center(content, "Deeper", w.center + new Vector2(0f, 200f), new Vector2(w.width * 0.5f, w.height * 0.45f), new Color(0.03f, 0.08f, 0.16f, 0.6f), circle);
        // 水面に映る月
        Center(content, "MoonReflection", new Vector2(420f, 560f), new Vector2(320f, 70f), new Color(0.85f, 0.88f, 0.80f, 0.16f), circle);
        Center(content, "MoonReflection2", new Vector2(440f, 530f), new Vector2(180f, 26f), new Color(0.85f, 0.88f, 0.80f, 0.22f), circle);

        // 岸辺の草
        var reed = new Color(0.12f, 0.16f, 0.12f);
        Vector2[] reeds =
        {
            new Vector2(-980f, 820f), new Vector2(-930f, 760f), new Vector2(-990f, 300f), new Vector2(950f, 820f),
            new Vector2(990f, 120f), new Vector2(960f, 60f), new Vector2(-960f, -200f), new Vector2(970f, -220f),
        };
        for (int i = 0; i < reeds.Length; i++)
            Center(content, "Reed_" + i, reeds[i], new Vector2(90f + i * 7 % 40, 60f + i * 13 % 30), reed, circle);
    }

    // 桟橋・プレイヤー・竿・リール・ロボット
    static void BuildPierView(RectTransform content, MyTownFishing fishing, TMP_FontAsset font)
    {
        var wood = new Color(0.30f, 0.22f, 0.15f);
        var plank = new Color(0.20f, 0.14f, 0.10f);
        Box(content, "Pier", new Vector2(0f, -720f), new Vector2(240f, 470f), wood);
        for (int i = 0; i < 12; i++)
            Box(content, "Pier_Plank_" + i, new Vector2(0f, -700f + i * 38f), new Vector2(240f, 4f), plank);
        Center(content, "Pier_PostL", new Vector2(-112f, -262f), new Vector2(26f, 26f), plank, circle);
        Center(content, "Pier_PostR", new Vector2(112f, -262f), new Vector2(26f, 26f), plank, circle);

        // プレイヤー（後ろ姿）
        var purple = new Color(0.62f, 0.30f, 0.85f);
        Center(content, "Player_Shadow", new Vector2(-40f, -415f), new Vector2(90f, 34f), new Color(0f, 0f, 0f, 0.4f), circle);
        Box(content, "Player_Body", new Vector2(-40f, -420f), new Vector2(66f, 80f), purple);
        Center(content, "Player_Head", new Vector2(-40f, -322f), new Vector2(50f, 50f), new Color(0.75f, 0.50f, 0.95f), circle);

        // 竿（pivotは手元）
        fishing.rod = Box(content, "Rod", new Vector2(-6f, -370f), new Vector2(8f, fishing.rodLength), new Color(0.62f, 0.48f, 0.30f));
        fishing.rod.GetComponent<Image>().raycastTarget = false;

        // リール（ファイト中だけ出る）
        fishing.reel = Center(content, "Reel", new Vector2(-14f, -360f), new Vector2(36f, 36f), new Color(0.75f, 0.78f, 0.82f), circle);
        var handle = Box(fishing.reel, "Handle", Vector2.zero, new Vector2(6f, 26f), new Color(0.3f, 0.3f, 0.35f));
        handle.anchoredPosition = Vector2.zero;
        Center(fishing.reel, "Knob", new Vector2(0f, 24f), new Vector2(12f, 12f), new Color(0.95f, 0.75f, 0.3f), circle);

        // お助けロボット（クリックでサーチ／アシスト）
        // 中心を原点にした箱（足元は y=-85）。箱全体がクリック判定
        var robotRoot = Center(content, "Robot", new Vector2(110f, -345f), new Vector2(120f, 170f), new Color(0f, 0f, 0f, 0f), null);
        robotRoot.GetComponent<Image>().raycastTarget = true;
        var robot = robotRoot.gameObject.AddComponent<FishingRobot>();
        robot.owner = fishing;
        fishing.robot = robot;

        Center(robotRoot, "Shadow", new Vector2(0f, -95f), new Vector2(80f, 24f), new Color(0f, 0f, 0f, 0.35f), circle);
        var body = Box(robotRoot, "Body", new Vector2(0f, -75f), new Vector2(0f, 0f), Color.clear);
        Object.DestroyImmediate(body.GetComponent<Image>());
        robot.body = body;
        var metal = new Color(0.62f, 0.66f, 0.72f);
        Box(body, "Torso", Vector2.zero, new Vector2(70f, 56f), metal);
        Center(body, "Head", new Vector2(0f, 90f), new Vector2(76f, 64f), new Color(0.72f, 0.76f, 0.82f), circle);
        Center(body, "Visor", new Vector2(0f, 92f), new Vector2(54f, 30f), new Color(0.08f, 0.10f, 0.14f), circle);
        robot.eye = Center(body, "Eye", new Vector2(0f, 92f), new Vector2(22f, 14f), robot.eyeNormal, circle).GetComponent<Image>();
        Box(body, "Antenna", new Vector2(0f, 118f), new Vector2(4f, 22f), metal);
        Center(body, "AntennaTip", new Vector2(0f, 142f), new Vector2(12f, 12f), new Color(0.95f, 0.75f, 0.3f), circle);
        foreach (Transform t in body.GetComponentsInChildren<Transform>())
        {
            var img = t.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }

        // 吹き出し
        var bubble = Center(robotRoot, "Bubble", new Vector2(0f, 150f), new Vector2(520f, 76f), new Color(0.95f, 0.96f, 0.98f, 0.95f), null);
        bubble.GetComponent<Image>().raycastTarget = false;
        var bubbleText = MakeText(bubble, "Text", "", font, 28f, new Vector2(0.5f, 0.5f), new Vector2(500f, 70f));
        bubbleText.color = new Color(0.1f, 0.12f, 0.18f);
        bubbleText.enableAutoSizing = true;
        bubbleText.fontSizeMin = 16f;
        bubbleText.fontSizeMax = 28f;
        bubbleText.raycastTarget = false;
        robot.bubble = bubble;
        robot.bubbleText = bubbleText;
        bubble.gameObject.SetActive(false);
    }

    // 釣果カード
    static void BuildResultPanel(RectTransform view, MyTownFishing fishing, TMP_FontAsset font)
    {
        var panel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.SetParent(view, false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.62f);
        panel.sizeDelta = new Vector2(760f, 420f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.92f);

        fishing.resultBadge = MakeText(panel, "Badge", "", font, 40f, new Vector2(0.5f, 0.9f), new Vector2(700f, 60f));
        fishing.resultBadge.color = new Color(1f, 0.8f, 0.3f);
        fishing.resultFish = Center(panel, "Fish", new Vector2(0f, 90f), new Vector2(240f, 84f), Color.white, circle).GetComponent<Image>();
        fishing.resultName = MakeText(panel, "Name", "", font, 64f, new Vector2(0.5f, 0.5f), new Vector2(700f, 90f));
        fishing.resultName.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        fishing.resultSize = MakeText(panel, "Size", "", font, 44f, new Vector2(0.5f, 0.5f), new Vector2(700f, 60f));
        fishing.resultSize.rectTransform.anchoredPosition = new Vector2(0f, -78f);
        fishing.resultSize.color = new Color(1f, 0.9f, 0.6f);
        fishing.resultDescription = MakeText(panel, "Description", "", font, 26f, new Vector2(0.5f, 0.5f), new Vector2(700f, 70f));
        fishing.resultDescription.rectTransform.anchoredPosition = new Vector2(0f, -150f);
        fishing.resultDescription.color = new Color(0.75f, 0.78f, 0.86f);

        foreach (var g in panel.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        panel.gameObject.SetActive(false);
        fishing.resultPanel = panel.gameObject;
    }

    // さかな図鑑
    static void BuildLogPanel(RectTransform view, MyTownFishing fishing, TMP_FontAsset font)
    {
        var panelGo = new GameObject("LogPanel", typeof(RectTransform), typeof(Image), typeof(FishingLogPanel));
        var panel = panelGo.GetComponent<RectTransform>();
        panel.SetParent(view, false);
        Stretch(panel);
        panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var log = panelGo.GetComponent<FishingLogPanel>();
        log.title = MakeText(panel, "Title", "さかな図鑑", font, 64f, new Vector2(0.5f, 0.9f), new Vector2(1200f, 100f));
        log.grid = Layer(panel, "Grid");
        log.grid.anchoredPosition = new Vector2(0f, -20f);
        log.circle = circle;
        log.font = font;

        panelGo.SetActive(false);
        fishing.logPanel = log;
    }

    // ─── 部品 ─────────────────────────────────────────────────────
    // 中心をpivotにした四角（sprite を渡すと丸）
    static RectTransform Center(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, Sprite sprite)
    {
        var rt = Box(parent, name, position, size, color);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        return rt;
    }

    // 何も描かない入れ物（原点に置く）
    static RectTransform Layer(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    static AudioClip LoadSound(string file)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + file);
    }
}
