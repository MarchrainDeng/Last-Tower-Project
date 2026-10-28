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
/// Lure    : 餌を自分で動かして魚を誘う。速く動かすと逃げる。本アタリ（ウキが沈む）で合わせる
/// Fight   : マウスをぐるぐる回してリールを巻く。魚が暴れている時に強く巻くと糸が切れ、
///           巻かずに放っておくとバレる。ゲージは出さず、魚の影の動き・水しぶき・糸の張り・カメラで伝える
/// Landing : 魚が跳ねて釣り上げる演出 → Result（図鑑に記録）
///
/// お助けロボット（FishingRobot）は サーチ（魚を光らせる）/ アタリの合図 / ファイト中のアシスト（1回）をしてくれる
///
/// 【操作】
/// - マウス  : カーソルで狙う、クリックで投げる・合わせる、左ドラッグで餌を動かす、ぐるぐる回して巻く、右クリックで戻る
/// - パッド  : 左スティックで狙う・餌を動かす、Aで投げる・合わせる、スティックを回して巻く、Yでロボ、Viewで図鑑、Bで戻る
/// - キーボード: WASD/矢印で狙う・餌を動かす（Shiftでゆっくり）、Space/Enterで投げる・合わせる、Space長押しで巻く、
///              Qでロボ、Tabで図鑑、Esc/Backspaceで戻る
///
/// 座標は釣り画面のカメラ（content）のローカル座標。下が桟橋、上が池の奥
/// 時間は unscaledDeltaTime に FishingCameraRig のスロー倍率を掛けて進める（町と同じく timeScale は触らない）
/// </summary>
public class MyTownFishing : MonoBehaviour
{
    enum Phase { Hidden, Entering, Aim, Casting, Lure, ReelIn, Fight, Landing, Result, Lost, Log, Exiting }
    enum FightPhase { Calm, Warn, Thrash }

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
    public RectTransform content;
    public RectTransform fishLayer;
    public RectTransform rippleLayer;
    public Sprite circle;
    public TMP_FontAsset font;

    [Header("── 池 ────────────────────────")]
    public Rect water = new Rect(-1000f, -260f, 2000f, 1120f);
    [Tooltip("魚をここまで寄せたら釣り上げ")]
    public Vector2 landingPoint = new Vector2(0f, -230f);
    [Tooltip("いつものカメラの注目点")]
    public Vector2 homeFocus = new Vector2(0f, 40f);
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
    public RectTransform catchFish;      // 釣り上げた時に跳ねる魚
    public Image catchFishBody;
    public Image catchFishTail;
    public FishingRobot robot;

    [Header("── 操作 ──────────────────────")]
    public float aimSpeed = 900f;
    public float baitSpeed = 170f;
    public float slowBaitSpeed = 60f;
    public float stickDeadZone = 0.25f;

    [Header("── ファイト ────────────────────")]
    [Tooltip("ふつうの速さ（1秒に1回転）で巻いた時に寄ってくる速さ")]
    public float reelPull = 190f;
    [Tooltip("暴れていない時にこれだけ巻かずにいるとバレる（秒）")]
    public float slackLimit = 2.5f;
    public float assistDuration = 2.5f;
    public float scanCooldown = 5f;

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

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip castSE;
    public AudioClip splashSE;
    public AudioClip nibbleSE;
    public AudioClip biteSE;
    public AudioClip hookSE;
    public AudioClip catchSE;
    public AudioClip failSE;

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
    float castDuration;
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
    bool assistUsed, warnedTension, warnedSlack;
    float splashTimer;
    float thrashVisual;

    // 釣果
    float robotAlertTimer;
    bool resultIsNew, resultIsRecord;
    float resultSizeCm;
    FishSpecies resultSpecies;

    readonly List<Ripple> ripples = new List<Ripple>();
    float bigTimer;
    float rodAngle;

    class Ripple
    {
        public RectTransform rect;
        public Image image;
        public float time, duration, size, alpha;
    }

    // ─── 魚から参照される ──────────────────────────────────────
    public bool IsActive => phase != Phase.Hidden;
    public bool BaitInWater { get; private set; }
    public Vector2 BaitPosition { get; private set; }
    public float BaitSpeed { get; private set; }
    public FishingFish EngagedFish { get; private set; }
    public Vector2 LandingPoint => landingPoint;

    // 釣り上げた魚が落ちる場所（桟橋の先、プレイヤーとロボの間）
    static readonly Vector2 CatchRestOffset = new Vector2(40f, -45f);

    Vector2 RodBase => rod.anchoredPosition;
    Vector2 RodTip => RodBase + (Vector2)(Quaternion.Euler(0f, 0f, rodAngle) * Vector3.up * rodLength);

    void Awake()
    {
        if (view != null) view.gameObject.SetActive(false);
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
        if (catchFish != null) catchFish.gameObject.SetActive(false);
        if (reel != null) reel.gameObject.SetActive(false);
        if (bigText != null) bigText.text = "";
        if (flash != null) flash.color = new Color(1f, 1f, 1f, 0f);

        ClearFish();
        for (int i = 0; i < fishCount; i++) SpawnFish();

        BaitInWater = false;
        EngagedFish = null;
        fightFish = null;
        scanTimer = 0f;
        timer = 0f;
        viewStarted = false;
        phase = Phase.Entering;
        SetHint("");
    }

    void Exit()
    {
        if (EngagedFish != null) EngagedFish.Flee();
        BaitInWater = false;
        robot.HideBubble();
        timer = 0f;
        phase = Phase.Exiting;
        SetHint("");
    }

    // ─── 毎フレーム ────────────────────────────────────────────
    void Update()
    {
        if (phase == Phase.Hidden) return;

        float dt = Time.unscaledDeltaTime;
        float simDt = dt * rig.TimeScale;
        reelInput.Update(dt);
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
        UpdateRipples(simDt);
        UpdateTackle(dt);
        UpdateEffects(dt);
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
            rig.Snap(landingPoint + new Vector2(0f, 60f), 2.2f);
            rig.Look(homeFocus, 1f, 0.7f);
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
        rig.Look(landingPoint, 1.8f, 0.4f);

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
        rig.Look(homeFocus, 1f, 0.6f);
        SetHint("<b>狙う</b> マウス / 左スティック　<b>投げる</b> クリック / A\n" +
                "<b>サーチ</b> ロボをクリック / Q / Y　<b>図鑑</b> Tab / View　<b>町に戻る</b> 右クリック / B");
    }

    void UpdateAim(float dt)
    {
        Vector2 move = ReadMove();
        if (MenuInput.PointerMoved()) aimWithMouse = true;
        if (move != Vector2.zero) aimWithMouse = false;

        if (aimWithMouse) reticlePos = ScreenToContent(MenuInput.PointerPosition());
        else reticlePos += move * aimSpeed * dt;
        reticlePos = ClampCast(reticlePos);
        reticle.anchoredPosition = reticlePos;
        reticle.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f);

        rig.Look(homeFocus + (reticlePos - homeFocus) * 0.2f, 1f);

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
        robot.HideBubble();
        PlaySE(castSE);
        rig.Look(Vector2.Lerp(homeFocus, castTo, 0.6f), 1.05f, 0.5f);
        SetHint("");
    }

    void UpdateCasting(float dt)
    {
        timer += dt;
        float p = Mathf.Clamp01(timer / castDuration);
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
        rig.Shake(0.15f);
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
            Vector2 target = ScreenToContent(MenuInput.PointerPosition());
            float d = Vector2.Distance(next, target);
            next = Vector2.MoveTowards(next, target, Mathf.Min(d * 2.5f, baitSpeed) * dt);
        }
        next = ClampCast(next);

        float measured = Vector2.Distance(next, BaitPosition) / dt;
        BaitSpeed = Mathf.Lerp(BaitSpeed, measured, 1f - Mathf.Exp(-dt * 10f));
        if (measured > 1f) bobberDip = Mathf.Max(bobberDip, 0.15f);
        BaitPosition = next;

        if (EngagedFish == null) bobberSunk = false;

        // ロボは少し遅れて本アタリを知らせる
        if (robotAlertTimer > 0f)
        {
            robotAlertTimer -= dt;
            if (robotAlertTimer <= 0f && EngagedFish != null && EngagedFish.Current == FishingFish.State.Bite)
            {
                PlaySE(biteSE);
                robot.Alert("いまだ!!");
            }
        }
        if (bobberSunk)
        {
            splashTimer -= dt;
            if (splashTimer <= 0f)
            {
                splashTimer = 0.15f;
                SpawnRipple(BaitPosition + UnityEngine.Random.insideUnitCircle * 10f, 90f, 0.5f, 0.45f);
            }
        }

        // 魚が近づくほどカメラがじわっと寄って、緊張感を出す
        var fish = EngagedFish;
        if (fish == null) rig.Look(Vector2.Lerp(homeFocus, BaitPosition, 0.6f), 1.1f, 0.6f);
        else if (fish.Current == FishingFish.State.Approach) rig.Look(Vector2.Lerp(homeFocus, BaitPosition, 0.85f), 1.3f, 1.2f);
        else rig.Look(Vector2.Lerp(homeFocus, BaitPosition, 0.9f), 1.45f, 0.9f);
    }

    void TryHook()
    {
        var fish = EngagedFish;
        if (fish != null && fish.Current == FishingFish.State.Bite)
        {
            StartFight(fish);
            return;
        }

        if (fish != null)
        {
            // 早合わせ：つついているだけの時に合わせると逃げられる
            fish.Flee();
            SpawnRipple(BaitPosition, 80f, 0.4f, 0.5f);
            ShowBig("早すぎた…", new Color(0.7f, 0.75f, 0.85f));
            robot.Say("あわてないで…");
            return;
        }

        // 誰も来ていない時は、ちょんと引いて誘う
        BaitPosition = ClampCast(Vector2.MoveTowards(BaitPosition, RodTip, 30f));
        BaitSpeed = 120f;
        bobberDip = 0.5f;
        SpawnRipple(BaitPosition, 60f, 0.4f, 0.35f);
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
        rig.Shake(0.1f);
        rig.Punch(0.02f);
        // ロボは本アタリかどうか見分けて教えてくれる
        if (nibbleCount == 1) robot.Say("…まだだよ", 0.7f);
    }

    public void OnBite(FishingFish fish)
    {
        bobberSunk = true;
        splashTimer = 0f;
        // 本アタリの合図はウキが沈むことだけ。音とロボの声は少し遅れる
        SpawnRipple(BaitPosition, 140f, 0.6f, 0.5f);
        rig.Shake(0.12f);
        robotAlertTimer = robotReactionTime;
    }

    public void OnBiteMissed(FishingFish fish)
    {
        bobberSunk = false;
        ShowBig("逃げられた…", new Color(0.7f, 0.75f, 0.85f));
        robot.Say("おしい！");
    }

    public void OnSpooked(FishingFish fish)
    {
        if (EngagedFish != fish) return;
        robot.Say("驚いて逃げちゃった…");
        Release(fish);
    }

    // ─── ファイト ──────────────────────────────────────────────
    void StartFight(FishingFish fish)
    {
        fightFish = fish;
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
        assistUsed = warnedTension = warnedSlack = false;
        swayDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        fightClock = 0f;
        // かかった瞬間は必ず一暴れする
        fightPhase = FightPhase.Thrash;
        fightTimer = 1f;
        reelInput.Reset();
        phase = Phase.Fight;

        // かかった瞬間：スロー＋グッと寄る＋フラッシュ
        ShowBig("HIT!", new Color(1f, 0.85f, 0.3f));
        PlaySE(hookSE);
        rig.SlowMo(0.25f, 0.4f);
        rig.Punch(0.2f);
        rig.Shake(0.5f);
        Flash(0.35f);
        SpawnRipple(fish.Position, 220f, 0.8f, 0.7f);
        robot.Say("かかった！");

        SetHint("<b>リールを巻く</b> マウスをぐるぐる回す / スティックを回す / Space長押し\n" +
                "魚が暴れたら巻くのをゆるめよう　<b>ロボの助け（1回）</b> ロボをクリック / Q / Y");
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
            if (assistTimer <= 0f) robot.SetAssist(false);
        }

        float reelPower = Mathf.Clamp(reelInput.Speed / FishingReelInput.NormalSpeed, 0f, 2.5f);
        Vector2 pos = fightFish.Position;
        Vector2 toPier = landingPoint - pos;
        float distance = toPier.magnitude;
        Vector2 dir = distance > 0.01f ? toPier / distance : Vector2.down;
        Vector2 side = new Vector2(-dir.y, dir.x) * swayDir;
        // 疲れるほど暴れる力が弱くなる
        float power = Mathf.Lerp(0.35f, 1f, stamina);
        Vector2 velocity;

        fightTimer -= dt;
        switch (fightPhase)
        {
            case FightPhase.Calm:
            {
                // おとなしい時：巻いた分だけ寄ってくる。魚はゆっくり横へ逃げようとする
                velocity = -dir * s.strength * 0.12f * power + side * s.swimSpeed * 0.35f;
                velocity += dir * reelPull * reelPower * (assisting ? 1.4f : 1f);
                // 巻きすぎていると張りが戻らず、次に暴れた時に切れやすい
                tension = Mathf.MoveTowards(tension, 0.1f + 0.3f * reelPower, dt * tensionRecovery);

                // 巻かずに放っておくと糸がたるんでバレる
                if (reelPower < 0.15f) slack += dt;
                else slack = Mathf.Max(0f, slack - dt * 2f);
                if (slack > slackLimit * 0.55f && !warnedSlack)
                {
                    warnedSlack = true;
                    robot.Say("糸がたるんでる！巻いて！");
                }
                if (slack > slackLimit) { Lose("バレた…", false); return; }

                stamina -= dt / s.stamina * 0.35f;
                thrashVisual = Mathf.MoveTowards(thrashVisual, 0f, dt * 3f);

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
                    fightPhase = FightPhase.Thrash;
                    fightTimer = Mathf.Lerp(1.0f, 2.6f, s.aggression) * (0.6f + 0.4f * stamina) * UnityEngine.Random.Range(0.8f, 1.2f);
                    swayDir = -swayDir;
                    SpawnRipple(pos, 200f, 0.6f, 0.6f);
                    rig.Punch(0.04f);
                }
                break;
            }
            default:
            {
                // 暴れている時：奥へジグザグに走る。強く巻くほど糸に負担がかかる
                float zig = Mathf.Sin(fightClock * Mathf.Lerp(5f, 11f, s.aggression));
                velocity = -dir * s.strength * thrashPullRate * power + side * zig * s.strength * 0.6f * power;
                velocity += dir * reelPull * reelPower * (assisting ? 0.7f : 0.35f);

                float strain = 1.25f * Mathf.Lerp(0.8f, 1.3f, Mathf.InverseLerp(120f, 420f, s.strength));
                float gain = reelPower * strain * power * (assisting ? 0.35f : 1f);
                tension += (gain - tensionTolerance) * dt;
                if (pos.y >= water.yMax - 80f) tension += 0.25f * dt;   // 池の奥まで走られると糸が出切る
                tension = Mathf.Max(tension, 0.25f);
                slack = 0f;

                // 走らせてあげた方が早く疲れる
                stamina -= dt / s.stamina * (reelPower < 0.5f ? 2f : 1.2f);
                thrashVisual = Mathf.MoveTowards(thrashVisual, power, dt * 6f);
                rig.Shake(dt * 1.1f * power);

                if (tension > 0.8f && !warnedTension)
                {
                    warnedTension = true;
                    robot.Say("切れちゃう！ゆるめて！");
                }
                if (tension >= 1f) { Lose("糸が切れた！", true); return; }

                if (fightTimer <= 0f)
                {
                    fightPhase = FightPhase.Calm;
                    fightTimer = Mathf.Lerp(2.2f, 0.7f, s.aggression) * UnityEngine.Random.Range(0.7f, 1.3f) + (1f - stamina) * 1.2f;
                    warnedTension = false;
                }
                break;
            }
        }
        stamina = Mathf.Max(0f, stamina);

        pos += velocity * dt;
        pos.x = Mathf.Clamp(pos.x, water.xMin + 40f, water.xMax - 40f);
        pos.y = Mathf.Clamp(pos.y, landingPoint.y - 20f, water.yMax - 40f);
        fightFish.SetHooked(pos, landingPoint, thrashVisual, 1f - stamina, dt);

        if (tension > 0.75f) rig.Shake(dt * 0.6f);

        // 水しぶき：暴れている時は激しく、おとなしい時は時々
        splashTimer -= dt;
        if (splashTimer <= 0f)
        {
            bool thrashing = fightPhase == FightPhase.Thrash;
            splashTimer = thrashing ? 0.07f : 0.5f;
            float size = (thrashing ? 130f : 60f) * Mathf.Lerp(0.8f, 1.4f, Mathf.InverseLerp(60f, 220f, s.shadowLength));
            SpawnRipple(pos + UnityEngine.Random.insideUnitCircle * s.shadowLength * 0.3f, size, 0.5f, thrashing ? 0.55f : 0.25f);
        }

        // カメラ：桟橋と魚の両方が入るように。寄せるほどアップに
        Vector2 span = pos - landingPoint;
        Vector2 focus = (pos + landingPoint) * 0.5f + new Vector2(0f, -30f);
        float zoom = Mathf.Clamp(Mathf.Min(900f / (Mathf.Abs(span.y) + 380f), 1700f / (Mathf.Abs(span.x) + 500f)), 0.7f, 1.45f);
        // 身構えた瞬間にぐっと寄り、暴れ出したら少し引いて揺らす
        if (fightPhase == FightPhase.Warn) rig.Look(Vector2.Lerp(focus, pos, 0.3f), zoom + 0.1f, 0.15f);
        else rig.Look(focus, zoom + (fightPhase == FightPhase.Thrash ? 0.04f : 0f), 0.4f);

        if (Vector2.Distance(pos, landingPoint) < 70f) StartLanding();
    }

    void Lose(string message, bool broke)
    {
        var fish = fightFish;
        fightFish = null;
        if (fish != null) fish.Flee();
        robot.SetAssist(false);
        if (reel != null) reel.gameObject.SetActive(false);

        timer = 0f;
        phase = Phase.Lost;
        ShowBig(message, new Color(0.75f, 0.8f, 0.9f));
        PlaySE(failSE);
        robot.Say(broke ? "ドンマイ…次は暴れてる時にゆるめよう" : "巻くのを止めすぎたかも…", 2.2f);
        rig.Shake(broke ? 0.7f : 0.3f);
        rig.Look(homeFocus, 1f, 0.8f);
        SetHint("");
    }

    void UpdateLost(float dt)
    {
        timer += dt;
        if (timer >= 1.6f) EnterAim();
    }

    // ─── 釣り上げ ──────────────────────────────────────────────
    void StartLanding()
    {
        var fish = fightFish;
        fightFish = null;
        robot.SetAssist(false);
        if (reel != null) reel.gameObject.SetActive(false);

        resultSpecies = fish.Species;
        resultSizeCm = fish.SizeCm;
        FishingRecord.Register(resultSpecies.id, resultSizeCm, out resultIsNew, out resultIsRecord);
        fishes.Remove(fish);
        Destroy(fish.gameObject);

        // 跳ねる魚の見た目
        float len = resultSpecies.shadowLength * 1.4f;
        catchFish.gameObject.SetActive(true);
        catchFish.anchoredPosition = landingPoint;
        catchFishBody.rectTransform.sizeDelta = new Vector2(len, len * resultSpecies.shadowWidthRate);
        catchFishTail.rectTransform.sizeDelta = new Vector2(len * 0.32f, len * resultSpecies.shadowWidthRate * 0.85f);
        catchFishTail.rectTransform.anchoredPosition = new Vector2(-len * 0.42f, 0f);
        catchFishBody.color = catchFishTail.color = resultSpecies.color;

        timer = 0f;
        phase = Phase.Landing;
        SetHint("");

        // 一番の見せ場：スロー＋アップ＋フラッシュ
        rig.SlowMo(0.3f, 0.8f);
        rig.Look(landingPoint + new Vector2(0f, 150f), 1.8f, 0.25f);
        rig.Punch(0.1f);
        rig.Shake(0.4f);
        Flash(0.6f);
        SpawnRipple(landingPoint, 260f, 0.9f, 0.7f);
        SpawnRipple(landingPoint, 140f, 0.6f, 0.8f);
        ShowBig("釣れた！", new Color(1f, 0.9f, 0.4f));
        PlaySE(catchSE);
        robot.Cheer(resultIsNew ? "はじめて見る魚だ！" : "やったー！");
    }

    void UpdateLanding(float dt)
    {
        timer += dt;
        const float jumpTime = 0.9f;
        float p = Mathf.Clamp01(timer / jumpTime);

        if (p < 1f)
        {
            // 水面から高く跳ね上がって、桟橋に落ちる
            catchFish.anchoredPosition = landingPoint + CatchRestOffset * p + new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 300f);
            catchFish.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.3f, p);
            catchFish.localRotation = Quaternion.Euler(0f, 0f, 90f + p * 300f);
        }
        else
        {
            // 桟橋でピチピチ跳ねる
            float flop = Mathf.Abs(Mathf.Sin(timer * 12f));
            catchFish.anchoredPosition = landingPoint + CatchRestOffset + new Vector2(0f, flop * 18f);
            catchFish.localRotation = Quaternion.Euler(0f, 0f, 30f + Mathf.Sin(timer * 12f) * 25f);
        }

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
        rig.Look(landingPoint + new Vector2(0f, 40f), 1.5f, 0.6f);
        SetHint("<b>続ける</b> クリック / A");
    }

    void UpdateResult(float dt)
    {
        timer += dt;
        resultPanel.transform.localScale = Vector3.Lerp(resultPanel.transform.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 14f));
        float flop = Mathf.Abs(Mathf.Sin(timer * 8f)) * Mathf.Exp(-timer);
        catchFish.anchoredPosition = landingPoint + CatchRestOffset + new Vector2(0f, flop * 14f);

        if (timer < 0.4f) return;
        if (ActionPressed() || CancelPressed())
        {
            resultPanel.SetActive(false);
            catchFish.gameObject.SetActive(false);
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
                robot.Say("もうエネルギーが…！");
                return;
            }
            assistUsed = true;
            assistTimer = assistDuration;
            robot.SetAssist(true);
            robot.Say("まかせて！いっしょに引くよ！", assistDuration);
            PlaySE(hookSE, 0.6f);
            rig.Punch(0.05f);
            return;
        }

        if (phase != Phase.Aim && phase != Phase.Lure) return;
        if (scanTimer > 0f)
        {
            robot.Say("充電中…", 0.8f);
            return;
        }

        // サーチ：輪が届いた魚から順に光る
        scanTimer = scanCooldown;
        robot.PlayScan();
        PlaySE(nibbleSE);
        int count = 0;
        bool rare = false;
        foreach (var f in fishes)
        {
            if (f.IsLeaving) continue;
            count++;
            if (f.Species.rarity <= 3f) rare = true;
            f.Reveal(Vector2.Distance(robot.Position, f.Position) / robot.ScanSpeed, 4f);
        }
        robot.Say(rare ? "大きな気配がある…！金色の影を狙って！" : "魚が " + count + " 匹いるよ", 2.4f);
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
            float sink = bobberSunk ? 0.55f : 1f - bobberDip * 0.35f;
            bobber.anchoredPosition = BaitPosition + new Vector2(0f, bobberSunk ? -6f : 0f);
            bobber.localScale = Vector3.one * (sink + bob);
        }
        if (bobberImage != null)
        {
            var c = bobberImage.color;
            c.a = phase == Phase.Lure && bobberSunk ? 0.35f : 1f;
            bobberImage.color = c;
        }

        // 糸
        bool showLine = phase != Phase.Lost && phase != Phase.Landing && phase != Phase.Result && phase != Phase.Log && phase != Phase.Exiting;
        line.enabled = showLine;
        if (!showLine) return;

        line.start = RodTip;
        line.end = fightFish != null ? fightFish.Position : bobber.anchoredPosition;
        if (phase == Phase.Fight)
        {
            // 張っているほどまっすぐ震え、色が赤みを帯びる。ゆるいとたるむ
            line.sag = Mathf.Lerp(0.35f, 0f, Mathf.Clamp01(tension * 1.6f)) * swayDir;
            line.vibration = Mathf.Max(0f, tension - 0.5f) * 14f + thrashVisual * 2f;
            line.color = Color.Lerp(new Color(0.9f, 0.92f, 1f, 0.85f), new Color(1f, 0.5f, 0.4f, 1f), Mathf.InverseLerp(0.6f, 1f, tension));
        }
        else
        {
            line.sag = phase == Phase.Lure ? (bobberSunk ? 0.05f : 0.2f) : 0.15f;
            line.vibration = bobberSunk ? 1.5f : 0f;
            line.color = new Color(0.9f, 0.92f, 1f, 0.7f);
        }
        line.Refresh(dt);
    }

    // ─── 演出 ──────────────────────────────────────────────────
    void SpawnRipple(Vector2 position, float size, float duration, float alpha)
    {
        Ripple r = null;
        foreach (var x in ripples)
            if (x.time >= x.duration) { r = x; break; }
        if (r == null)
        {
            var go = new GameObject("Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            r = new Ripple { rect = go.GetComponent<RectTransform>(), image = go.GetComponent<Image>() };
            r.rect.SetParent(rippleLayer, false);
            r.image.sprite = circle;
            r.image.raycastTarget = false;
            ripples.Add(r);
        }
        r.rect.gameObject.SetActive(true);
        r.rect.anchoredPosition = position;
        r.time = 0f;
        r.duration = duration;
        r.size = size;
        r.alpha = alpha;
    }

    void UpdateRipples(float dt)
    {
        foreach (var r in ripples)
        {
            if (r.time >= r.duration) continue;
            r.time += dt;
            float p = Mathf.Clamp01(r.time / r.duration);
            float s = r.size * (0.3f + 0.7f * (1f - (1f - p) * (1f - p)));
            r.rect.sizeDelta = new Vector2(s, s * 0.8f);
            r.image.color = new Color(0.75f, 0.85f, 1f, r.alpha * (1f - p) * 0.6f);
            if (p >= 1f) r.rect.gameObject.SetActive(false);
        }
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

    static bool IsSlowHeld()
    {
        var kb = Keyboard.current;
        return kb != null && kb.leftShiftKey.isPressed;
    }

    bool PointerOnButtons()
    {
        return MenuInput.IsPointerOver(robot.Rect) || MenuInput.IsPointerOver(logButton);
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

    Vector2 ScreenToContent(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screen, null, out var local);
        return local;
    }
}
