using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 釣り画面の見た目（プレイヤーの後ろから池を見る構図）
///
/// 魚の泳ぎ・アタリ・ファイトの計算は MyTownFishing が池の座標（真上から見た図）で行い、
/// ここでは毎フレームそれを受け取って「手前右下に竿、奥へ横長に広がる水面」の構図に描き直す（Project）
/// マウスの位置は ScreenToPond で池の座標に戻して、狙いや餌の操作に使う
///
/// - 狙う　　：水面に平たい丸の的。遠いほど小さい
/// - 投げる　：ウキが竿先から弧を描いて飛ぶ
/// - 誘う　　：ウキが水面に浮き、魚の影が水中をうっすら泳ぐ
/// - アタリ　：ウキが沈む（一気に／少しだけ）・倒れて流れる
/// - ファイト：魚が水面に背中を出して走る。横走りは引き波と「≫」、跳ねる前は泡と「！」
/// - 釣り上げ：魚がこちらへ跳んでくる
/// ロボは左下にいて、吹き出しでしゃべる（クリックでサーチ／アシスト）
/// </summary>
public class FishingBackView : MonoBehaviour
{
    public enum BobberMode { Hidden, Hanging, Flying, Floating, Returning }

    /// <summary>
    /// ファイト中に毎フレーム MyTownFishing から渡す状態
    /// </summary>
    public struct FightState
    {
        public Vector2 fishPos;         // 池の座標
        public Vector2 fishVelocity;    // 池の座標での速さ
        public float fishLength;
        public float fishHeading;       // 魚の向き（池の座標で、度。0 で右）
        public float thrash;            // 暴れ具合 0〜1
        public float tired;             // 疲れ具合 0〜1
        public float tension;           // 糸の張り 0〜1
        public float rodTilt;           // 竿の倒し具合 -1〜1
        public float reelAngle;         // リールの回転（度）
        public int runDir;              // 横走りの向き（-1 左 / 0 / 1 右）
        public bool jumpWarn;           // 跳ねる前の予兆
        public bool airborne;           // 跳んでいる
        public float airProgress;       // 跳んでいる進み 0〜1
        public bool tiltHint;           // 「竿をこっちへ」の案内を出す
        public bool countering;         // 逆に倒せている
        public bool assisting;          // ロボのアシスト中
        public bool lineBroken;
    }

    [Header("── 画面 ──────────────────────")]
    public FishingCameraRig rig;
    public RectTransform world;         // カメラで動かす中身（rig.root と同じ）
    public Sprite circle;
    [Tooltip("上から見た魚の形（水中の影・水面に出た背中）。頭が右向き")]
    public Sprite fishTop;
    [Tooltip("横から見た魚の形（跳ねた魚）。頭が右向き")]
    public Sprite fishSide;

    [Header("── 奥行き（画面の座標） ──────────")]
    public float nearY = -330f;         // 一番手前の水面
    public float farY = 110f;           // 一番奥（地平線の少し手前）
    public float nearScale = 1.35f;
    public float farScale = 0.6f;
    [Tooltip("池の座標でこれだけ離れると一番奥に見える")]
    public float depthRange = 1100f;

    // 斜めから見た水面なので、上から見た形を縦につぶして見せる
    const float WaterSquash = 0.45f;
    const float TopAspect = 112f / 256f;
    const float SideAspect = 128f / 256f;

    [Header("── 水面の小物 ──────────────────")]
    public RectTransform shadowLayer;   // 水中の魚の影
    public RectTransform rippleLayer;
    public RectTransform reticle;       // 狙いの的
    public RectTransform bobber;        // ウキ（pivot は水面）
    public Image bobberImage;
    public Image scanRing;
    public float scanDuration = 1.2f;
    [Tooltip("水中の魚の影の大きさの倍率")]
    public float shadowSizeRate = 1.8f;
    public float bobberSize = 1.3f;

    [Header("── ファイト中の魚 ────────────────")]
    public RectTransform fish;
    public Image fishBack;
    public RectTransform fishFin;
    public Image wake;
    public RectTransform airFish;
    public Image airBody;
    public Image airTail;
    public Color backColor = new Color(0.05f, 0.08f, 0.12f, 0.9f);
    public Color bellyColor = new Color(0.75f, 0.78f, 0.72f, 0.95f);
    [Tooltip("水面に出ている魚の大きさの倍率（遠くても見えるように大きめ）")]
    public float fishSizeRate = 2.2f;

    [Header("── 竿・糸 ──────────────────────")]
    public FishingLineGraphic rod;
    public FishingLineGraphic line;
    public RectTransform reel;
    public Vector2 rodBase = new Vector2(330f, -430f);   // 手元（プレイヤーの手）
    public float rodLength = 560f;

    [Header("── 合図（少しだけの記号） ──────────")]
    public TMP_Text runArrow;
    public TMP_Text jumpMark;
    public TMP_Text tiltHint;

    [Header("── ロボ ──────────────────────")]
    public RectTransform robot;
    public Image robotEye;
    public RectTransform bubble;
    public TMP_Text bubbleText;
    public Color eyeNormal = new Color(0.4f, 0.9f, 1f);
    public Color eyeAlert = new Color(1f, 0.3f, 0.25f);
    public Color eyeAssist = new Color(1f, 0.85f, 0.3f);

    [Header("── しぶき ──────────────────────")]
    public RectTransform sprayLayer;

    public TMP_FontAsset font;

    class Drop
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public float life, age, size;
    }

    class RippleMark
    {
        public RectTransform rect;
        public Image image;
        public Vector2 pond;
        public float time, duration, size, alpha;
    }

    class Shadow
    {
        public RectTransform rect;
        public Image body, glow;        // body は縦につぶした親の子。向きに合わせて回す
        public TMP_Text label;
        public bool seen;
    }

    readonly List<Drop> drops = new List<Drop>();
    readonly List<RippleMark> ripples = new List<RippleMark>();
    readonly Dictionary<FishingFish, Shadow> shadows = new Dictionary<FishingFish, Shadow>();

    Vector2 landing;
    float bubbleTimer, alertTimer, scanTimer = -1f, robotHop;
    bool assisting;
    float sprayTimer, finPhase;
    float fishFade = 1f;
    Vector2 fishScreen;
    float fishScale = 1f;
    float rodSpring;

    // 毎フレーム受け取る、ファイト以外の状態
    bool aimVisible;
    Vector2 aimPond;
    BobberMode bobberMode;
    Vector2 bobberPond;
    float bobberProgress, bobberDip;
    int biteKind = -1;                  // -1 なし / 0 一気に沈む / 1 少し沈む / 2 浮いて倒れる
    Vector2 focusPond;
    float focusZoom = 1f, focusFollow = 0.5f;
    Vector2 targetPond;                 // 竿を向ける先

    public bool InFight { get; private set; }
    public float TimeScale => rig != null ? rig.TimeScale : 1f;
    public RectTransform RobotRect => robot;

    // ─── 開始・終了 ────────────────────────────────────────────
    public void Open(Vector2 landingPoint)
    {
        landing = landingPoint;
        InFight = false;
        fishFade = 1f;
        rodSpring = 0f;
        foreach (var d in drops) { d.age = d.life; d.rect.gameObject.SetActive(false); }
        foreach (var r in ripples) { r.time = r.duration; r.rect.gameObject.SetActive(false); }
        ClearShadows();
        HideFightMarks();
        fish.gameObject.SetActive(false);
        airFish.gameObject.SetActive(false);
        bubble.gameObject.SetActive(false);
        scanRing.gameObject.SetActive(false);
        // 町から寄ってきた続きに見えるよう、寄った状態から引く
        rig.Snap(new Vector2(0f, -120f), 1.5f);
        rig.Look(Vector2.zero, 1f, 0.7f);
    }

    public void Close()
    {
        InFight = false;
        ClearShadows();
    }

    // ─── 池の座標 ⇔ この画面の座標 ──────────────────────────────
    // 桟橋から遠いほど奥（上）に小さく、横の広がりも狭く見える
    public void Project(Vector2 pondPos, out Vector2 screen, out float scale)
    {
        Vector2 rel = pondPos - landing;
        float d = Mathf.Pow(Mathf.Clamp01(rel.y / depthRange), 0.75f);
        scale = Mathf.Lerp(nearScale, farScale, d);
        screen = new Vector2(rel.x * Mathf.Lerp(0.95f, 0.35f, d), Mathf.Lerp(nearY, farY, d));
    }

    /// <summary>
    /// マウスの画面位置を、池の座標に戻す（水面を指しているとみなす）
    /// </summary>
    public Vector2 ScreenToPond(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(world, screenPoint, null, out var local);
        float d = Mathf.Clamp01(Mathf.InverseLerp(nearY, farY, local.y));
        float relY = depthRange * Mathf.Pow(d, 1f / 0.75f);
        float relX = local.x / Mathf.Lerp(0.95f, 0.35f, d);
        return landing + new Vector2(relX, relY);
    }

    // ─── 毎フレームの状態（MyTownFishing から） ──────────────────
    public void SetAim(bool visible, Vector2 pond)
    {
        aimVisible = visible;
        aimPond = pond;
    }

    /// <summary>
    /// ウキの状態。progress は投げる・巻き戻す途中の進み（0〜1）、dip はつつかれた時の沈み（0〜1）
    /// </summary>
    public void SetBobber(BobberMode mode, Vector2 pond, float progress, float dip, int bite)
    {
        bobberMode = mode;
        bobberPond = pond;
        bobberProgress = progress;
        bobberDip = dip;
        biteKind = bite;
    }

    /// <summary>
    /// ファイト以外の時のカメラ。注目する池の位置と倍率
    /// </summary>
    public void SetFocus(Vector2 pond, float zoom, float follow)
    {
        focusPond = pond;
        focusZoom = zoom;
        focusFollow = follow;
    }

    public void SetRodTarget(Vector2 pond)
    {
        targetPond = pond;
    }

    // ─── 水中の魚の影 ──────────────────────────────────────────
    public void SyncFish(List<FishingFish> fishes, FishingFish hooked)
    {
        foreach (var s in shadows.Values) s.seen = false;
        foreach (var f in fishes)
        {
            if (f == null) continue;
            if (!shadows.TryGetValue(f, out var s))
            {
                s = MakeShadow(f);
                shadows.Add(f, s);
            }
            s.seen = true;
            UpdateShadow(f, s, f == hooked);
        }

        // 消えた魚の影を片付ける
        List<FishingFish> gone = null;
        foreach (var kv in shadows)
        {
            if (kv.Value.seen) continue;
            (gone ??= new List<FishingFish>()).Add(kv.Key);
        }
        if (gone == null) return;
        foreach (var f in gone)
        {
            Destroy(shadows[f].rect.gameObject);
            shadows.Remove(f);
        }
    }

    Shadow MakeShadow(FishingFish f)
    {
        var go = new GameObject("Shadow_" + f.Species.id, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(shadowLayer, false);
        rt.sizeDelta = Vector2.zero;
        var s = new Shadow { rect = rt };
        s.glow = MakeImage(rt, "Glow", circle);
        var squash = new GameObject("Squash", typeof(RectTransform)).GetComponent<RectTransform>();
        squash.SetParent(rt, false);
        squash.sizeDelta = Vector2.zero;
        squash.localScale = new Vector3(1f, WaterSquash, 1f);
        s.body = MakeImage(squash, "Body", fishTop != null ? fishTop : circle);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        s.label = labelGo.GetComponent<TextMeshProUGUI>();
        s.label.rectTransform.SetParent(rt, false);
        s.label.rectTransform.sizeDelta = new Vector2(300f, 44f);
        s.label.fontSize = 30f;
        s.label.alignment = TextAlignmentOptions.Center;
        s.label.raycastTarget = false;
        if (font != null) s.label.font = font;
        return s;
    }

    Image MakeImage(RectTransform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    void UpdateShadow(FishingFish f, Shadow s, bool hooked)
    {
        // ファイト中の魚は水面に出ているので、影は出さない
        s.rect.gameObject.SetActive(!hooked);
        if (hooked) return;

        Project(f.Position, out var screen, out var scale);
        s.rect.anchoredPosition = screen;
        // 上から見た魚を泳ぐ向きに回し、縦につぶした親で斜めから見た形にする（横向きなら長く、奥向きなら短い）
        float len = f.Species.shadowLength * scale * shadowSizeRate;
        s.body.rectTransform.sizeDelta = new Vector2(len, len * TopAspect * Mathf.Max(1f, f.Species.shadowWidthRate / 0.38f));
        s.body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, f.Heading);
        s.body.color = new Color(0.02f, 0.04f, 0.07f, f.ShadowAlpha * 0.85f);

        s.glow.rectTransform.sizeDelta = new Vector2(len * 1.3f, len * 0.5f);
        var glow = f.GlowColor;
        glow.a = f.GlowAlpha;
        s.glow.color = glow;

        bool labeled = f.LabelText != null;
        s.label.gameObject.SetActive(labeled);
        if (labeled)
        {
            s.label.text = f.LabelText;
            s.label.color = new Color(0.9f, 0.95f, 1f, f.LabelAlpha);
            s.label.rectTransform.anchoredPosition = new Vector2(0f, len * 0.2f + 30f);
        }
    }

    void ClearShadows()
    {
        foreach (var s in shadows.Values)
            if (s.rect != null) Destroy(s.rect.gameObject);
        shadows.Clear();
    }

    // ─── 波紋・しぶき ──────────────────────────────────────────
    public void Ripple(Vector2 pond, float size, float duration, float alpha)
    {
        RippleMark r = null;
        foreach (var x in ripples)
            if (x.time >= x.duration) { r = x; break; }
        if (r == null)
        {
            var go = new GameObject("Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            r = new RippleMark { rect = go.GetComponent<RectTransform>(), image = go.GetComponent<Image>() };
            r.rect.SetParent(rippleLayer, false);
            r.image.sprite = circle;
            r.image.raycastTarget = false;
            ripples.Add(r);
        }
        r.rect.gameObject.SetActive(true);
        r.pond = pond;
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
            Project(r.pond, out var screen, out var scale);
            // 斜めから見た水面なので、波紋は横長のだ円
            float s = r.size * scale * (0.3f + 0.7f * (1f - (1f - p) * (1f - p)));
            r.rect.anchoredPosition = screen;
            r.rect.sizeDelta = new Vector2(s, s * 0.28f);
            r.image.color = new Color(0.75f, 0.85f, 1f, r.alpha * (1f - p) * 0.6f);
            if (p >= 1f) r.rect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 池の座標の位置から、白いしぶきを跳ね上げる
    /// </summary>
    public void Spray(Vector2 pond, float power, int count)
    {
        Project(pond, out var screen, out var scale);
        SprayAt(screen, scale * power, count);
    }

    void SprayAt(Vector2 screen, float power, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var d = GetDrop();
            d.rect.anchoredPosition = screen + new Vector2(Random.Range(-30f, 30f) * power, Random.Range(-6f, 6f));
            d.velocity = new Vector2(Random.Range(-220f, 220f), Random.Range(260f, 620f)) * power;
            d.size = Random.Range(10f, 26f) * power;
            d.life = Random.Range(0.45f, 0.85f);
            d.age = 0f;
        }
    }

    Drop GetDrop()
    {
        foreach (var d in drops)
            if (d.age >= d.life) { d.rect.gameObject.SetActive(true); return d; }

        var go = new GameObject("Drop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var drop = new Drop { rect = go.GetComponent<RectTransform>(), image = go.GetComponent<Image>() };
        drop.rect.SetParent(sprayLayer, false);
        drop.image.sprite = circle;
        drop.image.raycastTarget = false;
        drops.Add(drop);
        return drop;
    }

    void UpdateDrops(float dt)
    {
        foreach (var d in drops)
        {
            if (d.age >= d.life) continue;
            d.age += dt;
            d.velocity.y -= 1500f * dt;
            d.rect.anchoredPosition += d.velocity * dt;
            float p = d.age / d.life;
            d.rect.sizeDelta = Vector2.one * d.size * (1f - 0.5f * p);
            d.image.color = new Color(0.85f, 0.92f, 1f, 0.85f * (1f - p));
            if (d.age >= d.life) d.rect.gameObject.SetActive(false);
        }
    }

    // ─── ロボ ──────────────────────────────────────────────────
    public void Say(string text, float duration = 1.6f)
    {
        bubbleText.text = text;
        bubbleTimer = duration;
        bubble.gameObject.SetActive(true);
        bubble.localScale = Vector3.one * 1.2f;
    }

    /// <summary>
    /// 目を赤くして跳ねながら知らせる（アタリの合図）
    /// </summary>
    public void Alert(string text)
    {
        alertTimer = 0.8f;
        robotHop = 1f;
        Say(text, 0.9f);
    }

    public void Cheer(string text)
    {
        robotHop = 1f;
        Say(text, 2.2f);
    }

    public void HideBubble()
    {
        bubbleTimer = 0f;
        bubble.gameObject.SetActive(false);
    }

    public void SetAssist(bool on)
    {
        assisting = on;
    }

    /// <summary>
    /// サーチ：ロボのいる手前から、奥へ波紋が水面を広がっていく
    /// </summary>
    public void PlayScan()
    {
        scanTimer = 0f;
        robotHop = 0.5f;
        scanRing.gameObject.SetActive(true);
    }

    /// <summary>
    /// サーチの波紋がその位置に届くまでの時間（魚が光るタイミング）
    /// </summary>
    public float ScanDelay(Vector2 pond)
    {
        return Mathf.Clamp01((pond.y - landing.y) / depthRange) * scanDuration;
    }

    // ─── ファイト ──────────────────────────────────────────────
    public void StartFight(FishSpecies species, Vector2 fishPos)
    {
        InFight = true;
        fishFade = 1f;
        rodSpring = 0f;

        // 跳ねた魚は横から見た形。細長い魚（ウナギなど）は細く
        float len = species.shadowLength * 1.5f;
        airBody.sprite = fishSide != null ? fishSide : circle;
        airBody.rectTransform.sizeDelta = new Vector2(len, len * SideAspect * Mathf.Clamp(species.shadowWidthRate / 0.38f, 0.45f, 1.1f));
        airBody.color = species.color;
        if (airTail != null) airTail.gameObject.SetActive(false);
        fishBack.sprite = fishTop != null ? fishTop : circle;
        airFish.gameObject.SetActive(false);
        fish.gameObject.SetActive(true);
        HideFightMarks();

        Spray(fishPos, 1.4f, 22);
    }

    /// <summary>
    /// ファイトを終えて、いつもの池の様子に戻る（釣り上げた魚の姿は残す）
    /// </summary>
    public void EndFight()
    {
        InFight = false;
        fish.gameObject.SetActive(false);
        HideFightMarks();
    }

    public void HideAirFish()
    {
        airFish.gameObject.SetActive(false);
    }

    void HideFightMarks()
    {
        runArrow.gameObject.SetActive(false);
        jumpMark.gameObject.SetActive(false);
        tiltHint.gameObject.SetActive(false);
    }

    public void TickFight(FightState s, float dt)
    {
        if (!InFight) return;

        Project(s.fishPos, out fishScreen, out fishScale);
        // 横走りの強さは池の座標で見る（遠くの魚は画面上の動きが小さいので）
        float lateral = s.fishVelocity.x;
        float lateralRate = Mathf.Clamp01(Mathf.Abs(lateral) / 200f);

        // 魚：水面に出ている背中。上から見た形を泳ぐ向きに回すので、横に走ると長く、沖へ逃げると短く見える
        // 暴れるほど激しく体をくねらせ、疲れると浮いて腹（明るい色）を見せる
        float len = s.fishLength * fishScale * fishSizeRate;
        fish.anchoredPosition = fishScreen + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 18f) * 6f * s.thrash);
        fishBack.rectTransform.sizeDelta = new Vector2(len, len * TopAspect * 1.15f);
        float wiggle = Mathf.Sin(Time.unscaledTime * Mathf.Lerp(3f, 20f, s.thrash)) * Mathf.Lerp(4f, 22f, s.thrash);
        fishBack.rectTransform.localRotation = Quaternion.Euler(0f, 0f, s.fishHeading + wiggle);
        Color back = Color.Lerp(backColor, bellyColor, Mathf.InverseLerp(0.55f, 1f, s.tired));
        back.a *= fishFade * (s.airborne ? 0f : 1f);
        fishBack.color = back;

        // 背びれ：水面から突き出た三角。少し揺れる
        finPhase += dt * Mathf.Lerp(4f, 26f, s.thrash);
        fishFin.sizeDelta = Vector2.one * len * 0.16f;
        fishFin.anchoredPosition = new Vector2(Mathf.Sin(finPhase) * len * 0.05f, len * 0.06f);
        fishFin.GetComponent<Image>().color = new Color(back.r, back.g, back.b, back.a * (1f - s.tired));

        // 引き波：走る方向の反対側に白く伸びる
        float wakeLen = len * (0.8f + 2.2f * lateralRate);
        wake.rectTransform.sizeDelta = new Vector2(wakeLen, len * 0.3f);
        wake.rectTransform.anchoredPosition = new Vector2(-Mathf.Sign(lateral) * wakeLen * 0.45f, -len * 0.05f);
        wake.color = new Color(0.85f, 0.93f, 1f, (0.15f + 0.55f * Mathf.Max(lateralRate, s.thrash * 0.5f)) * fishFade * (s.airborne ? 0f : 1f));

        // 暴れている間は水面が割れてしぶきが上がり続ける。跳ねる前は泡立つ
        sprayTimer -= dt;
        if (sprayTimer <= 0f && !s.airborne && !s.lineBroken)
        {
            if (s.jumpWarn) { sprayTimer = 0.03f; SprayAt(fishScreen, fishScale * 0.9f, 4); }
            else if (s.thrash > 0.5f) { sprayTimer = 0.05f; SprayAt(fishScreen, fishScale * 1.5f * s.thrash, 3); }
            else { sprayTimer = 0.4f; SprayAt(fishScreen, fishScale * 0.5f, 1); }
        }

        // 跳ねる：水面から飛び出して首を振る
        airFish.gameObject.SetActive(s.airborne);
        if (s.airborne)
        {
            float p = s.airProgress;
            airFish.anchoredPosition = fishScreen + new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 300f * fishScale);
            airFish.localScale = Vector3.one * fishScale * 1.2f;
            airFish.localRotation = Quaternion.Euler(0f, 0f, 70f + Mathf.Sin(p * 28f) * 45f);
        }

        // 合図の記号
        bool running = s.runDir != 0 && lateralRate > 0.45f && !s.airborne;
        runArrow.gameObject.SetActive(running);
        if (running)
        {
            runArrow.text = s.runDir > 0 ? "≫" : "≪";
            runArrow.rectTransform.anchoredPosition = fishScreen + new Vector2(s.runDir * (len * 0.8f + 90f), len * 0.15f);
            runArrow.alpha = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f));
        }

        jumpMark.gameObject.SetActive(s.jumpWarn);
        if (s.jumpWarn)
        {
            jumpMark.rectTransform.anchoredPosition = fishScreen + new Vector2(0f, len * 0.5f + 70f);
            jumpMark.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f)));
        }

        // 竿：魚の方へ向き（引っぱられる）、プレイヤーが倒した分だけ傾き、張りが強いほど大きくしなる
        if (rodSpring > 0f) rodSpring = Mathf.MoveTowards(rodSpring, 0f, dt * 2f);
        float tension = s.lineBroken ? 0f : s.tension;
        Vector2 tip = DrawRod(fishScreen, s.rodTilt, tension, s.thrash, dt);

        line.enabled = !s.lineBroken;
        if (!s.lineBroken)
        {
            float bendSide = fishScreen.x < tip.x ? 1f : -1f;
            line.start = tip;
            line.end = s.airborne ? airFish.anchoredPosition : fishScreen;
            line.sag = Mathf.Lerp(0.3f, 0f, Mathf.Clamp01(tension * 1.6f)) * bendSide;
            line.vibration = Mathf.Max(0f, tension - 0.5f) * 16f + s.thrash * 2f;
            line.color = Color.Lerp(new Color(0.92f, 0.94f, 1f, 0.9f), new Color(1f, 0.45f, 0.35f, 1f), Mathf.InverseLerp(0.6f, 1f, tension));
            line.Refresh(dt);
        }

        // 案内：最初の数回だけ、竿の先に「倒す向き」を出す
        bool hint = s.tiltHint && s.runDir != 0 && !s.countering;
        tiltHint.gameObject.SetActive(hint);
        if (hint)
        {
            tiltHint.text = s.runDir > 0 ? "◀ 竿を左へ" : "竿を右へ ▶";
            tiltHint.rectTransform.anchoredPosition = tip + new Vector2(-s.runDir * 170f, -40f);
            tiltHint.alpha = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
        }

        if (reel != null) reel.localRotation = Quaternion.Euler(0f, 0f, -s.reelAngle);
        assisting = s.assisting;

        // カメラ：魚を少し追う。跳ねる前は魚へぐっと寄る
        Vector2 focus = new Vector2(fishScreen.x * 0.3f, fishScreen.y * 0.15f);
        if (s.jumpWarn || s.airborne) rig.Look(Vector2.Lerp(focus, fishScreen, 0.35f), 1.15f, 0.2f);
        else rig.Look(focus, 1f + s.thrash * 0.04f, 0.4f);
    }

    // 竿を描いて、竿先の位置を返す
    Vector2 DrawRod(Vector2 target, float tilt, float tension, float shake, float dt)
    {
        float toTarget = Vector2.SignedAngle(Vector2.up, target - rodBase);
        float angle = Mathf.Clamp(toTarget * 0.55f - tilt * 35f, -75f, 75f) + Mathf.Sin(Time.unscaledTime * 40f) * rodSpring * 10f;
        Vector2 tip = rodBase + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.up) * rodLength * (1f - 0.25f * tension);
        // しなりは狙う側へ。FishingLineGraphic の sag は「線の左側」が正
        float bendSide = target.x < tip.x ? 1f : -1f;
        rod.start = rodBase;
        rod.end = tip;
        rod.sag = bendSide * (0.1f + 0.38f * tension);
        rod.vibration = tension * tension * 5f + shake * 2f;
        rod.Refresh(dt);
        return tip;
    }

    /// <summary>
    /// 糸が切れた：竿が跳ね返り、しぶきが上がる
    /// </summary>
    public void Snap()
    {
        rodSpring = 1f;
        SprayAt(fishScreen, fishScale, 14);
    }

    /// <summary>
    /// 逃げられた：魚の背中が沈んで消える
    /// </summary>
    public void FishDive(float dt)
    {
        fishFade = Mathf.MoveTowards(fishFade, 0f, dt * 2f);
        var c = fishBack.color;
        c.a = backColor.a * fishFade;
        fishBack.color = c;
        fishFin.GetComponent<Image>().color = c;
        wake.color = new Color(1f, 1f, 1f, 0f);
        HideFightMarks();
    }

    /// <summary>
    /// 釣り上げ：魚がカメラの方へ跳び上がってくる（progress 0〜1）。跳びきったら手前で跳ね続ける
    /// </summary>
    public void LandingJump(float progress, float time)
    {
        airFish.gameObject.SetActive(true);
        fishFade = 0f;
        fish.gameObject.SetActive(false);
        line.enabled = false;
        HideFightMarks();

        // 結果カードの下、手前左寄りに落ちてくる
        Vector2 to = new Vector2(-120f, -290f);
        const float endScale = 2f;
        if (progress < 1f)
        {
            airFish.anchoredPosition = Vector2.Lerp(fishScreen, to, progress) + new Vector2(0f, Mathf.Sin(progress * Mathf.PI) * 320f);
            airFish.localScale = Vector3.one * Mathf.Lerp(fishScale, endScale, progress * progress);
            airFish.localRotation = Quaternion.Euler(0f, 0f, 60f + progress * 340f);
        }
        else
        {
            // 手前でピチピチ跳ねる
            float flop = Mathf.Abs(Mathf.Sin(time * 10f)) * Mathf.Exp(-(time - 1f) * 0.8f);
            airFish.anchoredPosition = to + new Vector2(0f, flop * 40f);
            airFish.localScale = Vector3.one * endScale;
            airFish.localRotation = Quaternion.Euler(0f, 0f, 30f + Mathf.Sin(time * 10f) * 20f);
        }
        rig.Look(new Vector2(0f, -120f), 1.1f, 0.4f);
    }

    // ─── 毎フレームの描画（ファイト以外） ─────────────────────────
    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float simDt = dt * TimeScale;
        UpdateDrops(simDt);
        UpdateRipples(simDt);
        UpdateRobot(dt);
        UpdateScan(dt);

        // ファイト中（釣り上げ・結果も含む）は竿と糸を TickFight / LandingJump が描く。ウキと的は出さない
        if (InFight)
        {
            reticle.gameObject.SetActive(false);
            bobber.gameObject.SetActive(false);
            return;
        }

        // 狙いの的：水面に平たく置く
        reticle.gameObject.SetActive(aimVisible);
        if (aimVisible)
        {
            Project(aimPond, out var screen, out var scale);
            reticle.anchoredPosition = screen;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.06f;
            reticle.sizeDelta = new Vector2(150f, 44f) * scale * pulse;
        }

        // ウキと竿・糸
        Vector2 rodTarget;
        Project(targetPond, out rodTarget, out _);
        Vector2 tip = DrawRod(rodTarget, 0f, bobberMode == BobberMode.Floating && biteKind == 0 ? 0.35f : 0.05f, 0f, dt);
        Vector2 bobberScreen = tip + new Vector2(16f, -70f);
        float bobberScale = 1f;
        float tilt = 0f;
        float alpha = 1f;

        switch (bobberMode)
        {
            case BobberMode.Flying:
            {
                Project(bobberPond, out var to, out var s);
                float p = bobberProgress;
                bobberScreen = Vector2.Lerp(tip, to, 1f - (1f - p) * (1f - p)) + new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 260f);
                bobberScale = Mathf.Lerp(1f, s, p);
                break;
            }
            case BobberMode.Returning:
            {
                Project(bobberPond, out var from, out var s);
                float p = bobberProgress;
                bobberScreen = Vector2.Lerp(from, tip + new Vector2(16f, -70f), p * p);
                bobberScale = Mathf.Lerp(s, 1f, p);
                break;
            }
            case BobberMode.Floating:
            {
                Project(bobberPond, out bobberScreen, out bobberScale);
                bobberScreen.y += Mathf.Sin(Time.unscaledTime * 3f) * 2f * bobberScale - bobberDip * 10f * bobberScale;
                // アタリの見え方：一気に沈む / 少しだけ沈んで止まる / 浮いて横に倒れる
                if (biteKind == 0) { bobberScreen.y -= 14f * bobberScale; alpha = 0.3f; }
                else if (biteKind == 1) { bobberScreen.y -= 5f * bobberScale; alpha = 0.85f; }
                else if (biteKind == 2) { bobberScreen.y += 4f * bobberScale; tilt = 75f; }
                break;
            }
        }

        bool showBobber = bobberMode != BobberMode.Hidden;
        bobber.gameObject.SetActive(showBobber);
        if (showBobber)
        {
            bobber.anchoredPosition = bobberScreen;
            bobber.localScale = Vector3.one * bobberScale * bobberSize;
            bobber.localRotation = Quaternion.Euler(0f, 0f, tilt);
            var c = bobberImage.color;
            c.a = alpha;
            bobberImage.color = c;
        }

        line.enabled = showBobber;
        if (showBobber)
        {
            line.start = tip;
            line.end = bobberScreen;
            line.sag = bobberMode == BobberMode.Floating ? (biteKind == 0 ? 0.04f : biteKind == 2 ? 0.3f : 0.18f) : 0.1f;
            line.vibration = biteKind == 0 ? 1.5f : 0f;
            line.color = new Color(0.92f, 0.94f, 1f, 0.75f);
            line.Refresh(dt);
        }

        // カメラ：注目点を少し追う（遠くのウキほど寄る）
        Project(focusPond, out var focusScreen, out _);
        rig.Look(new Vector2(focusScreen.x * 0.5f, focusScreen.y * 0.5f), focusZoom, focusFollow);
    }

    void UpdateRobot(float dt)
    {
        robotHop = Mathf.MoveTowards(robotHop, 0f, dt * 2.5f);
        if (alertTimer > 0f) alertTimer -= dt;
        float t = Time.unscaledTime;
        Vector2 offset = new Vector2(0f, Mathf.Sin(t * 3f) * 5f + Mathf.Sin(robotHop * Mathf.PI) * 36f);
        if (assisting) offset += new Vector2(Mathf.Sin(t * 60f) * 3f, -6f);
        robot.anchoredPosition = new Vector2(150f, 290f) + offset;

        if (robotEye != null)
        {
            robotEye.color = assisting ? eyeAssist : alertTimer > 0f ? eyeAlert : eyeNormal;
            float blink = Mathf.Repeat(t, 3.3f) < 0.12f ? 0.2f : 1f;
            robotEye.rectTransform.localScale = new Vector3(1f, blink * (alertTimer > 0f ? 1.4f : 1f), 1f);
        }

        if (bubble.gameObject.activeSelf)
        {
            bubbleTimer -= dt;
            bubble.localScale = Vector3.Lerp(bubble.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 18f));
            if (bubbleTimer <= 0f) bubble.gameObject.SetActive(false);
        }
    }

    void UpdateScan(float dt)
    {
        if (scanTimer < 0f) return;
        scanTimer += dt;
        float p = scanTimer / scanDuration;
        // ロボのいる左手前から、水面を奥へ広がるだ円
        float r = Mathf.Lerp(0f, 2600f, p);
        scanRing.rectTransform.anchoredPosition = new Vector2(-600f, nearY);
        scanRing.rectTransform.sizeDelta = new Vector2(r * 2f, r * 0.55f);
        scanRing.color = new Color(eyeNormal.r, eyeNormal.g, eyeNormal.b, 0.25f * (1f - p));
        if (p >= 1f)
        {
            scanTimer = -1f;
            scanRing.gameObject.SetActive(false);
        }
    }
}
