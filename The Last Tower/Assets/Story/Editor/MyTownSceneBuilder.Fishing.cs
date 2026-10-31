using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マイタウンの釣り部分（町の手前の池と、釣り画面）を組み立てる
/// MyTownSceneBuilder.Build から呼ばれる
///
/// 画面に映すのは、プレイヤーの後ろから池を見る構図（BackView）
/// 池を真上から見た図（Camera 以下）は計算用に残しているだけで、透明にして隠しておく
/// </summary>
public static partial class MyTownSceneBuilder
{
    const string SoundFolder = "Assets/Sound/";
    const string FishSidePath = "Assets/Sprites/Fishing/fish_side.png";
    const string FishTopPath = "Assets/Sprites/Fishing/fish_top.png";

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
        // 真上から見た図は計算用。画面には出さない
        var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
        contentGroup.alpha = 0f;
        contentGroup.blocksRaycasts = false;
        contentGroup.interactable = false;
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
        // 後ろから見た画面（演出UIの下）
        fishing.backView = BuildBackView(view, fishing, font);

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

        // SE（Assets/Sound/Fishing の仮SE。無いものは空のまま）
        fishing.castSE = LoadSound("Fishing/fish_cast.wav");
        fishing.splashSE = LoadSound("Fishing/fish_splash.wav");
        fishing.nibbleSE = LoadSound("Fishing/fish_plop.wav");
        fishing.biteSE = LoadSound("Fishing/fish_bite.wav");
        fishing.hookSE = LoadSound("Fishing/fish_hook.wav");
        fishing.catchSE = LoadSound("Fishing/fish_catch.wav");
        fishing.failSE = LoadSound("Fishing/fish_fail.wav");
        fishing.snapSE = LoadSound("Fishing/fish_snap.wav");
        fishing.jumpSE = LoadSound("Fishing/fish_jump.wav");
        fishing.reelSE = LoadSound("Fishing/fish_reel.wav");
        fishing.dragSE = LoadSound("Fishing/fish_drag.wav");

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

        // 吹き出しはロボより内側（プレイヤーの頭上）に出して、寄った時に画面の右へはみ出さないようにする
        var bubble = Center(robotRoot, "Bubble", new Vector2(-170f, 150f), new Vector2(520f, 76f), new Color(0.95f, 0.96f, 0.98f, 0.95f), null);
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

    // ─── 後ろから見た画面 ─────────────────────────────────────────
    // プレイヤーの後ろから池を見る構図。手前右下から竿が大きく伸び、奥へ水面が横長に広がる
    static FishingBackView BuildBackView(RectTransform view, MyTownFishing fishing, TMP_FontAsset font)
    {
        var go = new GameObject("BackView", typeof(RectTransform), typeof(FishingCameraRig), typeof(FishingBackView));
        var root = go.GetComponent<RectTransform>();
        root.SetParent(view, false);
        Stretch(root);
        var fv = go.GetComponent<FishingBackView>();
        fv.font = font;
        fv.fishTop = LoadSprite(FishTopPath);
        fv.fishSide = LoadSprite(FishSidePath);
        fishing.fishSide = fv.fishSide;
        fv.rig = go.GetComponent<FishingCameraRig>();
        fv.rig.maxShake = 30f;
        fv.circle = circle;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bg.SetParent(root, false);
        Stretch(bg);
        bg.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f);
        bg.GetComponent<Image>().raycastTarget = false;

        var cam = Layer(root, "Camera");
        fv.rig.root = cam;
        fv.world = cam;

        // 夜空と月
        Center(cam, "Sky", new Vector2(0f, 420f), new Vector2(2800f, 640f), new Color(0.07f, 0.09f, 0.16f), null);
        Center(cam, "MoonGlow", new Vector2(560f, 360f), new Vector2(300f, 300f), new Color(0.8f, 0.85f, 0.9f, 0.1f), circle);
        Center(cam, "Moon", new Vector2(560f, 360f), new Vector2(110f, 110f), new Color(0.88f, 0.9f, 0.82f), circle);

        // 水面：奥（地平線）ほど明るく、手前ほど暗い帯を重ねる
        const float horizon = 130f;
        Color far = new Color(0.17f, 0.25f, 0.38f), near = new Color(0.04f, 0.09f, 0.16f);
        const int bands = 10;
        for (int i = 0; i < bands; i++)
        {
            float t0 = i / (float)bands, t1 = (i + 1) / (float)bands;
            float top = Mathf.Lerp(horizon, -700f, t0 * t0);
            float bottom = Mathf.Lerp(horizon, -700f, t1 * t1);
            Box(cam, "Water_" + i, new Vector2(0f, bottom), new Vector2(2800f, top - bottom + 1f), Color.Lerp(far, near, t0));
        }
        // 水面に映る月（地平線から手前へ伸びる光の帯）
        for (int i = 0; i < 7; i++)
        {
            float y = horizon - 20f - i * i * 9f;
            Center(cam, "MoonStreak_" + i, new Vector2(520f - i * 6f, y), new Vector2(120f + i * 30f, 10f + i * 2f), new Color(0.88f, 0.9f, 0.82f, 0.22f - i * 0.025f), circle);
        }
        // 向こう岸と木のシルエット
        var bank = new Color(0.04f, 0.05f, 0.08f);
        Box(cam, "FarBank", new Vector2(0f, horizon - 6f), new Vector2(2800f, 40f), bank);
        for (int i = 0; i < 14; i++)
        {
            float x = -1300f + i * 200f + (i * 53 % 90);
            float s = 120f + (i * 37 % 110);
            Center(cam, "Tree_" + i, new Vector2(x, horizon + s * 0.35f), new Vector2(s * 1.2f, s), bank, circle);
        }

        // 水面の小物：水中の魚の影・波紋・サーチの波・狙いの的
        fv.shadowLayer = Layer(cam, "Shadows");
        fv.rippleLayer = Layer(cam, "Ripples");
        var scan = Center(cam, "ScanRing", Vector2.zero, Vector2.zero, new Color(0.4f, 0.9f, 1f, 0f), circle);
        fv.scanRing = scan.GetComponent<Image>();
        scan.gameObject.SetActive(false);
        fv.reticle = Center(cam, "Reticle", Vector2.zero, new Vector2(150f, 44f), new Color(1f, 1f, 1f, 0.22f), circle);
        Center(fv.reticle, "Dot", Vector2.zero, new Vector2(18f, 7f), new Color(1f, 1f, 1f, 0.9f), circle).anchorMin =
            new Vector2(0.5f, 0.5f);

        // ファイト中の魚（水面に出た背中・背びれ・引き波）
        var fish = Layer(cam, "Fish");
        fv.fish = fish;
        fv.wake = Center(fish, "Wake", Vector2.zero, new Vector2(100f, 30f), new Color(1f, 1f, 1f, 0f), circle).GetComponent<Image>();
        var fin = Box(fish, "Fin", Vector2.zero, new Vector2(30f, 30f), new Color(0.05f, 0.08f, 0.12f, 0.9f));
        fin.pivot = new Vector2(0.5f, 0.5f);
        fin.localRotation = Quaternion.Euler(0f, 0f, 45f);
        fv.fishFin = fin;
        var backSquash = Layer(fish, "BackSquash");
        backSquash.localScale = new Vector3(1f, 0.45f, 1f);
        fv.fishBack = Center(backSquash, "Back", Vector2.zero, new Vector2(100f, 44f), fv.backColor, fv.fishTop).GetComponent<Image>();

        // 水から飛び出した魚（エラ洗い・釣り上げ）
        var air = Layer(cam, "AirFish");
        fv.airFish = air;
        fv.airTail = Center(air, "Tail", new Vector2(-50f, 0f), new Vector2(40f, 30f), Color.white, circle).GetComponent<Image>();
        fv.airBody = Center(air, "Body", Vector2.zero, new Vector2(120f, 45f), Color.white, circle).GetComponent<Image>();
        var eye = Center(fv.airBody.rectTransform, "Eye", Vector2.zero, new Vector2(10f, 10f), Color.black, circle);
        eye.anchorMin = eye.anchorMax = new Vector2(0.8f, 0.58f);
        air.gameObject.SetActive(false);

        fv.sprayLayer = Layer(cam, "Spray");

        // 糸と竿（どちらも原点に置き、頂点はカメラ座標で描く）
        fv.line = MakeCurve(cam, "Line", 3f, new Color(0.92f, 0.94f, 1f, 0.9f));

        // ウキ（原点が水面。上半分が赤、下が白、上に細い棒）
        var bobber = Layer(cam, "Bobber");
        fv.bobber = bobber;
        Center(bobber, "Bottom", new Vector2(0f, 6f), new Vector2(22f, 16f), Color.white, circle);
        fv.bobberImage = Center(bobber, "Top", new Vector2(0f, 16f), new Vector2(24f, 22f), new Color(0.92f, 0.25f, 0.2f), circle).GetComponent<Image>();
        Box(bobber, "Stick", new Vector2(0f, 24f), new Vector2(4f, 18f), new Color(0.92f, 0.25f, 0.2f));

        fv.rod = MakeCurve(cam, "Rod", 18f, new Color(0.55f, 0.40f, 0.24f));

        // プレイヤーの手とリール（竿の根元。下の案内バーに隠れない高さ）
        var purple = new Color(0.62f, 0.30f, 0.85f);
        Center(cam, "Sleeve", new Vector2(440f, -520f), new Vector2(260f, 190f), purple * 0.8f + new Color(0f, 0f, 0f, 0.2f), circle);
        fv.reel = Center(cam, "Reel", new Vector2(250f, -390f), new Vector2(84f, 84f), new Color(0.72f, 0.75f, 0.8f), circle);
        var handle = Box(fv.reel, "Handle", Vector2.zero, new Vector2(12f, 56f), new Color(0.3f, 0.3f, 0.35f));
        handle.anchoredPosition = Vector2.zero;
        Center(fv.reel, "Knob", new Vector2(0f, 54f), new Vector2(26f, 26f), new Color(0.95f, 0.75f, 0.3f), circle);
        Center(cam, "Hand", new Vector2(330f, -430f), new Vector2(110f, 90f), new Color(0.75f, 0.50f, 0.95f), circle);

        // 合図の記号（横走りの向き・エラ洗いの予兆・竿を倒す向き）
        fv.runArrow = MakeText(cam, "RunArrow", "≫", font, 110f, new Vector2(0.5f, 0.5f), new Vector2(220f, 140f));
        fv.runArrow.color = new Color(0.9f, 0.97f, 1f);
        fv.runArrow.fontStyle = FontStyles.Bold;
        fv.jumpMark = MakeText(cam, "JumpMark", "！", font, 150f, new Vector2(0.5f, 0.5f), new Vector2(200f, 200f));
        fv.jumpMark.color = new Color(1f, 0.82f, 0.25f);
        fv.jumpMark.fontStyle = FontStyles.Bold;
        fv.tiltHint = MakeText(cam, "TiltHint", "竿を右へ ▶", font, 44f, new Vector2(0.5f, 0.5f), new Vector2(420f, 70f));
        fv.tiltHint.color = new Color(1f, 0.82f, 0.25f);
        fv.tiltHint.fontStyle = FontStyles.Bold;
        foreach (var t in new[] { fv.runArrow, fv.jumpMark, fv.tiltHint })
        {
            t.raycastTarget = false;
            t.gameObject.SetActive(false);
        }

        // ロボ（左下。クリックでサーチ／アシスト）
        var face = new GameObject("Robot", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
        face.SetParent(root, false);
        face.anchorMin = face.anchorMax = new Vector2(0f, 0f);
        face.sizeDelta = new Vector2(150f, 130f);
        face.anchoredPosition = new Vector2(150f, 290f);
        fv.robot = face;
        // 体（頭の下）
        Box(face, "Body", new Vector2(0f, -150f), new Vector2(100f, 90f), new Color(0.62f, 0.66f, 0.72f)).GetComponent<Image>().raycastTarget = false;
        var faceImage = face.GetComponent<Image>();
        faceImage.sprite = circle;
        faceImage.color = new Color(0.72f, 0.76f, 0.82f);
        UnityEventTools.AddPersistentListener(face.GetComponent<Button>().onClick, fishing.RobotAction);
        Center(face, "Visor", new Vector2(0f, 2f), new Vector2(104f, 56f), new Color(0.08f, 0.10f, 0.14f), circle).GetComponent<Image>().raycastTarget = false;
        fv.robotEye = Center(face, "Eye", new Vector2(0f, 2f), new Vector2(42f, 26f), fv.eyeNormal, circle).GetComponent<Image>();
        fv.robotEye.raycastTarget = false;
        Box(face, "Antenna", new Vector2(0f, 60f), new Vector2(6f, 30f), new Color(0.62f, 0.66f, 0.72f));
        Center(face, "AntennaTip", new Vector2(0f, 96f), new Vector2(18f, 18f), new Color(0.95f, 0.75f, 0.3f), circle);

        var bubble = Center(root, "Bubble", Vector2.zero, new Vector2(560f, 84f), new Color(0.95f, 0.96f, 0.98f, 0.95f), null);
        bubble.anchorMin = bubble.anchorMax = new Vector2(0f, 0f);
        bubble.pivot = new Vector2(0f, 0.5f);
        bubble.anchoredPosition = new Vector2(250f, 320f);
        bubble.GetComponent<Image>().raycastTarget = false;
        var bubbleText = MakeText(bubble, "Text", "", font, 32f, new Vector2(0.5f, 0.5f), new Vector2(540f, 78f));
        bubbleText.color = new Color(0.1f, 0.12f, 0.18f);
        bubbleText.enableAutoSizing = true;
        bubbleText.fontSizeMin = 18f;
        bubbleText.fontSizeMax = 32f;
        bubbleText.raycastTarget = false;
        fv.bubble = bubble;
        fv.bubbleText = bubbleText;
        bubble.gameObject.SetActive(false);

        foreach (var img in cam.GetComponentsInChildren<Image>(true)) img.raycastTarget = false;
        return fv;
    }

    static FishingLineGraphic MakeCurve(RectTransform parent, string name, float thickness, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(FishingLineGraphic));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = Vector2.zero;
        var curve = go.GetComponent<FishingLineGraphic>();
        curve.thickness = thickness;
        curve.color = color;
        curve.raycastTarget = false;
        return curve;
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
        fishing.resultFish = Center(panel, "Fish", new Vector2(0f, 90f), new Vector2(220f, 110f), Color.white, LoadSprite(FishSidePath)).GetComponent<Image>();
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
        log.fishSprite = LoadSprite(FishSidePath);
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

    // 魚の形の画像を、UI で使えるスプライトとして読み込む（取り込み設定もここで直す）
    static Sprite LoadSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static AudioClip LoadSound(string file)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + file);
    }
}
