using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// マイタウンの池でできる釣りミニゲーム
///
/// 【流れ】
/// 町の池の前で決定 → カメラが池に寄って釣り画面へ（Entering）
/// Aim     : 投げる場所を狙う
/// Casting : 餌が飛んでいく
/// Lure    : 餌を自分で動かして魚を誘う。速く動かすと逃げる。
///           本アタリは 消し込み／居食い／食い上げ の3種類。合わせるタイミングで掛かり具合（浅い・しっかり・深い）が決まり、
///           待つほど深く食い込む（送り込み）。待ちすぎると餌だけ取られる。つつき中の早合わせでもたまに浅く掛かる
/// Fight   : マウスをぐるぐる回してリールを巻く。魚が暴れている時に強く巻くと糸が切れ、
///           巻かずに放っておくとバレる。魚が横に走ったら竿を逆に倒す（竿さばき）と寄せやすく、糸の負担も減る。
///           跳ねる魚はエラ洗いをする。跳んでいる間に巻くと外れる。浅掛かりは外れやすい
///           ファイト中はプレイヤーの後ろから見た画面（FishingBackView）に切り替わり、竿のしなりと魚の走りを大きく見せる。
///           ゲージは出さず、魚の動き・しぶき・糸の張り・竿のしなり・振動・ドラグ音・カメラで伝え、
///           横走りの向き（≫）とエラ洗いの予兆（！）だけ小さな記号を添える。最初の数回はロボが一瞬スローにして教える
/// Landing : 魚が跳ねて釣り上げる演出 → Result（図鑑に記録）
///
/// お助けロボット（FishingRobot）は サーチ（魚を光らせる）/ アタリの合図 / ファイト中のアシスト（1回）をしてくれる
///
/// 【操作】
/// - マウス  : カーソルで狙う、クリックで投げる・合わせる、左ドラッグで餌を動かす、ぐるぐる回して巻く、
///             右ドラッグ中だけ左右で竿を倒す、右クリックで戻る
/// - パッド  : 左スティックで狙う・餌を動かす、Aで投げる・合わせる、左スティックを回して巻く、右スティック左右で竿を倒す、
///             Yでロボ、Viewで図鑑、Bで戻る（ファイト中は引きに合わせて振動する）
/// - キーボード: WASD/矢印で狙う・餌を動かす（Shiftでゆっくり）、Space/Enterで投げる・合わせる、Space長押しで巻く、
///              ファイト中は A/D で竿を倒す、Qでロボ、Tabで図鑑、Esc/Backspaceで戻る
///
/// 計算は池の座標（真上から見た図。下が桟橋、上が池の奥）で行い、画面にはプレイヤーの後ろから見た構図で映す（FishingBackView）
/// 真上から見た図（content 以下）は計算用に残しているだけで、画面には出さない
/// 時間は unscaledDeltaTime に FishingCameraRig のスロー倍率を掛けて進める（町と同じく timeScale は触らない）
/// </summary>
public class MyTownFishing : MonoBehaviour
{
    enum Phase { Hidden, Entering, Aim, Casting, Lure, ReelIn, Fight, Landing, Result, Lost, Log, Exiting }
    enum FightPhase { Calm, Warn, Thrash, JumpWarn, Jump }
    enum HookDepth { Shallow, Solid, Deep }

    [Header("── 町からの切り替え ────────────────")]
    [Tooltip("町の背景と建物をまとめた親。釣り開始時にこれを池へ向けて拡大する")]
    public RectTransform townView;
    [Tooltip("町の中で寄る位置（池の桟橋）")]
    public Vector2 townFocus = new Vector2(0f, -470f);
    public float townZoom = 2.8f;
    public float enterDuration = 1.1f;

    [Header("── 釣り画面 ──────────────────────")]
    public CanvasGroup view;
    public FishingCameraRig rig;
    [Tooltip("プレイヤーの後ろから池を見る画面（実際に映すのはこちら）")]
    public FishingBackView backView;
    public RectTransform content;
    public RectTransform fishLayer;
    public RectTransform rippleLayer;
    public Sprite circle;
    public TMP_FontAsset font;

    [Header("── 池 ────────────────────────")]
    public Rect water = new Rect(-1000f, -260f, 2000f, 1120f);
    [Tooltip("魚をここまで寄せたら釣り上げ")]
    public Vector2 landingPoint = new Vector2(0f, -230f);
    public float castRange = 1000f;
    public int fishCount = 5;
    public FishSpecies[] species = FishSpecies.Defaults();

    [Header("── 道具 ──────────────────────")]
    public RectTransform rod;            // pivotは手元
    public float rodLength = 170f;
    public RectTransform reel;           // ファイト中だけ出る回るリール
    public RectTransform bobber;
    public Image bobberImage;
    public FishingLineGraphic line;
    public RectTransform reticle;
    public FishingRobot robot;

    [Header("── 操作 ──────────────────────")]
    public float aimSpeed = 900f;
    public float baitSpeed = 170f;
    public float slowBaitSpeed = 60f;
    public float stickDeadZone = 0.25f;
    [Tooltip("右ドラッグ1pxで竿が倒れる量（-1〜1）")]
    public float mouseTiltSensitivity = 0.005f;
    [Tooltip("操作していない時に竿がまっすぐに戻る速さ")]
    public float tiltReturnSpeed = 1.5f;

    [Header("── ファイト ────────────────────")]
    [Tooltip("ふつうの速さ（1秒に1回転）で巻いた時に寄ってくる速さ")]
    public float reelPull = 150f;
    [Tooltip("ファイトの長さ。魚の体力にかける倍率（大きいほど疲れにくく、暴れる・休むを何度も繰り返す）")]
    public float fightStaminaScale = 1f;
    [Tooltip("魚の体力の最低値（秒）。小さい魚でもすぐには疲れないようにする")]
    public float staminaFloor = 15f;
    [Tooltip("これより元気なまま足元まで寄せると、最後のひと暴れで沖へ走り出す（0で無効）")]
    [Range(0f, 1f)] public float landingStamina = 0.55f;
    [Tooltip("足元とみなす距離（池の座標）")]
    public float nearPierDistance = 260f;
    [Tooltip("最後のひと暴れで沖へ走る最低の速さ（小さい魚でもしっかり走る）")]
    public float lastRunMinPull = 260f;
    [Tooltip("暴れていない時にこれだけ巻かずにいるとバレる（秒）")]
    public float slackLimit = 2.5f;
    public float assistDuration = 2.5f;
    public float scanCooldown = 5f;
    [Tooltip("つついているだけの時に合わせて、浅く掛かる確率")]
    [Range(0f, 1f)] public float nibbleHookChance = 0.25f;
    [Tooltip("跳ねる魚が、暴れ出す時にエラ洗いをする確率")]
    [Range(0f, 1f)] public float jumpChance = 0.35f;
    [Tooltip("浅掛かりの時、暴れ出すたびに外れる確率")]
    [Range(0f, 1f)] public float shallowHookOutChance = 0.12f;
    [Tooltip("エラ洗いの予兆（水面が泡立ってから跳ぶまで）の時間")]
    public float jumpWarnTime = 0.8f;
    [Tooltip("横走り・エラ洗いを、最初の何回までロボがスローにして教えるか")]
    public int coachCount = 2;
    [Tooltip("パッドの振動の強さ（0で無効）")]
    [Range(0f, 1f)] public float rumbleScale = 1f;

    [Header("── 難しさ ──────────────────────")]
    [Tooltip("本アタリの猶予にかける倍率（小さいほどシビア）")]
    public float biteWindowScale = 0.6f;
    [Tooltip("フェイント（つつき）の回数に足す数")]
    public int extraNibbles = 1;
    [Tooltip("魚が逃げる餌の速さにかける倍率（小さいほど警戒する）")]
    public float spookScale = 0.75f;
    [Tooltip("本アタリからロボが「いまだ!!」と言うまでの遅れ（秒）。ロボ頼みだと間に合わない魚もいる")]
    public float robotReactionTime = 0.25f;
    [Tooltip("暴れる前に身構える時間（秒）")]
    public float warnTime = 0.3f;
    [Tooltip("暴れた時に逃げる力の倍率")]
    public float thrashPullRate = 0.85f;
    [Tooltip("暴れている時に糸が耐えられる巻きの強さ（小さいほど、ほぼ止めないと切れる）")]
    public float tensionTolerance = 0.3f;
    [Tooltip("おとなしい時に糸の張りが戻る速さ（小さいほど、暴れるたびに張りが溜まっていく）")]
    public float tensionRecovery = 0.5f;

    [Header("── 演出UI ──────────────────────")]
    public Image flash;
    public TMP_Text bigText;
    public TMP_Text hintText;
    public GameObject resultPanel;
    public TMP_Text resultName;
    public TMP_Text resultSize;
    public TMP_Text resultBadge;
    public TMP_Text resultDescription;
    public Image resultFish;
    public FishingLogPanel logPanel;
    public RectTransform logButton;
    [Tooltip("横から見た魚の形（結果カード）")]
    public Sprite fishSide;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip castSE;
    public AudioClip splashSE;
    public AudioClip nibbleSE;
    public AudioClip biteSE;
    public AudioClip hookSE;
    public AudioClip catchSE;
    public AudioClip failSE;
    public AudioClip snapSE;             // 糸が切れた
    public AudioClip jumpSE;             // エラ洗い
    public AudioClip reelSE;             // リールのカチッ
    public AudioClip dragSE;             // ドラグのジジジ（ループ）

    Phase phase = Phase.Hidden;
    Action onExit;
    float timer;
    bool viewStarted;

    readonly List<FishingFish> fishes = new List<FishingFish>();
    float respawnTimer;

    // 狙い・餌
    Vector2 reticlePos = new Vector2(0f, 350f);
    bool aimWithMouse;
    Vector2 castFrom, castTo;
    float castDuration, castProgress;
    float bobberDip;
    bool bobberSunk;
    int nibbleCount;
    float scanTimer;

    // ファイト
    readonly FishingReelInput reelInput = new FishingReelInput();
    FishingFish fightFish;
    FightPhase fightPhase;
    float fightTimer, fightClock;
    float stamina, tension, slack;
    float swayDir;
    float assistTimer;
    bool assistUsed, warnedTension, warnedSlack, warnedLastRun;
    bool lastRun;                        // 足元での最後のひと暴れ中
    float splashTimer;
    float thrashVisual;
    HookDepth hookDepth;
    float hookHold;                      // 掛かりの深さ（浅 0.45 / しっかり 1 / 深 1.4）
    float rodTilt;                       // 竿の倒し具合 -1（左）〜 1（右）
    int tiltCoachLeft = -1, jumpCoachLeft = -1, praiseLeft = -1;   // -1 = まだ数えていない
    bool tiltHintThisThrash;             // この暴れでは「竿をこっちへ」の案内を出す
    float goodTimer;                     // 逆に倒し続けている時間
    bool turnedThisThrash;               // この暴れで魚の向きを変えさせた
    bool lineSnapped;
    Vector2 jumpOrigin;
    float jumpMaxReel;
    float lastReelClickAngle;
    FishingFish.BiteKind biteKind;

    // 振動・ドラグ音
    AudioSource dragSource;
    float rumbleLow, rumbleHigh;
    float pulseTimer, pulseLow, pulseHigh;

    // 釣果
    float robotAlertTimer;
    bool resultIsNew, resultIsRecord;
    float resultSizeCm;
    FishSpecies resultSpecies;

    float bigTimer;
    float rodAngle;

    // ─── 魚から参照される ──────────────────────────────────────
    public bool IsActive => phase != Phase.Hidden;
    public bool BaitInWater { get; private set; }
    public Vector2 BaitPosition { get; private set; }
    public float BaitSpeed { get; private set; }
    public FishingFish EngagedFish { get; private set; }
    public Vector2 LandingPoint => landingPoint;

    // 演出をかけるカメラ（後ろから見た画面のカメラ）
    FishingCameraRig Cam => backView.rig;

    Vector2 RodBase => rod.anchoredPosition;
    Vector2 RodTip => RodBase + (Vector2)(Quaternion.Euler(0f, 0f, rodAngle) * Vector3.up * rodLength);

    void Awake()
    {
        if (view != null) view.gameObject.SetActive(false);

        // ドラグ音はファイト中ずっと鳴らして音量で強弱をつけるので、専用のループ再生口を持つ
        dragSource = gameObject.AddComponent<AudioSource>();
        dragSource.clip = dragSE;
        dragSource.loop = true;
        dragSource.playOnAwake = false;
        dragSource.volume = 0f;
    }

    void OnDisable()
    {
        StopFightFeedback();
    }

    // ─── 開始・終了 ────────────────────────────────────────────
    /// <summary>
    /// 釣りを始める。終わったら finished を呼ぶ
    /// </summary>
    public void Begin(Action finished)
    {
        if (IsActive) return;
        onExit = finished;

        view.gameObject.SetActive(true);
        view.alpha = 0f;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (logPanel != null) logPanel.Close();
        if (reel != null) reel.gameObject.SetActive(false);
        if (bigText != null) bigText.text = "";
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);

        ClearFish();
        for (int i = 0; i < fishCount; i++) SpawnFish();

        BaitInWater = false;
        EngagedFish = null;
        fightFish = null;
        scanTimer = 0f;
        backView.Open(landingPoint);
        if (tiltCoachLeft < 0) tiltCoachLeft = jumpCoachLeft = praiseLeft = coachCount;
        timer = 0f;
        viewStarted = false;
        phase = Phase.Entering;
        SetHint("");
    }

    void Exit()
    {
        StopFightFeedback();
        backView.Close();
        if (EngagedFish != null) EngagedFish.Flee();
        BaitInWater = false;
        backView.HideBubble();
        timer = 0f;
        phase = Phase.Exiting;
        SetHint("");
    }

    // ─── 毎フレーム ────────────────────────────────────────────
    void Update()
    {
        if (phase == Phase.Hidden) return;

        float dt = Time.unscaledDeltaTime;
        float simDt = dt * Cam.TimeScale;
        // 右ドラッグ中は竿を倒す操作なので、マウスの動きをリールとして数えない
        reelInput.Update(dt, RightHeld());
        if (scanTimer > 0f) scanTimer -= dt;

        switch (phase)
        {
            case Phase.Entering: UpdateEntering(dt); break;
            case Phase.Exiting: UpdateExiting(dt); break;
            case Phase.Aim: UpdateAim(dt); break;
            case Phase.Casting: UpdateCasting(simDt); break;
            case Phase.Lure: UpdateLure(simDt); break;
            case Phase.ReelIn: UpdateReelIn(dt); break;
            case Phase.Fight: UpdateFight(simDt); break;
            case Phase.Landing: UpdateLanding(simDt); break;
            case Phase.Result: UpdateResult(dt); break;
            case Phase.Lost: UpdateLost(dt); break;
            case Phase.Log: UpdateLog(); break;
        }
        if (phase == Phase.Hidden) return;

        UpdateFish(simDt);
        UpdateTackle(dt);
        UpdateBackView();
        UpdateEffects(dt);
        UpdateRumble(dt);
    }

    // ─── 町 ⇔ 釣り画面 ─────────────────────────────────────────
    void UpdateEntering(float dt)
    {
        timer += dt;
        float p = Mathf.Clamp01(timer / enterDuration);
        // 最初はゆっくり、だんだん速く池へ吸い込まれるように寄る
        SetTownZoom(Mathf.Lerp(1f, townZoom, p * p * p));
        view.alpha = Mathf.InverseLerp(0.55f, 1f, p);

        if (!viewStarted && p >= 0.55f)
        {
            // 釣り画面も寄った状態から始めて、池全体が見えるまで引く（町からカメラが続いているように見せる）
            viewStarted = true;
        }
        if (p >= 1f) EnterAim();
    }

    void UpdateExiting(float dt)
    {
        timer += dt;
        float p = Mathf.Clamp01(timer / 0.8f);
        view.alpha = 1f - Mathf.InverseLerp(0f, 0.4f, p);
        float e = 1f - Mathf.Pow(1f - p, 3f);
        SetTownZoom(Mathf.Lerp(townZoom, 1f, e));

        if (p >= 1f)
        {
            SetTownZoom(1f);
            ClearFish();
            view.gameObject.SetActive(false);
            phase = Phase.Hidden;
            var callback = onExit;
            onExit = null;
            callback?.Invoke();
        }
    }

    void SetTownZoom(float z)
    {
        if (townView == null) return;
        float t = townZoom > 1f ? (z - 1f) / (townZoom - 1f) : 0f;
        townView.localScale = Vector3.one * z;
        // 拡大するほど池が画面の中心に来るように
        townView.anchoredPosition = Vector2.Lerp(Vector2.zero, -townFocus * z, t);
    }

    // ─── 狙う ──────────────────────────────────────────────────
    void EnterAim()
    {
        phase = Phase.Aim;
        BaitInWater = false;
        EngagedFish = null;
        fightFish = null;
        bobberSunk = false;
        aimWithMouse = Mouse.current != null && Gamepad.current == null;
        reticle.gameObject.SetActive(true);
        bobber.gameObject.SetActive(true);
        if (reel != null) reel.gameObject.SetActive(false);
        if (backView.InFight) backView.EndFight();
        backView.HideAirFish();
        backView.SetFocus(landingPoint + new Vector2(0f, 300f), 1f, 0.6f);
        SetHint("<b>狙う</b> マウス / 左スティック　<b>投げる</b> クリック / A\n" +
                "<b>サーチ</b> ロボをクリック / Q / Y　<b>図鑑</b> Tab / View　<b>町に戻る</b> 右クリック / B");
    }

    void UpdateAim(float dt)
    {
        Vector2 move = ReadMove();
        if (MenuInput.PointerMoved()) aimWithMouse = true;
        if (move != Vector2.zero) aimWithMouse = false;

        if (aimWithMouse) reticlePos = backView.ScreenToPond(MenuInput.PointerPosition());
        else reticlePos += move * aimSpeed * dt;
        reticlePos = ClampCast(reticlePos);
        reticle.anchoredPosition = reticlePos;
        reticle.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f);

        backView.SetFocus(Vector2.Lerp(landingPoint + new Vector2(0f, 300f), reticlePos, 0.3f), 1f, 0.5f);

        if (LogPressed()) { OpenLog(); return; }
        if (RobotPressed()) RobotAction();
        if (CancelPressed()) { Exit(); return; }
        if (ActionPressed()) Cast();
    }

    Vector2 ClampCast(Vector2 p)
    {
        p.x = Mathf.Clamp(p.x, water.xMin + 60f, water.xMax - 60f);
        p.y = Mathf.Clamp(p.y, water.yMin + 140f, water.yMax - 60f);
        Vector2 from = RodBase;
        Vector2 d = p - from;
        if (d.magnitude > castRange) p = from + d.normalized * castRange;
        return p;
    }

    // ─── 投げる ────────────────────────────────────────────────
    void Cast()
    {
        castFrom = RodTip;
        castTo = reticlePos;
        castDuration = 0.35f + Vector2.Distance(castFrom, castTo) / 2200f;
        timer = 0f;
        phase = Phase.Casting;
        reticle.gameObject.SetActive(false);
        backView.HideBubble();
        PlaySE(castSE);
        castProgress = 0f;
        backView.SetFocus(castTo, 1.05f, 0.5f);
        SetHint("");
    }

    void UpdateCasting(float dt)
    {
        timer += dt;
        float p = Mathf.Clamp01(timer / castDuration);
        castProgress = p;
        float e = 1f - (1f - p) * (1f - p);
        // 上から見ているので、高く飛んでいる間は大きく見せる
        bobber.anchoredPosition = Vector2.Lerp(castFrom, castTo, e);
        bobber.localScale = Vector3.one * (1f + Mathf.Sin(p * Mathf.PI) * 0.9f);
        if (p >= 1f) Land();
    }

    void Land()
    {
        BaitPosition = castTo;
        BaitInWater = true;
        BaitSpeed = 0f;
        bobberDip = 0.6f;
        nibbleCount = 0;
        phase = Phase.Lure;

        SpawnRipple(BaitPosition, 150f, 0.9f, 0.5f);
        SpawnRipple(BaitPosition, 70f, 0.5f, 0.6f);
        PlaySE(splashSE);
        Cam.Shake(0.15f);
        backView.Spray(BaitPosition, 0.6f, 10);
        foreach (var f in fishes) f.OnSplash(BaitPosition);

        SetHint("<b>餌を動かす</b> 左ドラッグ / スティック / WASD（Shiftでゆっくり）　<b>合わせる・誘う</b> クリック / A\n" +
                "<b>サーチ</b> ロボ / Q / Y　<b>巻き戻す</b> 右クリック / B");
    }

    // ─── 餌で誘う ──────────────────────────────────────────────
    void UpdateLure(float dt)
    {
        if (dt <= 0f) return;

        bool clicked = ActionPressed();
        if (clicked) TryHook();
        if (phase != Phase.Lure) return;

        if (CancelPressed()) { StartReelIn(); return; }
        if (RobotPressed()) RobotAction();

        // 餌を動かす（速く動かすほど魚が警戒する）
        Vector2 next = BaitPosition;
        Vector2 move = ReadMove();
        if (move != Vector2.zero)
        {
            float speed = IsSlowHeld() ? slowBaitSpeed : baitSpeed;
            next += move * speed * dt;
        }
        else if (MenuInput.LeftHeld() && !clicked && !PointerOnButtons())
        {
            // カーソルに向かって引っぱる。近いほどゆっくり動くので、そっと誘える
            Vector2 target = backView.ScreenToPond(MenuInput.PointerPosition());
            float d = Vector2.Distance(next, target);
            next = Vector2.MoveTowards(next, target, Mathf.Min(d * 2.5f, baitSpeed) * dt);
        }
        next = ClampCast(next);

        float measured = Vector2.Distance(next, BaitPosition) / dt;
        BaitSpeed = Mathf.Lerp(BaitSpeed, measured, 1f - Mathf.Exp(-dt * 10f));
        if (measured > 1f) bobberDip = Mathf.Max(bobberDip, 0.15f);
        BaitPosition = next;

        if (EngagedFish == null || EngagedFish.Current != FishingFish.State.Bite) bobberSunk = false;

        // ロボは少し遅れて本アタリを知らせる。アタリの出方で言い方が変わる
        if (robotAlertTimer > 0f)
        {
            robotAlertTimer -= dt;
            if (robotAlertTimer <= 0f && bobberSunk)
            {
                PlaySE(biteSE);
                backView.Alert(biteKind == FishingFish.BiteKind.Sudden ? "いまだ!!" : biteKind == FishingFish.BiteKind.Hold ? "…食ってる？" : "浮いた！");
            }
        }
        if (bobberSunk)
        {
            // 食い上げ：魚が餌をくわえたまま上がってくるので、ウキが岸の方へ流れる
            if (biteKind == FishingFish.BiteKind.Lift)
            {
                Vector2 toRod = (RodTip - BaitPosition).normalized;
                BaitPosition = ClampCast(BaitPosition + (toRod * 0.7f + new Vector2(-toRod.y, toRod.x) * 0.5f) * 35f * dt);
            }

            splashTimer -= dt;
            if (splashTimer <= 0f)
            {
                switch (biteKind)
                {
                    case FishingFish.BiteKind.Sudden:
                        splashTimer = 0.15f;
                        SpawnRipple(BaitPosition + UnityEngine.Random.insideUnitCircle * 10f, 90f, 0.5f, 0.45f);
                        break;
                    case FishingFish.BiteKind.Hold:
                        splashTimer = 0.7f;
                        SpawnRipple(BaitPosition, 40f, 0.6f, 0.25f);
                        break;
                    default:
                        splashTimer = 0.35f;
                        SpawnRipple(BaitPosition, 55f, 0.5f, 0.3f);
                        break;
                }
            }
        }

        // 魚が近づくほどカメラがじわっと寄って、緊張感を出す
        var fish = EngagedFish;
        float far = Mathf.InverseLerp(landingPoint.y, water.yMax, BaitPosition.y);
        if (fish == null) backView.SetFocus(BaitPosition, 1.1f + 0.15f * far, 0.6f);
        else if (fish.Current == FishingFish.State.Approach) backView.SetFocus(BaitPosition, 1.35f + 0.25f * far, 1.2f);
        else backView.SetFocus(BaitPosition, 1.55f + 0.35f * far, 0.9f);
    }

    void TryHook()
    {
        var fish = EngagedFish;
        if (fish != null && fish.Current == FishingFish.State.Bite)
        {
            StartFight(fish, HookDepthFor(fish.CurrentBite, fish.BiteProgress));
            return;
        }

        // つついている最中の早合わせでも、たまに口の端に浅く掛かる
        if (fish != null && fish.Current == FishingFish.State.Nibble && UnityEngine.Random.value < nibbleHookChance)
        {
            StartFight(fish, HookDepth.Shallow);
            return;
        }

        if (fish != null)
        {
            // 早合わせ：つついているだけの時に合わせると逃げられる
            fish.Flee();
            SpawnRipple(BaitPosition, 80f, 0.4f, 0.5f);
            ShowBig("早すぎた…", new Color(0.7f, 0.75f, 0.85f));
            Say("あわてないで…");
            return;
        }

        // 誰も来ていない時は、ちょんと引いて誘う
        BaitPosition = ClampCast(Vector2.MoveTowards(BaitPosition, RodTip, 30f));
        BaitSpeed = 120f;
        bobberDip = 0.5f;
        SpawnRipple(BaitPosition, 60f, 0.4f, 0.35f);
    }

    // 合わせたタイミングで掛かり具合が決まる。待つほど深く食い込む（送り込み）
    static HookDepth HookDepthFor(FishingFish.BiteKind kind, float progress)
    {
        switch (kind)
        {
            case FishingFish.BiteKind.Sudden:
                return progress < 0.65f ? HookDepth.Solid : HookDepth.Deep;
            case FishingFish.BiteKind.Hold:
                return progress < 0.3f ? HookDepth.Shallow : progress < 0.75f ? HookDepth.Solid : HookDepth.Deep;
            default:
                return progress < 0.35f ? HookDepth.Shallow : progress < 0.85f ? HookDepth.Solid : HookDepth.Deep;
        }
    }

    void StartReelIn()
    {
        if (EngagedFish != null) EngagedFish.Flee();
        BaitInWater = false;
        castFrom = BaitPosition;
        timer = 0f;
        phase = Phase.ReelIn;
        SetHint("");
    }

    void UpdateReelIn(float dt)
    {
        timer += dt;
        float p = Mathf.Clamp01(timer / 0.4f);
        castProgress = p;
        bobber.anchoredPosition = Vector2.Lerp(castFrom, HangingBobber(), p * p);
        if (p >= 1f) EnterAim();
    }

    // ─── 魚からの知らせ ────────────────────────────────────────
    public bool Claim(FishingFish fish)
    {
        if (phase != Phase.Lure || !BaitInWater || EngagedFish != null) return false;
        EngagedFish = fish;
        nibbleCount = 0;
        return true;
    }

    public void Release(FishingFish fish)
    {
        if (EngagedFish == fish) EngagedFish = null;
    }

    public void OnNibble(FishingFish fish)
    {
        nibbleCount++;
        bobberDip = 0.7f;
        SpawnRipple(BaitPosition, 70f, 0.5f, 0.4f);
        PlaySE(nibbleSE, 0.6f);
        Cam.Shake(0.1f);
        Cam.Punch(0.02f);
        Pulse(0.25f, 0.1f, 0.08f);
        // ロボは本アタリかどうか見分けて教えてくれる
        if (nibbleCount == 1) Say("…まだだよ", 0.7f);
    }

    public void OnBite(FishingFish fish)
    {
        bobberSunk = true;
        biteKind = fish.CurrentBite;
        splashTimer = 0f;
        // 本アタリの合図はウキの動きだけ。音とロボの声は少し遅れる
        switch (biteKind)
        {
            case FishingFish.BiteKind.Sudden:
                SpawnRipple(BaitPosition, 140f, 0.6f, 0.5f);
                Cam.Shake(0.12f);
                Pulse(0.6f, 0.3f, 0.25f);
                break;
            case FishingFish.BiteKind.Hold:
                // 居食いはほとんど何も起きない。ウキがわずかに沈むだけ
                SpawnRipple(BaitPosition, 45f, 0.5f, 0.3f);
                Pulse(0.15f, 0f, 0.12f);
                break;
            default:
                SpawnRipple(BaitPosition, 70f, 0.5f, 0.35f);
                Pulse(0.3f, 0.1f, 0.15f);
                break;
        }
        robotAlertTimer = robotReactionTime;
    }

    public void OnBiteMissed(FishingFish fish)
    {
        bobberSunk = false;
        string message = biteKind == FishingFish.BiteKind.Hold ? "餌だけ取られた…"
                       : biteKind == FishingFish.BiteKind.Lift ? "吐き出された…" : "逃げられた…";
        ShowBig(message, new Color(0.7f, 0.75f, 0.85f));
        Say(biteKind == FishingFish.BiteKind.Hold ? "ウキ、ちょっと沈んでたかも…" : "おしい！");
    }

    public void OnSpooked(FishingFish fish)
    {
        if (EngagedFish != fish) return;
        Say("驚いて逃げちゃった…");
        Release(fish);
    }

    // ─── ファイト ──────────────────────────────────────────────
    void StartFight(FishingFish fish, HookDepth depth)
    {
        fightFish = fish;
        hookDepth = depth;
        hookHold = depth == HookDepth.Shallow ? 0.45f : depth == HookDepth.Deep ? 1.4f : 1f;
        rodTilt = 0f;
        goodTimer = 0f;
        turnedThisThrash = false;
        tiltHintThisThrash = false;
        lineSnapped = false;
        lastReelClickAngle = reelInput.Angle;
        // ここからはプレイヤーの後ろから見た画面で戦う
        if (backView != null) backView.StartFight(fish.Species, fish.Position);
        fish.Hook();
        EngagedFish = null;
        BaitInWater = false;
        bobberSunk = false;
        bobber.gameObject.SetActive(false);
        if (reel != null) reel.gameObject.SetActive(true);

        stamina = 1f;
        tension = 0.3f;
        slack = 0f;
        assistTimer = 0f;
        assistUsed = warnedTension = warnedSlack = warnedLastRun = lastRun = false;
        swayDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        fightClock = 0f;
        // かかった瞬間は必ず一暴れする
        fightPhase = FightPhase.Thrash;
        fightTimer = 1f;
        reelInput.Reset();
        phase = Phase.Fight;

        // かかった瞬間：スロー＋グッと寄る＋フラッシュ。浅い時は控えめ、深い時は派手に
        // 画面の文字は釣りを知らない人にも分かる言葉にする
        string depthLabel = depth == HookDepth.Shallow ? "かかりが浅い" : depth == HookDepth.Deep ? "がっちり！" : "";
        ShowBig(depthLabel == "" ? "HIT!" : "HIT!\n<size=45%>" + depthLabel + "</size>", new Color(1f, 0.85f, 0.3f));
        PlaySE(hookSE);
        Cam.SlowMo(0.25f, 0.4f);
        Cam.Punch(depth == HookDepth.Shallow ? 0.12f : 0.2f);
        Cam.Shake(depth == HookDepth.Deep ? 0.65f : 0.5f);
        Flash(0.35f);
        SpawnRipple(fish.Position, 220f, 0.8f, 0.7f);
        Pulse(1f, 0.7f, 0.35f);
        Say(depth == HookDepth.Shallow ? "浅いかも…慎重に！" : depth == HookDepth.Deep ? "がっちり掛かった！" : "かかった！");

        if (dragSource != null && dragSE != null)
        {
            dragSource.clip = dragSE;
            dragSource.volume = 0f;
            dragSource.Play();
        }

        SetHint("<b>巻く</b> マウスをぐるぐる / 左スティックを回す / Space長押し　<b>竿を倒す</b> 右ドラッグで左右 / 右スティック / A・D\n" +
                "暴れたらゆるめる・横に走ったら逆へ倒す・跳ねたら巻かない　<b>ロボ（1回）</b> Q / Y");
    }

    void UpdateFight(float dt)
    {
        if (RobotPressed()) RobotAction();
        if (dt <= 0f) return;

        var s = fightFish.Species;
        fightClock += dt;
        bool assisting = assistTimer > 0f;
        if (assisting)
        {
            assistTimer -= dt;
            if (assistTimer <= 0f) backView.SetAssist(false);
        }

        UpdateRodTilt(dt);

        float reelPower = Mathf.Clamp(reelInput.Speed / FishingReelInput.NormalSpeed, 0f, 2.5f);
        Vector2 pos = fightFish.Position;
        Vector2 toPier = landingPoint - pos;
        float distance = toPier.magnitude;
        Vector2 dir = distance > 0.01f ? toPier / distance : Vector2.down;
        Vector2 side = new Vector2(-dir.y, dir.x) * swayDir;
        // 疲れるほど暴れる力が弱くなる
        float power = Mathf.Lerp(0.35f, 1f, stamina);

        // 竿さばき：魚が横に走る向きと逆に竿を倒せているか（good）、同じ側に倒しているか（bad）
        float runX = Mathf.Abs(side.x) > 0.2f ? Mathf.Sign(side.x) : 0f;
        float good = Mathf.Max(0f, -rodTilt * runX);
        float bad = Mathf.Max(0f, rodTilt * runX);
        Vector2 velocity;

        fightTimer -= dt;
        switch (fightPhase)
        {
            case FightPhase.Calm:
            {
                // おとなしい時：巻いた分だけ寄ってくる。魚はゆっくり横へ逃げようとする（逆に倒すと止まる）
                velocity = -dir * s.strength * 0.12f * power + side * s.swimSpeed * 0.35f * (1f - 0.7f * good);
                velocity += dir * reelPull * (reelPower * (assisting ? 1.4f : 1f) + 0.15f * good);
                // 巻きすぎていると張りが戻らず、次に暴れた時に切れやすい
                tension = Mathf.MoveTowards(tension, 0.1f + 0.3f * reelPower, dt * tensionRecovery);

                // 巻かずに放っておくと糸がたるんでバレる。浅掛かりほど早い
                float limit = slackLimit * (hookDepth == HookDepth.Shallow ? 0.5f : hookDepth == HookDepth.Deep ? 1.25f : 1f);
                if (reelPower < 0.15f) slack += dt;
                else slack = Mathf.Max(0f, slack - dt * 2f);
                if (slack > limit * 0.55f && !warnedSlack)
                {
                    warnedSlack = true;
                    Say("糸がたるんでる！巻いて！");
                }
                if (slack > limit) { Lose("バレた…", false, "巻くのを止めすぎたかも…"); return; }

                stamina -= dt / FightStamina(s) * 0.35f;
                thrashVisual = Mathf.MoveTowards(thrashVisual, 0f, dt * 3f);

                // まだ元気なうちに足元まで寄せると、最後のひと暴れで沖へ走り出す
                if (distance < nearPierDistance && stamina > landingStamina && fightTimer > 0f)
                {
                    fightTimer = 0f;
                    lastRun = true;
                    if (!warnedLastRun)
                    {
                        warnedLastRun = true;
                        Say("まだ元気だ！もうひと暴れくるよ！");
                    }
                }

                if (fightTimer <= 0f)
                {
                    // 暴れる前に一瞬ぐっと身構える（これが合図）
                    fightPhase = FightPhase.Warn;
                    fightTimer = warnTime * (1f - 0.4f * s.aggression);
                }
                break;
            }
            case FightPhase.Warn:
            {
                velocity = dir * reelPull * reelPower * 0.3f;
                thrashVisual = Mathf.MoveTowards(thrashVisual, 0.4f, dt * 4f);
                if (fightTimer <= 0f)
                {
                    // 浅掛かりは暴れ出した拍子に外れることがある
                    if (hookDepth == HookDepth.Shallow && UnityEngine.Random.value < shallowHookOutChance)
                    {
                        Lose("外れた…", false, "かかりが浅かったみたい…");
                        return;
                    }

                    swayDir = -swayDir;
                    if (s.jumps && distance < 750f && UnityEngine.Random.value < jumpChance)
                    {
                        // エラ洗い：水面が泡立って「！」が出たあとに跳ぶ
                        fightPhase = FightPhase.JumpWarn;
                        fightTimer = jumpWarnTime;
                        Pulse(0.4f, 0.4f, 0.2f);
                        if (jumpCoachLeft > 0)
                        {
                            jumpCoachLeft--;
                            Cam.SlowMo(0.4f, 0.7f);
                            Say("跳ぶよ！巻くのを止めて！", 1.6f);
                        }
                    }
                    else
                    {
                        fightPhase = FightPhase.Thrash;
                        fightTimer = Mathf.Lerp(1.0f, 2.6f, s.aggression) * (0.6f + 0.4f * stamina) * UnityEngine.Random.Range(0.8f, 1.2f);
                        goodTimer = 0f;
                        turnedThisThrash = false;
                        tiltHintThisThrash = false;
                        SpawnRipple(pos, 200f, 0.6f, 0.6f);
                        if (backView != null) backView.Spray(pos, 1.3f, 16);
                        Cam.Punch(0.04f);
                        Pulse(0.8f, 0.3f, 0.15f);

                        // 最初の数回は、一瞬スローにして竿の倒し方を教える
                        float newRunX = Mathf.Sign((new Vector2(-dir.y, dir.x) * swayDir).x);
                        if (tiltCoachLeft > 0)
                        {
                            tiltCoachLeft--;
                            tiltHintThisThrash = true;
                            Cam.SlowMo(0.4f, 0.6f);
                            Say(newRunX < 0f ? "左に走る！竿を右へ倒して！" : "右に走る！竿を左へ倒して！", 1.8f);
                        }
                    }
                }
                break;
            }
            case FightPhase.JumpWarn:
            {
                // 水面近くに浮いてきて、しぶきが立つ
                velocity = -dir * s.strength * 0.2f;
                thrashVisual = Mathf.MoveTowards(thrashVisual, 1f, dt * 6f);
                splashTimer = Mathf.Min(splashTimer, 0.05f);
                if (fightTimer <= 0f) StartJump(pos);
                break;
            }
            case FightPhase.Jump:
            {
                // 跳んでいる間に巻くと針が外れる
                velocity = Vector2.zero;
                jumpMaxReel = Mathf.Max(jumpMaxReel, reelPower);
                if (fightTimer <= 0f && !EndJump(s)) return;
                break;
            }
            default:
            {
                // 暴れている時：奥へ、横へと走る。強く巻くほど糸に負担がかかる。逆に竿を倒すと横走りを止められる
                float zig = Mathf.Sin(fightClock * Mathf.Lerp(5f, 11f, s.aggression));
                float run = lastRun ? Mathf.Max(s.strength, lastRunMinPull) : s.strength;
                velocity = -dir * run * thrashPullRate * power
                         + side * s.strength * 0.5f * power * (1f - 0.75f * good)
                         + side * zig * s.strength * 0.2f * power;
                velocity += dir * reelPull * (reelPower * (assisting ? 0.7f : 0.35f) + 0.35f * good);

                float strain = 1.25f * Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(120f, 420f, s.strength));
                float gain = reelPower * strain * power * (assisting ? 0.35f : 1f) * (1f - 0.45f * good);
                tension += (gain - tensionTolerance + 0.4f * bad) * dt;
                if (pos.y >= water.yMax - 80f) tension += 0.25f * dt;   // 池の奥まで走られると糸が出切る
                tension = Mathf.Max(tension, 0.25f);
                slack = 0f;

                // 走らせてあげた方が早く疲れる
                stamina -= dt / FightStamina(s) * (reelPower < 0.5f ? 2f : 1.2f);
                thrashVisual = Mathf.MoveTowards(thrashVisual, power, dt * 6f);
                Cam.Shake(dt * 1.1f * power);

                // 逆に倒し続けると、魚がグイッと向きを変える（うまくいった手応え）
                goodTimer = good > 0.6f ? goodTimer + dt : 0f;
                if (goodTimer > 0.35f && !turnedThisThrash)
                {
                    turnedThisThrash = true;
                    tension = Mathf.Max(0.25f, tension - 0.12f);
                    if (backView != null) backView.Spray(pos, 1.5f, 20);
                    SpawnRipple(pos, 180f, 0.5f, 0.6f);
                    PlaySE(splashSE, 0.7f);
                    Cam.Punch(0.06f);
                    Pulse(0.7f, 0.2f, 0.15f);
                    if (praiseLeft > 0)
                    {
                        praiseLeft--;
                        Say("いいね！向きが変わった！");
                    }
                }

                if (tension > 0.8f && !warnedTension)
                {
                    warnedTension = true;
                    Say("切れちゃう！ゆるめて！");
                }
                if (tension >= 1f) { Lose("糸が切れた！", true, "ドンマイ…次は暴れてる時にゆるめよう"); return; }

                if (fightTimer <= 0f)
                {
                    fightPhase = FightPhase.Calm;
                    fightTimer = Mathf.Lerp(2.2f, 0.7f, s.aggression) * UnityEngine.Random.Range(0.7f, 1.3f) + (1f - stamina) * 1.2f;
                    warnedTension = false;
                    lastRun = false;
                }
                break;
            }
        }
        stamina = Mathf.Max(0f, stamina);

        pos += velocity * dt;
        pos.x = Mathf.Clamp(pos.x, water.xMin + 40f, water.xMax - 40f);
        pos.y = Mathf.Clamp(pos.y, landingPoint.y - 20f, water.yMax - 40f);
        fightFish.SetHooked(pos, landingPoint, thrashVisual, 1f - stamina, dt);

        if (tension > 0.75f) Cam.Shake(dt * 0.6f);

        UpdateFightFeedback(velocity, dir, power, dt);

        if (backView != null)
        {
            backView.TickFight(new FishingBackView.FightState
            {
                fishPos = pos,
                fishVelocity = velocity,
                fishLength = s.shadowLength,
                fishHeading = fightFish.Heading,
                thrash = thrashVisual,
                tired = 1f - stamina,
                tension = tension,
                rodTilt = rodTilt,
                reelAngle = reelInput.Angle,
                runDir = fightPhase == FightPhase.Thrash || fightPhase == FightPhase.Calm ? (int)runX : 0,
                jumpWarn = fightPhase == FightPhase.JumpWarn,
                airborne = fightPhase == FightPhase.Jump,
                airProgress = fightPhase == FightPhase.Jump ? 1f - Mathf.Clamp01(fightTimer / JumpTime) : 0f,
                tiltHint = tiltHintThisThrash,
                countering = good > 0.4f,
                assisting = assistTimer > 0f,
            }, dt);
        }

        // 水しぶき：暴れている時は激しく、おとなしい時は時々（跳んでいる間は出さない）
        splashTimer -= dt;
        if (splashTimer <= 0f && fightPhase != FightPhase.Jump)
        {
            bool thrashing = fightPhase == FightPhase.Thrash || fightPhase == FightPhase.JumpWarn;
            splashTimer = thrashing ? 0.07f : 0.5f;
            float size = (thrashing ? 130f : 60f) * Mathf.Lerp(0.8f, 1.4f, Mathf.InverseLerp(60f, 220f, s.shadowLength));
            SpawnRipple(pos + UnityEngine.Random.insideUnitCircle * s.shadowLength * 0.3f, size, 0.5f, thrashing ? 0.55f : 0.25f);
        }

        // 暴れている魚はすくえない。おとなしい時に足元まで寄せたら釣り上げ
        if (fightPhase == FightPhase.Calm && Vector2.Distance(pos, landingPoint) < 70f) StartLanding();
    }

    const float JumpTime = 0.75f;

    // ファイトで使う魚の体力（秒）
    float FightStamina(FishSpecies s)
    {
        return Mathf.Max(s.stamina, staminaFloor) * fightStaminaScale;
    }

    void StartJump(Vector2 pos)
    {
        fightPhase = FightPhase.Jump;
        fightTimer = JumpTime;
        jumpOrigin = pos;
        jumpMaxReel = 0f;

        fightFish.SetHidden(true);

        PlaySE(jumpSE);
        Cam.SlowMo(0.35f, 0.5f);
        Cam.Punch(0.12f);
        Cam.Shake(0.4f);
        Flash(0.2f);
        SpawnRipple(pos, 240f, 0.7f, 0.7f);
        if (backView != null) backView.Spray(pos, 1.6f, 26);
        Pulse(1f, 1f, 0.3f);
    }

    // 着水。巻いていたら外れる（false を返したらファイト終了）
    bool EndJump(FishSpecies s)
    {
        fightFish.SetHidden(false);
        PlaySE(splashSE);
        SpawnRipple(jumpOrigin, 220f, 0.7f, 0.6f);
        if (backView != null) backView.Spray(jumpOrigin, 1.5f, 24);
        Cam.Shake(0.3f);

        // 深く掛かっているほど、多少巻いていても耐える
        float allowed = 0.15f + 0.25f * hookHold;
        if (jumpMaxReel > allowed || tension > 0.85f)
        {
            Lose("跳ねて外れた…", false, "跳んでる時は巻くのを止めよう！");
            return false;
        }

        Say("ナイス！耐えた！");
        Pulse(0.6f, 0.2f, 0.2f);
        stamina = Mathf.Max(0f, stamina - 0.12f);  // 跳ぶと疲れる
        fightPhase = FightPhase.Calm;
        fightTimer = 1f;
        return true;
    }

    // 竿の左右：右ドラッグ中のマウス左右 / 右スティック / A・D。操作していない時はまっすぐに戻る
    void UpdateRodTilt(float dt)
    {
        bool input = false;
        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed)
        {
            rodTilt += mouse.delta.ReadValue().x * mouseTiltSensitivity;
            input = true;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            float x = pad.rightStick.ReadValue().x;
            if (Mathf.Abs(x) > 0.3f)
            {
                rodTilt = Mathf.MoveTowards(rodTilt, x, dt * 4f);
                input = true;
            }
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            if (x != 0f)
            {
                rodTilt = Mathf.MoveTowards(rodTilt, x, dt * 4f);
                input = true;
            }
        }

        if (!input) rodTilt = Mathf.MoveTowards(rodTilt, 0f, dt * tiltReturnSpeed);
        rodTilt = Mathf.Clamp(rodTilt, -1f, 1f);
    }

    // ファイト中の手応え：パッドの振動・ドラグ音・リールのカチカチ
    void UpdateFightFeedback(Vector2 velocity, Vector2 dir, float power, float dt)
    {
        bool airborne = fightPhase == FightPhase.Jump;
        rumbleLow = airborne ? 0f : thrashVisual * 0.45f * power + (fightPhase == FightPhase.JumpWarn ? 0.3f : 0f) + tension * 0.1f;
        rumbleHigh = airborne ? 0f : Mathf.Max(0f, tension - 0.6f) * 1.6f;

        // 魚に糸を引き出されている時だけドラグが鳴る
        if (dragSource != null && dragSource.isPlaying)
        {
            float outward = Mathf.Max(0f, -Vector2.Dot(velocity, dir));
            float target = airborne ? 0f : Mathf.Clamp01(outward / 250f) * (tension > 0.3f ? 1f : 0.5f);
            dragSource.volume = Mathf.MoveTowards(dragSource.volume, target * 0.7f, dt * 4f);
            dragSource.pitch = 0.85f + 0.4f * target;
        }

        if (reelInput.Angle - lastReelClickAngle >= 45f)
        {
            lastReelClickAngle = reelInput.Angle;
            PlaySE(reelSE, 0.25f);
        }
    }

    void StopFightFeedback()
    {
        rumbleLow = rumbleHigh = 0f;
        pulseTimer = 0f;
        if (dragSource != null) dragSource.Stop();
        var pad = Gamepad.current;
        if (pad != null) pad.SetMotorSpeeds(0f, 0f);
    }

    // 一瞬だけ強く振動させる
    void Pulse(float low, float high, float duration)
    {
        pulseLow = low;
        pulseHigh = high;
        pulseTimer = duration;
    }

    void UpdateRumble(float dt)
    {
        var pad = Gamepad.current;
        if (pad == null) return;
        if (pulseTimer > 0f) pulseTimer -= dt;
        bool pulsing = pulseTimer > 0f;
        float low = Mathf.Max(pulsing ? pulseLow : 0f, phase == Phase.Fight ? rumbleLow : 0f);
        float high = Mathf.Max(pulsing ? pulseHigh : 0f, phase == Phase.Fight ? rumbleHigh : 0f);
        pad.SetMotorSpeeds(Mathf.Clamp01(low * rumbleScale), Mathf.Clamp01(high * rumbleScale));
    }

    void Lose(string message, bool broke, string robotLine)
    {
        var fish = fightFish;
        fightFish = null;
        if (fish != null)
        {
            jumpOrigin = fish.Position;
            fish.SetHidden(false);
            fish.Flee();
        }
        backView.SetAssist(false);
        if (reel != null) reel.gameObject.SetActive(false);
        StopFightFeedback();

        timer = 0f;
        phase = Phase.Lost;
        ShowBig(message, new Color(0.75f, 0.8f, 0.9f));
        PlaySE(broke && snapSE != null ? snapSE : failSE);
        if (broke) Pulse(1f, 1f, 0.2f);
        lineSnapped = broke;
        if (broke && backView != null) backView.Snap();
        Say(robotLine, 2.2f);
        Cam.Shake(broke ? 0.7f : 0.3f);
        SetHint("");
    }

    void UpdateLost(float dt)
    {
        timer += dt;
        // ファイト画面で魚が潜っていくのを見せてから、池の画面に戻る
        if (backView != null && backView.InFight)
        {
            backView.FishDive(dt);
            backView.TickFight(new FishingBackView.FightState
            {
                fishPos = jumpOrigin, fishLength = 0f,
                rodTilt = rodTilt, reelAngle = reelInput.Angle, lineBroken = lineSnapped,
            }, dt);
            if (timer >= 0.9f) backView.EndFight();
        }
        if (timer >= 1.6f) EnterAim();
    }

    // ─── 釣り上げ ──────────────────────────────────────────────
    void StartLanding()
    {
        var fish = fightFish;
        fightFish = null;
        backView.SetAssist(false);
        if (reel != null) reel.gameObject.SetActive(false);
        StopFightFeedback();

        resultSpecies = fish.Species;
        resultSizeCm = fish.SizeCm;
        FishingRecord.Register(resultSpecies.id, resultSizeCm, out resultIsNew, out resultIsRecord);
        backView.Spray(fish.Position, 1.8f, 30);
        fishes.Remove(fish);
        Destroy(fish.gameObject);

        timer = 0f;
        phase = Phase.Landing;
        SetHint("");

        // 一番の見せ場：魚がこちらへ跳び上がってくる。スロー＋アップ＋フラッシュ
        Cam.SlowMo(0.3f, 0.8f);
        Cam.Punch(0.1f);
        Cam.Shake(0.4f);
        Flash(0.6f);
        SpawnRipple(landingPoint, 260f, 0.9f, 0.7f);
        ShowBig("釣れた！", new Color(1f, 0.9f, 0.4f));
        PlaySE(catchSE);
        Pulse(1f, 0.5f, 0.4f);
        backView.Cheer(resultIsNew ? "はじめて見る魚だ！" : "やったー！");
    }

    void UpdateLanding(float dt)
    {
        timer += dt;
        backView.LandingJump(Mathf.Clamp01(timer / 0.9f), timer);
        if (timer >= 1.3f) ShowResult();
    }

    void ShowResult()
    {
        phase = Phase.Result;
        timer = 0f;
        resultPanel.SetActive(true);
        resultPanel.transform.localScale = Vector3.one * 0.6f;
        resultName.text = resultSpecies.displayName;
        resultSize.text = resultSizeCm.ToString("0.0") + " cm";
        resultBadge.text = resultIsNew ? "NEW!" : resultIsRecord ? "最大記録更新！" : "";
        resultDescription.text = resultSpecies.description;
        resultFish.color = resultSpecies.color;
        if (fishSide != null) resultFish.sprite = fishSide;
        SetHint("<b>続ける</b> クリック / A");
    }

    void UpdateResult(float dt)
    {
        timer += dt;
        resultPanel.transform.localScale = Vector3.Lerp(resultPanel.transform.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 14f));
        // 釣った魚は手前で跳ね続ける
        backView.LandingJump(1f, 1.3f + timer);

        if (timer < 0.4f) return;
        if (ActionPressed() || CancelPressed())
        {
            resultPanel.SetActive(false);
            EnterAim();
        }
    }

    // ─── 図鑑 ──────────────────────────────────────────────────
    // 図鑑ボタン（Button.onClick）からも呼ばれる
    public void OpenLog()
    {
        if (phase != Phase.Aim || logPanel == null) return;
        logPanel.Open(species);
        reticle.gameObject.SetActive(false);
        phase = Phase.Log;
        SetHint("<b>閉じる</b> クリック / B / Tab");
    }

    void UpdateLog()
    {
        if (CancelPressed() || ActionPressed(true) || LogPressed())
        {
            logPanel.Close();
            EnterAim();
        }
    }

    // ─── ロボット ──────────────────────────────────────────────
    // ロボをクリック、または Q / Y で呼ばれる
    public void RobotAction()
    {
        if (phase == Phase.Fight)
        {
            if (assistUsed)
            {
                Say("もうエネルギーが…！");
                return;
            }
            assistUsed = true;
            assistTimer = assistDuration;
            backView.SetAssist(true);
            Say("まかせて！いっしょに引くよ！", assistDuration);
            PlaySE(hookSE, 0.6f);
            Cam.Punch(0.05f);
            return;
        }

        if (phase != Phase.Aim && phase != Phase.Lure) return;
        if (scanTimer > 0f)
        {
            Say("充電中…", 0.8f);
            return;
        }

        // サーチ：輪が届いた魚から順に光る
        scanTimer = scanCooldown;
        backView.PlayScan();
        PlaySE(nibbleSE);
        int count = 0;
        bool rare = false;
        foreach (var f in fishes)
        {
            if (f.IsLeaving) continue;
            count++;
            if (f.Species.rarity <= 3f) rare = true;
            f.Reveal(backView.ScanDelay(f.Position), 4f);
        }
        Say(rare ? "大きな気配がある…！金色の影を狙って！" : "魚が " + count + " 匹いるよ", 2.4f);
    }

    // ─── 魚 ────────────────────────────────────────────────────
    void SpawnFish()
    {
        float total = 0f;
        foreach (var s in species) total += s.rarity;
        float r = UnityEngine.Random.value * total;
        FishSpecies picked = species[0];
        foreach (var s in species)
        {
            r -= s.rarity;
            if (r <= 0f) { picked = s; break; }
        }

        var pos = new Vector2(UnityEngine.Random.Range(water.xMin + 150f, water.xMax - 150f),
                              UnityEngine.Random.Range(water.yMin + 220f, water.yMax - 100f));
        fishes.Add(FishingFish.Create(fishLayer, picked, pos, this, circle, font));
    }

    void UpdateFish(float dt)
    {
        for (int i = fishes.Count - 1; i >= 0; i--)
        {
            var f = fishes[i];
            f.Tick(dt);
            if (f.Current == FishingFish.State.Gone)
            {
                fishes.RemoveAt(i);
                Destroy(f.gameObject);
            }
        }

        if (fishes.Count < fishCount)
        {
            respawnTimer -= dt;
            if (respawnTimer <= 0f)
            {
                respawnTimer = UnityEngine.Random.Range(3f, 6f);
                SpawnFish();
            }
        }
    }

    void ClearFish()
    {
        foreach (var f in fishes)
            if (f != null) Destroy(f.gameObject);
        fishes.Clear();
    }

    // ─── 竿・糸・ウキ ──────────────────────────────────────────
    Vector2 HangingBobber()
    {
        return RodTip + new Vector2(14f, -30f);
    }

    void UpdateTackle(float dt)
    {
        // 竿は狙っている方へ向ける。ファイト中は魚に引っぱられてしなる
        Vector2 target = phase == Phase.Aim ? reticlePos
                       : fightFish != null ? fightFish.Position
                       : phase == Phase.Landing || phase == Phase.Result ? landingPoint + new Vector2(0f, 200f)
                       : bobber.anchoredPosition;
        float angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.up, target - RodBase), -55f, 55f);
        if (phase == Phase.Fight)
        {
            angle *= 1f + tension * 0.25f;
            angle += Mathf.Sin(Time.unscaledTime * 30f) * tension * tension * 4f;
            // 竿さばき：プレイヤーが倒した分だけ竿が傾く（右に倒す = 時計回り）
            angle = Mathf.Clamp(angle - rodTilt * 40f, -85f, 85f);
        }
        rodAngle = Mathf.LerpAngle(rodAngle, angle, 1f - Mathf.Exp(-dt * 10f));
        rod.localRotation = Quaternion.Euler(0f, 0f, rodAngle);

        if (reel != null && reel.gameObject.activeSelf) reel.localRotation = Quaternion.Euler(0f, 0f, -reelInput.Angle);

        // ウキ
        if (phase == Phase.Aim || phase == Phase.Entering || phase == Phase.Log)
        {
            bobber.anchoredPosition = HangingBobber();
            bobber.localScale = Vector3.one * 0.8f;
        }
        else if (phase == Phase.Lure)
        {
            bobberDip = Mathf.MoveTowards(bobberDip, 0f, dt * 2.5f);
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 0.04f;
            // 消し込みは深く沈み、居食いはわずかに沈んだまま、食い上げは浮いて（上から見ると大きく）見える
            float sink = !bobberSunk ? 1f - bobberDip * 0.35f
                       : biteKind == FishingFish.BiteKind.Sudden ? 0.55f
                       : biteKind == FishingFish.BiteKind.Hold ? 0.84f : 1.15f;
            float offset = !bobberSunk ? 0f : biteKind == FishingFish.BiteKind.Sudden ? -6f : biteKind == FishingFish.BiteKind.Hold ? -2f : 4f;
            bobber.anchoredPosition = BaitPosition + new Vector2(0f, offset);
            bobber.localScale = Vector3.one * (sink + (bobberSunk && biteKind != FishingFish.BiteKind.Lift ? 0f : bob));
        }
        if (bobberImage != null)
        {
            var c = bobberImage.color;
            c.a = phase != Phase.Lure || !bobberSunk ? 1f
                : biteKind == FishingFish.BiteKind.Sudden ? 0.35f : biteKind == FishingFish.BiteKind.Hold ? 0.8f : 1f;
            bobberImage.color = c;
        }

        // 糸
        bool showLine = phase != Phase.Lost && phase != Phase.Landing && phase != Phase.Result && phase != Phase.Log && phase != Phase.Exiting;
        line.enabled = showLine;
        if (!showLine) return;

        line.start = RodTip;
        line.end = fightFish == null ? bobber.anchoredPosition
                 : fightFish.Position;
        if (phase == Phase.Fight)
        {
            // 張っているほどまっすぐ震え、色が赤みを帯びる。ゆるいとたるむ
            line.sag = Mathf.Lerp(0.35f, 0f, Mathf.Clamp01(tension * 1.6f)) * swayDir;
            line.vibration = Mathf.Max(0f, tension - 0.5f) * 14f + thrashVisual * 2f;
            line.color = Color.Lerp(new Color(0.9f, 0.92f, 1f, 0.85f), new Color(1f, 0.5f, 0.4f, 1f), Mathf.InverseLerp(0.6f, 1f, tension));
        }
        else
        {
            // 食い上げはウキが浮いて糸がたるむ。消し込みは糸がピンと張る
            line.sag = phase != Phase.Lure ? 0.15f
                     : !bobberSunk ? 0.2f
                     : biteKind == FishingFish.BiteKind.Sudden ? 0.05f : biteKind == FishingFish.BiteKind.Hold ? 0.14f : 0.32f;
            line.vibration = bobberSunk && biteKind == FishingFish.BiteKind.Sudden ? 1.5f : bobberSunk ? 0.4f : 0f;
            line.color = new Color(0.9f, 0.92f, 1f, 0.7f);
        }
        line.Refresh(dt);
    }

    // ─── 後ろから見た画面へ状態を渡す ────────────────────────────
    void UpdateBackView()
    {
        backView.SetAim(phase == Phase.Aim, reticlePos);
        backView.SyncFish(fishes, fightFish);

        int bite = phase == Phase.Lure && bobberSunk ? (int)biteKind : -1;
        switch (phase)
        {
            case Phase.Casting:
                backView.SetBobber(FishingBackView.BobberMode.Flying, castTo, castProgress, 0f, -1);
                backView.SetRodTarget(castTo);
                break;
            case Phase.Lure:
                backView.SetBobber(FishingBackView.BobberMode.Floating, BaitPosition, 0f, bobberDip, bite);
                backView.SetRodTarget(BaitPosition);
                break;
            case Phase.ReelIn:
                backView.SetBobber(FishingBackView.BobberMode.Returning, castFrom, castProgress, 0f, -1);
                backView.SetRodTarget(castFrom);
                break;
            case Phase.Aim:
            case Phase.Entering:
            case Phase.Log:
                backView.SetBobber(FishingBackView.BobberMode.Hanging, BaitPosition, 0f, 0f, -1);
                backView.SetRodTarget(phase == Phase.Aim ? reticlePos : landingPoint + new Vector2(0f, 300f));
                break;
            default:
                backView.SetBobber(FishingBackView.BobberMode.Hidden, BaitPosition, 0f, 0f, -1);
                backView.SetRodTarget(landingPoint + new Vector2(0f, 300f));
                break;
        }
    }

    // ─── 演出 ──────────────────────────────────────────────────
    void SpawnRipple(Vector2 position, float size, float duration, float alpha)
    {
        backView.Ripple(position, size, duration, alpha);
    }

    void ShowBig(string text, Color color)
    {
        if (bigText == null) return;
        bigText.text = text;
        bigText.color = color;
        bigText.rectTransform.localScale = Vector3.one * 1.8f;
        bigTimer = 1.4f;
    }

    void Flash(float alpha)
    {
        if (flash != null) flash.color = new Color(1f, 1f, 1f, Mathf.Max(flash.color.a, alpha));
    }

    void UpdateEffects(float dt)
    {
        if (flash != null && flash.color.a > 0f)
            flash.color = new Color(1f, 1f, 1f, Mathf.MoveTowards(flash.color.a, 0f, dt * 2.5f));

        if (bigText != null && bigTimer > 0f)
        {
            bigTimer -= dt;
            var t = bigText.rectTransform;
            t.localScale = Vector3.Lerp(t.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 16f));
            var c = bigText.color;
            c.a = Mathf.Clamp01(bigTimer / 0.4f);
            bigText.color = c;
        }
    }

    // ロボにしゃべらせる。ファイト画面が出ている間は、そちらの左下のロボが吹き出しを出す
    void Say(string text, float duration = 1.6f)
    {
        backView.Say(text, duration);
    }

    void SetHint(string text)
    {
        if (hintText != null) hintText.text = text;
    }

    void PlaySE(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
    }

    // ─── 入力 ──────────────────────────────────────────────────
    Vector2 ReadMove()
    {
        Vector2 dir = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            dir += pad.dpad.ReadValue();
            // スティックは倒し具合で速さが変わる（そっと倒せばそっと動く）
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude > stickDeadZone) dir += stick;
        }
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    static bool RightHeld()
    {
        return Mouse.current != null && Mouse.current.rightButton.isPressed;
    }

    static bool IsSlowHeld()
    {
        var kb = Keyboard.current;
        return kb != null && kb.leftShiftKey.isPressed;
    }

    bool PointerOnButtons()
    {
        return MenuInput.IsPointerOver(backView.RobotRect) || MenuInput.IsPointerOver(logButton);
    }

    // 投げる・合わせる。ロボや図鑑ボタンのクリックは除く
    bool ActionPressed(bool anyClick = false)
    {
        if (MenuInput.ConfirmPressed()) return true;
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
        return MenuInput.LeftClicked() && (anyClick || !PointerOnButtons());
    }

    static bool CancelPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        return MenuInput.CancelPressed();
    }

    static bool RobotPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.qKey.wasPressedThisFrame) return true;
        var pad = Gamepad.current;
        return pad != null && pad.buttonNorth.wasPressedThisFrame;
    }

    static bool LogPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame) return true;
        var pad = Gamepad.current;
        return pad != null && pad.selectButton.wasPressedThisFrame;
    }
}
