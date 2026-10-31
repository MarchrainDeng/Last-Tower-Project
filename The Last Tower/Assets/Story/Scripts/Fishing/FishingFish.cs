using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 池を泳ぐ魚の影1匹分
///
/// 【流れ】
/// Wander（うろうろ）→ 餌に気づくと Approach（そっと近づく）→ Nibble（つつく＝フェイント）
/// → Bite（本アタリ。この間に合わせる）→ Hooked（ファイト中。動きは MyTownFishing が決める）
/// 本アタリは3種類（BiteKind）。種類ごとに猶予と、餌（ウキ）の動き方が違う
/// 餌を速く動かしたり、早合わせしたり、合わせ損ねると Flee（逃げる）→ Gone（消える）
///
/// 見た目は全部コードで作る（楕円の影＋しっぽ。スキャン中は光と名札を出す）
/// 座標は池の座標（真上から見た図）。画面に映すのは FishingBackView で、
/// ここでは「どう見えるか」（影の濃さ・光・名札）を ShadowAlpha などで渡す
/// </summary>
public class FishingFish : MonoBehaviour
{
    public enum State { Wander, Approach, Nibble, Bite, Hooked, Flee, Gone }

    /// <summary>
    /// 本アタリの出方
    /// Sudden: 消し込み（一気に沈む）/ Hold: 居食い（少し沈んで止まる）/ Lift: 食い上げ（浮いて横に流れる）
    /// </summary>
    public enum BiteKind { Sudden, Hold, Lift }

    const float BaseAlpha = 0.45f;
    const float RevealedAlpha = 0.8f;

    public FishSpecies Species { get; private set; }
    public float SizeCm { get; private set; }
    public State Current { get; private set; }
    public Vector2 Position { get; private set; }
    public BiteKind CurrentBite { get; private set; }
    // 本アタリが始まってからの経過（0〜1）。待つほど深く食い込む
    public float BiteProgress => biteDuration > 0f ? Mathf.Clamp01(1f - stateTimer / biteDuration) : 0f;

    // 見た目（FishingBackView が使う）
    public float Heading => heading;
    public float ShadowAlpha { get; private set; }
    public float GlowAlpha { get; private set; }
    public Color GlowColor { get; private set; }
    public string LabelText { get; private set; }     // サーチで光っている間だけ。それ以外は null
    public float LabelAlpha { get; private set; }

    MyTownFishing pond;
    RectTransform rect, body, tail;
    Image shadow, tailImage, glow;
    TMP_Text nameTag;

    Vector2 velocity, wanderTarget;
    float heading;          // 向き（度）。0で右向き
    float tailPhase;
    float fade;             // 出現・消滅のフェード 0〜1
    float stateTimer;
    int nibblesLeft;
    bool darting;           // つつく瞬間の突進中
    float revealDelay, revealTimer;
    float thrash, tired;    // ファイト中の見た目（暴れ具合・疲れ具合）
    float biteDuration;
    bool hidden;            // エラ洗いで水面から飛び出している間は影を消す

    float Length => Species.shadowLength;
    float ContactDistance => Length * 0.5f + 8f;
    // 警戒心が強いほど、ゆっくりした餌の動きでも逃げる
    float SpookSpeed => Mathf.Lerp(230f, 95f, Species.wariness) * pond.spookScale;
    float SightRange => Mathf.Lerp(330f, 230f, Species.wariness);

    // ─── 生成 ──────────────────────────────────────────────────
    public static FishingFish Create(RectTransform parent, FishSpecies species, Vector2 position,
                                     MyTownFishing owner, Sprite circle, TMP_FontAsset font)
    {
        var go = new GameObject("Fish_" + species.id, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;

        var fish = go.AddComponent<FishingFish>();
        fish.Build(species, circle, font);
        fish.pond = owner;
        fish.SizeCm = Mathf.Round(Random.Range(species.minSize, species.maxSize) * 10f) / 10f;
        fish.Position = position;
        fish.heading = Random.Range(0f, 360f);
        fish.wanderTarget = position;
        fish.Current = State.Wander;
        fish.Apply(0f);
        return fish;
    }

    void Build(FishSpecies species, Sprite circle, TMP_FontAsset font)
    {
        Species = species;
        rect = (RectTransform)transform;
        float len = species.shadowLength;
        float width = len * species.shadowWidthRate;

        // スキャン時の光（向きに関係なく丸く光る）
        glow = MakeImage(rect, "Glow", circle, new Vector2(len * 1.5f, len * 1.5f));

        body = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
        body.SetParent(rect, false);
        body.sizeDelta = Vector2.zero;

        // しっぽ（体の後ろ。根元を軸に振る）
        tail = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
        tail.SetParent(body, false);
        tail.pivot = new Vector2(1f, 0.5f);
        tail.anchoredPosition = new Vector2(-len * 0.38f, 0f);
        tail.sizeDelta = new Vector2(len * 0.3f, width * 0.8f);
        tailImage = tail.GetComponent<Image>();
        tailImage.sprite = circle;
        tailImage.raycastTarget = false;

        shadow = MakeImage(body, "Shadow", circle, new Vector2(len, width));

        nameTag = new GameObject("Tag", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        nameTag.rectTransform.SetParent(rect, false);
        nameTag.rectTransform.sizeDelta = new Vector2(260f, 40f);
        nameTag.rectTransform.anchoredPosition = new Vector2(0f, len * 0.45f + 26f);
        nameTag.fontSize = 26f;
        nameTag.alignment = TextAlignmentOptions.Center;
        nameTag.raycastTarget = false;
        if (font != null) nameTag.font = font;
    }

    static Image MakeImage(RectTransform parent, string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // ─── 外から呼ぶ ────────────────────────────────────────────
    public bool IsEngaged => Current == State.Approach || Current == State.Nibble || Current == State.Bite;
    public bool IsLeaving => Current == State.Flee || Current == State.Gone;

    /// <summary>
    /// ロボのスキャン。delay 秒後に duration 秒だけ光って名前を見せる
    /// </summary>
    public void Reveal(float delay, float duration)
    {
        revealDelay = delay;
        revealTimer = duration;
    }

    /// <summary>
    /// 近くに餌が落ちた。警戒心の強い魚は驚いて逃げ、そうでなければ気づいて寄ってくる
    /// </summary>
    public void OnSplash(Vector2 point)
    {
        if (Current != State.Wander) return;
        float d = Vector2.Distance(point, Position);
        if (d < Length * 0.5f + 40f + Species.wariness * 90f && Random.value < Species.wariness + 0.2f)
        {
            Flee();
            return;
        }
        if (d < SightRange && Random.value < 0.6f) TryEngage();
    }

    public void Hook()
    {
        Current = State.Hooked;
        velocity = Vector2.zero;
        hidden = false;
    }

    public void SetHidden(bool value)
    {
        hidden = value;
        Apply(0f);
    }

    /// <summary>
    /// ファイト中の位置と見た目。thrashAmount は暴れ具合 0〜1、tiredAmount は疲れ具合 0〜1
    /// </summary>
    public void SetHooked(Vector2 position, Vector2 awayFrom, float thrashAmount, float tiredAmount, float deltaTime)
    {
        Vector2 moved = position - Position;
        Position = position;
        thrash = thrashAmount;
        tired = tiredAmount;

        // 基本は糸と反対（池の奥）を向き、暴れるほど激しく首を振る
        Vector2 away = Position - awayFrom;
        float target = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
        if (moved.sqrMagnitude > 4f) target = Mathf.LerpAngle(target, Mathf.Atan2(moved.y, moved.x) * Mathf.Rad2Deg, 0.4f);
        target += Mathf.Sin(Time.unscaledTime * Mathf.Lerp(4f, 22f, thrash)) * Mathf.Lerp(8f, 45f, thrash) * (1f - tired * 0.6f);
        heading = Mathf.LerpAngle(heading, target, 1f - Mathf.Exp(-deltaTime * 10f));

        tailPhase += deltaTime * Mathf.Lerp(5f, 30f, thrash) * (1f - tired * 0.7f);
        Apply(deltaTime);
    }

    public void Flee()
    {
        if (IsLeaving) return;
        Current = State.Flee;
        stateTimer = 1.2f;
        Vector2 away = Position - (pond.BaitInWater ? pond.BaitPosition : pond.LandingPoint);
        if (away.sqrMagnitude < 1f) away = Random.insideUnitCircle;
        velocity = away.normalized * Species.swimSpeed * 3f;
        if (pond.EngagedFish == this) pond.Release(this);
    }

    // ─── 毎フレーム（MyTownFishing から呼ばれる） ───────────────
    public void Tick(float dt)
    {
        if (Current == State.Hooked || Current == State.Gone) return;

        if (revealDelay > 0f) revealDelay -= dt;
        else if (revealTimer > 0f) revealTimer -= dt;

        switch (Current)
        {
            case State.Wander: Wander(dt); break;
            case State.Approach: Approach(dt); break;
            case State.Nibble: Nibble(dt); break;
            case State.Bite: Bite(dt); break;
            case State.Flee: FleeMove(dt); break;
        }

        Position += velocity * dt;
        if (Current != State.Flee) Position = ClampToWater(Position);

        if (velocity.sqrMagnitude > 4f)
        {
            float target = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            heading = Mathf.MoveTowardsAngle(heading, target, 220f * dt);
        }
        tailPhase += dt * (4f + velocity.magnitude * 0.08f);

        fade = Current == State.Flee ? Mathf.Clamp01(stateTimer / 1.2f) : Mathf.MoveTowards(fade, 1f, dt);
        Apply(dt);
    }

    void Wander(float dt)
    {
        if ((wanderTarget - Position).sqrMagnitude < 400f || stateTimer <= 0f)
        {
            wanderTarget = ClampToWater(Position + Random.insideUnitCircle * 350f);
            stateTimer = Random.Range(2f, 5f);
        }
        stateTimer -= dt;
        SwimToward(wanderTarget, Species.swimSpeed * 0.45f, dt);

        if (!pond.BaitInWater) return;
        float d = Vector2.Distance(pond.BaitPosition, Position);

        // すぐそばで餌を激しく動かすと、警戒心の強い魚は逃げる
        if (d < 140f && pond.BaitSpeed > SpookSpeed * 1.2f && Random.value < Species.wariness)
        {
            Flee();
            return;
        }

        if (d > SightRange) return;
        // 止まっている餌より、ゆっくり動く餌（誘い）の方が気づかれやすい。速すぎると気づかない
        float speed = pond.BaitSpeed;
        float rate = speed < 5f ? 0.25f : speed < SpookSpeed ? 0.9f : 0f;
        if (Random.value < rate * dt) TryEngage();
    }

    void TryEngage()
    {
        if (!pond.Claim(this)) return;
        Current = State.Approach;
        stateTimer = 0f;
    }

    void Approach(float dt)
    {
        if (CheckSpooked()) return;

        Vector2 toBait = pond.BaitPosition - Position;
        float d = toBait.magnitude;
        Vector2 stop = pond.BaitPosition - toBait.normalized * ContactDistance;

        // 少し進んでは止まる、を繰り返しながら慎重に近づく
        stateTimer += dt;
        float burst = 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(stateTimer * 3.2f));
        SwimToward(stop, Species.swimSpeed * 0.55f * burst, dt);

        if (d <= ContactDistance + 6f)
        {
            Current = State.Nibble;
            nibblesLeft = Random.Range(Species.minNibbles, Species.maxNibbles + 1) + pond.extraNibbles;
            stateTimer = Random.Range(0.4f, 1.0f);
            darting = false;
        }
    }

    // つつく：少し下がって間を置き、一気に餌へ突っ込む。回数を使い切ったら本アタリ
    void Nibble(float dt)
    {
        if (CheckSpooked()) return;

        Vector2 toBait = pond.BaitPosition - Position;
        Vector2 dir = toBait.sqrMagnitude > 0.01f ? toBait.normalized : Vector2.right;
        stateTimer -= dt;

        if (darting)
        {
            SwimToward(pond.BaitPosition - dir * (ContactDistance - 6f), Species.swimSpeed * 4f, dt);
            if (stateTimer <= 0f)
            {
                darting = false;
                if (nibblesLeft > 0)
                {
                    nibblesLeft--;
                    pond.OnNibble(this);
                    stateTimer = Random.Range(0.5f, 1.4f);
                }
                else
                {
                    Current = State.Bite;
                    CurrentBite = PickBite();
                    // 居食いは気づきにくい分、猶予を長く
                    float rate = CurrentBite == BiteKind.Hold ? 1.8f : CurrentBite == BiteKind.Lift ? 1.3f : 1f;
                    biteDuration = stateTimer = Species.biteWindow * pond.biteWindowScale * rate;
                    pond.OnBite(this);
                }
            }
            return;
        }

        // 下がって様子を見る
        SwimToward(pond.BaitPosition - dir * (ContactDistance + 30f), Species.swimSpeed * 0.6f, dt);
        if (stateTimer <= 0f)
        {
            darting = true;
            stateTimer = 0.12f;
        }
    }

    BiteKind PickBite()
    {
        float total = Species.suddenBite + Species.holdBite + Species.liftBite;
        if (total <= 0f) return BiteKind.Sudden;
        float r = Random.value * total;
        if ((r -= Species.suddenBite) < 0f) return BiteKind.Sudden;
        if ((r -= Species.holdBite) < 0f) return BiteKind.Hold;
        return BiteKind.Lift;
    }

    void Bite(float dt)
    {
        // 消し込みは餌をくわえて引き込み、居食いはその場でじっと、食い上げは餌をくわえたまま岸の方へ上がってくる
        Vector2 dir = (pond.BaitPosition - Position).normalized;
        float speed = CurrentBite == BiteKind.Hold ? Species.swimSpeed * 0.3f : Species.swimSpeed;
        SwimToward(pond.BaitPosition - dir * (ContactDistance - 12f), speed, dt);

        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            pond.OnBiteMissed(this);
            Flee();
        }
    }

    void FleeMove(float dt)
    {
        stateTimer -= dt;
        velocity *= Mathf.Exp(-dt * 0.8f);
        if (stateTimer <= 0f)
        {
            Current = State.Gone;
            gameObject.SetActive(false);
        }
    }

    // 近づいている最中に餌を速く動かされると逃げる
    bool CheckSpooked()
    {
        if (!pond.BaitInWater || pond.BaitSpeed > SpookSpeed)
        {
            pond.OnSpooked(this);
            Flee();
            return true;
        }
        return false;
    }

    void SwimToward(Vector2 target, float speed, float dt)
    {
        Vector2 desired = target - Position;
        float d = desired.magnitude;
        // 近づくほど減速して、行き過ぎない
        desired = d > 0.01f ? desired / d * Mathf.Min(speed, d * 4f) : Vector2.zero;
        velocity = Vector2.Lerp(velocity, desired, 1f - Mathf.Exp(-dt * 4f));
    }

    Vector2 ClampToWater(Vector2 p)
    {
        Rect w = pond.water;
        float margin = Length * 0.5f;
        p.x = Mathf.Clamp(p.x, w.xMin + margin, w.xMax - margin);
        p.y = Mathf.Clamp(p.y, w.yMin + margin, w.yMax - margin);
        return p;
    }

    // ─── 見た目 ────────────────────────────────────────────────
    void Apply(float dt)
    {
        rect.localPosition = Position;
        body.localRotation = Quaternion.Euler(0f, 0f, heading);
        tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tailPhase) * Mathf.Lerp(18f, 35f, thrash));

        bool revealed = revealDelay <= 0f && revealTimer > 0f;
        bool hooked = Current == State.Hooked;

        // 暴れている魚は水面近くに上がってくるので、影が濃く大きく見える
        float alpha = (revealed || hooked ? RevealedAlpha : BaseAlpha) * fade * (hidden ? 0f : 1f);
        alpha = Mathf.Lerp(alpha, 0.95f * fade, thrash);
        alpha *= 1f - tired * 0.25f;
        var shadowColor = new Color(0.02f, 0.04f, 0.07f, alpha);
        shadow.color = tailImage.color = shadowColor;
        ShadowAlpha = alpha;
        body.localScale = Vector3.one * (1f + thrash * 0.12f + Mathf.Sin(tailPhase * 0.5f) * thrash * 0.05f);

        float glowAlpha = 0f;
        Color glowColor = Species.color;
        if (revealed)
        {
            glowAlpha = 0.25f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f);
            // レアな魚ほど金色に光る
            if (Species.rarity <= 3f) glowColor = new Color(1f, 0.8f, 0.3f);
        }
        if (Species.glows) glowAlpha = Mathf.Max(glowAlpha, 0.18f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.3f));
        glowColor.a = glowAlpha * fade;
        glow.color = glowColor;
        GlowAlpha = glowColor.a;
        GlowColor = glowColor;

        nameTag.gameObject.SetActive(revealed && !hooked);
        LabelText = null;
        if (revealed && !hooked)
        {
            string name = FishingRecord.IsCaught(Species.id) ? Species.displayName : "？？？";
            nameTag.text = LabelText = name + "  <size=70%>" + FishSpecies.SizeClass(Species.shadowLength) + "</size>";
            LabelAlpha = Mathf.Clamp01(revealTimer) * fade;
            nameTag.color = new Color(0.9f, 0.95f, 1f, LabelAlpha);
        }
    }
}
