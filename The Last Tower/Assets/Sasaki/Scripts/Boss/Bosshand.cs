using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 儃僗偺庤1杮暘
/// BossManager偐傜嵍塃偦傟偧傟偵傾僞僢僠偟偰巊偆
///
/// 亂Inspector偱傾僒僀儞偡傞傕偺亃
/// - handData   : 庤偺僗僥乕僞僗愝掕
/// - towerHP    : 僞儚乕偺HP僐儞億乕僱儞僩
/// - towerTransform : 戜嵗偺Transform
/// - hpSlider   : 偙偺庤偺HP僗儔僀僟乕乮UI乯
/// </summary>
public class BossHand : MonoBehaviour
{
    [Header("劅劅 愝掕 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅")]
    public BossHandData handData;

    [Header("劅劅 嶲徠 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅")]
    public TowerHP towerHP;
    public Transform towerTransform;
    public UnityEngine.UI.Slider hpSlider;
    public GameObject paintOverlay;  // 儁僀儞僩墘弌梡UI乮Inspector偱傾僒僀儞乯
    public Collider2D handCollider;  // 庤偺摉偨傝敾掕乮Poke帪偺傒IsTrigger傪僆僼偵偡傞乯
    public BlockSelectionFlowManager flowManager; // Flick偱憖嶌拞僽儘僢僋傪旘偽偟偨帪偵師偺慖戰傪恑傔傞偨傔
    public Animator animator; // 傾僯儊乕僔儑儞嵞惗梡

    [Header("劅劅 SE 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅")]
    public AudioSource audioSource;
    public AudioClip pokeSE;   // 彫撍偔
    public AudioClip punchSE;  // 僷儞僠
    public AudioClip flickSE;  // 僨僐僺儞

    [Header("劅劅 僇儊儔僔僃僀僋 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅")]
    public CameraShake cameraShake;       // 僷儞僠帪偵梙傜偡
    public float cameraShakeDelay = 0.2f; // 梙傜偡傑偱偺抶墑乮昩乯

    // 劅劅劅 撪晹忬懺 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    float currentHP;
    int hitCount = 0;   // 僲僢僋僶僢僋梡旐抏夞悢

    bool isKnockbacking = false;
    bool isApproaching = false;
    bool isDead = false;

    Vector3 originPos;              // 僗億乕儞埵抲乮僲僢僋僶僢僋栠傝愭乯
    float side;                   // 嵍=-1 塃=1

    // 峌寕僼僃乕僘娗棟
    bool isHarassPhase = true;      // true=朩奞僼僃乕僘, false=峌寕僼僃乕僘
    int actionCount = 0;         // 尰僼僃乕僘偱幚峴偟偨傾僋僔儑儞悢

    public bool IsDead => isDead;

    // 奜晹僀儀儞僩
    public System.Action OnDefeated;

    // 劅劅劅 婲摦 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    void Start()
    {
        /*
        currentHP = handData.maxHP;
        originPos = transform.position;
        side = transform.position.x < towerTransform.position.x ? 1f : -1f;
        */

        //UpdateHPBar();
        // 峴摦奐巒偼BossManager偐傜StartBehavior()傪屇傫偱峴偆
    }

    private void Awake()
    {
        currentHP = handData.maxHP;
        originPos = transform.position;
        side = transform.position.x < towerTransform.position.x ? 1f : -1f;

        UpdateHPBar();
    }

    /// <summary>
    /// BossManager偐傜屇傇丅儃僗弌尰帪偵峴摦傪奐巒偡傞
    /// </summary>
    public void StartBehavior()
    {
        StartCoroutine(BehaviorLoop());
    }

    // 劅劅劅 儊僀儞儖乕僾 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    IEnumerator BehaviorLoop()
    {
        while (!isDead)
        {
            // 僎乕儉堦帪掆巭拞偼懸婡
            while (GameStateManager.IsPaused) yield return null;

            // 僲僢僋僶僢僋拞偼姰慡偵懸婡
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // 僞儚乕偵岦偐偭偰愙嬤
            yield return StartCoroutine(Approach());
            if (isDead) yield break;

            // 僲僢僋僶僢僋偑棃偨傜愙嬤傪傗傝捈偡
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // 峌寕幚峴
            yield return StartCoroutine(ExecuteAction());
            if (isDead) yield break;

            // 僲僢僋僶僢僋偑棃偨傜峌寕屻傕懸婡
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // 僼僃乕僘愗傝懼偊敾掕
            SwitchPhaseIfNeeded();
        }
    }

    // 劅劅劅 愙嬤 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    /*
    IEnumerator Approach()
    {
        float targetX = towerTransform.position.x + side * handData.approachStopX;
        var dest = new Vector3(targetX, transform.position.y, 0f);

        while (Vector3.Distance(transform.position, dest) > 0.1f)
        {
            if (isKnockbacking || isDead) yield break;
            transform.position = Vector3.MoveTowards(
                transform.position, dest, handData.moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
    */
    IEnumerator Approach()
    {
        if (towerTransform == null || handData == null)
            yield break;

        isApproaching = true;

        float targetX =
            towerTransform.position.x +
            side * handData.approachStopX;

        Vector3 destination = new Vector3(
            targetX,
            transform.position.y,
            transform.position.z
        );

        while (Mathf.Abs(transform.position.x - targetX) > 0.01f)
        {
            if (isKnockbacking || isDead)
            {
                isApproaching = false;
                yield break;
            }

            Vector3 currentPosition = transform.position;

            currentPosition.x = Mathf.MoveTowards(
                currentPosition.x,
                targetX,
                handData.moveSpeed * Time.deltaTime
            );

            transform.position = currentPosition;

            yield return null;
        }

        // 最后强制对齐目标位置，避免留下误差
        transform.position = destination;

        isApproaching = false;
    }

    // 劅劅劅 傾僋僔儑儞慖戰丒幚峴 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    IEnumerator ExecuteAction()
    {
        var pool = isHarassPhase ? handData.harassActions : handData.attackActions;
        if (pool == null || pool.Count == 0) yield break;

        var action = SelectAction(pool);
        if (action == null) yield break;

        yield return StartCoroutine(PerformAction(action));
        actionCount++;

        yield return new WaitForSeconds(action.cooldown);
    }

    IEnumerator PerformAction(BossActionData action)
    {
        switch (action.type)
        {
            case BossActionType.Punch:
                yield return StartCoroutine(ActionPunch(action));
                break;
            case BossActionType.Paint:
                yield return StartCoroutine(ActionPaint(action));
                break;
            case BossActionType.Juggling:
                yield return StartCoroutine(ActionJuggling(action));
                break;
            case BossActionType.Poke:
                yield return StartCoroutine(ActionPoke(action));
                break;
            case BossActionType.Flick:
                yield return StartCoroutine(ActionFlick(action));
                break;
        }
    }

    // 劅劅劅 奺傾僋僔儑儞幚憰 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅

    // 僷儞僠丗僞儚乕廃曈偺抧柺傪梙傜偡
    IEnumerator ActionPunch(BossActionData action)
    {
        Debug.Log($"[BossHand] Punch! 僟儊乕僕:{action.damage}");

        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);

        if (animator != null)
            animator.SetBool(handData.punchAnimTrigger, true);

        PlaySE(punchSE);

        if (cameraShake != null)
            StartCoroutine(DelayedShake());

        towerHP.TakeDamage(action.damage);

        if (towerHP.pedestalRb != null)
            yield return StartCoroutine(ShakePedestal());

        yield return new WaitForSeconds(handData.punchAnimDuration);

        if (animator != null)
            animator.SetBool(handData.punchAnimTrigger, false);
    }

    // 劅劅劅 戜嵗梙傟乮TowerHP.ShakePedestal偲摨偠巇慻傒乯 劅劅劅劅劅劅劅劅劅劅劅劅
    // 劅劅劅 僇儊儔僔僃僀僋傪抶墑偝偣偰幚峴 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    IEnumerator DelayedShake()
    {
        yield return new WaitForSeconds(cameraShakeDelay);
        if (cameraShake != null)
            cameraShake.Shake();
    }

    IEnumerator ShakePedestal()
    {
        var pedestalRb = towerHP.pedestalRb;
        Vector2 origin = pedestalRb.position;
        float timer = 0f;

        // 梙傟拞偩偗堏摦傪嫋壜
        pedestalRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        while (timer < handData.punchShakeDuration)
        {
            float envelope = Mathf.Sin(timer / handData.punchShakeDuration * Mathf.PI);
            float offset = Mathf.Sin(timer * handData.punchShakeFrequency) * handData.punchShakeAmplitude * envelope;
            pedestalRb.MovePosition(origin + new Vector2(offset, 0f));
            timer += Time.deltaTime;
            yield return null;
        }

        pedestalRb.MovePosition(origin);

        // 梙傟廔傢偭偨傜嵞傃屌掕
        pedestalRb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // 儁僀儞僩丗夋柺偵帇奅朩奞UI傪昞帵
    IEnumerator ActionPaint(BossActionData action)
    {
        Debug.Log("[BossHand] Paint!");

        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);

        if (paintOverlay != null)
            paintOverlay.SetActive(true);

        yield return new WaitForSeconds(handData.paintDuration);

        if (paintOverlay != null)
            paintOverlay.SetActive(false);
    }

    // 僕儍僌儕儞僌丗堦斣忋偺僽儘僢僋傪捦傫偱偍庤嬍偺傛偆偵梙傜偡
    IEnumerator ActionJuggling(BossActionData action)
    {
        Debug.Log("[BossHand] Juggling!");

        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);

        var topBlock = GetTopBlock();
        if (topBlock == null)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // 捦傓丗暔棟傪柍岠壔偟偰庤偵捛廬偝偣傞
        var originalBodyType = topBlock.bodyType;
        topBlock.bodyType = RigidbodyType2D.Kinematic;
        topBlock.linearVelocity = Vector2.zero;

        Vector3 grabOffset = new Vector3(0f, -handData.jugglingHoldOffsetY, 0f);

        // 偍庤嬍偺傛偆偵忋壓偵梙傜偡
        float timer = 0f;
        while (timer < handData.jugglingDuration)
        {
            timer += Time.deltaTime;
            float bounce = Mathf.Abs(Mathf.Sin(timer * handData.jugglingFrequency)) * handData.jugglingHeight;
            Vector3 handPos = transform.position + grabOffset + Vector3.up * bounce;
            topBlock.position = handPos;
            yield return null;
        }

        // 曻偡丗暔棟傪尦偵栠偡
        topBlock.bodyType = originalBodyType;

        yield return new WaitForSeconds(0.3f);
    }

    // 僞儚乕偺堦斣忋偺僽儘僢僋傪庢摼乮僞僌偱専嶕乯
    Rigidbody2D GetTopBlock()
    {
        var blocks = GameObject.FindGameObjectsWithTag(handData.towerBlockTag);
        if (blocks.Length == 0) return null;

        Rigidbody2D top = null;
        float maxY = float.MinValue;

        foreach (var block in blocks)
        {
            var rb = block.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            if (rb.position.y > maxY)
            {
                maxY = rb.position.y;
                top = rb;
            }
        }
        return top;
    }

    // 彫撍偔丗彫僟儊乕僕亄暔棟揑偵撍偒弌偟偰徴撍偝偣傞
    IEnumerator ActionPoke(BossActionData action)
    {
        Debug.Log($"[BossHand] Poke! 僟儊乕僕:{action.damage}");

        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);

        if (animator != null)
            animator.SetBool(handData.pokeAnimTrigger, true);

        PlaySE(pokeSE);

        Vector3 startPos = transform.position;
        Vector3 pokeDest = startPos + new Vector3(-side * handData.pokeDistance, 0f, 0f);

        // Poke拞偩偗IsTrigger傪僆僼偵偟偰暔棟徴撍偝偣傞
        bool originalIsTrigger = false;
        if (handCollider != null)
        {
            originalIsTrigger = handCollider.isTrigger;
            handCollider.isTrigger = false;
        }

        // 撍偒弌偡
        float timer = 0f;
        while (timer < handData.pokeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / handData.pokeDuration);
            transform.position = Vector3.Lerp(startPos, pokeDest, t);
            yield return null;
        }
        transform.position = pokeDest;

        towerHP.TakeDamage(action.damage);

        // 堷偔
        timer = 0f;
        while (timer < handData.pokeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / handData.pokeDuration);
            transform.position = Vector3.Lerp(pokeDest, startPos, t);
            yield return null;
        }
        transform.position = startPos;

        // IsTrigger傪尦偵栠偡
        if (handCollider != null)
            handCollider.isTrigger = originalIsTrigger;

        if (animator != null)
            animator.SetBool(handData.pokeAnimTrigger, false);
    }

    // 僨僐僺儞丗崅僟儊乕僕亄堦斣忋偺僽儘僢僋傪暔棟揑偵悂偒旘偽偡
    IEnumerator ActionFlick(BossActionData action)
    {
        Debug.Log($"[BossHand] Flick! 僟儊乕僕:{action.damage}");

        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);

        if (animator != null)
            animator.SetBool(handData.flickAnimTrigger, true);

        PlaySE(flickSE);

        towerHP.TakeDamage(action.damage);

        var topBlock = GetTopBlock();
        if (topBlock != null)
        {
            // 憖嶌拞偺僽儘僢僋偐偳偆偐傪敾掕
            var moveController = topBlock.GetComponent<BlockMoveController>();
            if (moveController != null)
            {
                // 憖嶌尃傪扗偆
                moveController.enabled = false;

                // BlockLanding偑擇廳偵OnCurrentBlockLanded()傪屇偽側偄傛偆柍岠壔
                var landing = topBlock.GetComponent<BlockLanding>();
                if (landing != null)
                    landing.enabled = false;

                // 師偺僽儘僢僋慖戰偵恑傔傞
                if (flowManager != null)
                    flowManager.OnCurrentBlockLanded();
                else
                    Debug.LogWarning("[BossHand] flowManager 偑枹傾僒僀儞偱偡");
            }

            // 忋曽岦亄墶曽岦乮side媡岦偒乯偵僀儞僷儖僗傪壛偊傞
            Vector2 flickDir = new Vector2(-side * handData.flickForceX, handData.flickForceY);
            topBlock.linearVelocity = flickDir;   // 幙検偵埶懚偟側偄堦掕懍搙
            topBlock.angularVelocity = -side * handData.flickTorque;
        }

        yield return new WaitForSeconds(handData.flickAnimDuration);

        if (animator != null)
            animator.SetBool(handData.flickAnimTrigger, false);
    }

    // 劅劅劅 僼僃乕僘愗傝懼偊 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    void SwitchPhaseIfNeeded()
    {
        switch (handData.phaseOrder)
        {
            case BossPhaseOrder.HarassFirst:
                // 朩奞仺峌寕偺弴偱1夞偢偮
                if (isHarassPhase && actionCount >= 1) { isHarassPhase = false; actionCount = 0; }
                else if (!isHarassPhase && actionCount >= 1) { isHarassPhase = true; actionCount = 0; }
                break;

            case BossPhaseOrder.AttackFirst:
                if (!isHarassPhase && actionCount >= 1) { isHarassPhase = true; actionCount = 0; }
                else if (isHarassPhase && actionCount >= 1) { isHarassPhase = false; actionCount = 0; }
                break;

            case BossPhaseOrder.Alternate:
                isHarassPhase = !isHarassPhase;
                actionCount = 0;
                break;

            case BossPhaseOrder.Random:
                isHarassPhase = Random.value < 0.5f;
                actionCount = 0;
                break;
        }
    }

    // 劅劅劅 傾僋僔儑儞廳傒慖戰 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    BossActionData SelectAction(List<BossActionData> pool)
    {
        float total = 0f;
        foreach (var a in pool) total += a.weight;
        float rand = Random.Range(0f, total);
        float cum = 0f;
        foreach (var a in pool)
        {
            cum += a.weight;
            if (rand <= cum) return a;
        }
        return pool[^1];
    }

    // 劅劅劅 僟儊乕僕庴晅 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    public void TakeDamage(int amount) => TakeDamage((float)amount);

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);

        Debug.Log($"[BossHand] {gameObject.name} HP : {currentHP}");
        UpdateHPBar();

        // 僲僢僋僶僢僋拞偼僇僂儞僩偟側偄
        Debug.Log($"[BossHand] isKnockbacking={isKnockbacking} hitCount={hitCount} threshold={handData.knockbackHitThreshold}");
        if (!isKnockbacking)
        {
            hitCount++;
            Debug.Log($"[BossHand] 旐抏夞悢 hitCount={hitCount}");
            if (hitCount >= handData.knockbackHitThreshold)
            {
                Debug.Log("[BossHand] 僲僢僋僶僢僋敪摦両");
                hitCount = 0;
                isKnockbacking = true;  // 僐儖乕僠儞奐巒慜偵棫偰傞
                StartCoroutine(Knockback());
            }
        }
        else
        {
            Debug.Log("[BossHand] 僲僢僋僶僢僋拞偺偨傔僗僉僢僾");
        }

        if (currentHP <= 0f)
            Die();
    }

    // 劅劅劅 僲僢僋僶僢僋乮嶳側傝屖乯 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    IEnumerator Knockback()
    {
        Debug.Log($"[BossHand] Knockback() 奐巒 isKnockbacking={isKnockbacking}");
        Vector3 startPos = transform.position;
        Vector3 knockDest = startPos + new Vector3(-side * handData.knockbackDistance, 0f, 0f);

        // 屻戅乮嶳側傝丒屌掕帪娫乯
        float timer = 0f;
        float duration = handData.knockbackDuration;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float x = Mathf.Lerp(startPos.x, knockDest.x, t);
            float arcY = Mathf.Sin(t * Mathf.PI) * handData.knockbackArcHeight;
            float y = Mathf.Lerp(startPos.y, knockDest.y, t) + arcY;
            transform.position = new Vector3(x, y, startPos.z);
            yield return null;
        }
        transform.position = knockDest;

        // 彮偟懸偭偰偐傜栠傞
        yield return new WaitForSeconds(0.5f);

        // 栠傞乮捈慄丒屌掕帪娫乯
        // 栠傝愭偼 approachStopX 偺埵抲乮峌寕帪偺掕埵抲乯
        float returnX = towerTransform.position.x + side * handData.approachStopX;
        Vector3 returnDest = new Vector3(returnX, originPos.y, originPos.z);
        Vector3 returnStart = transform.position;
        timer = 0f;
        duration = handData.returnDuration;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            transform.position = Vector3.Lerp(returnStart, returnDest, t);
            yield return null;
        }
        transform.position = returnDest;

        hitCount = 0;  // 廔椆帪偵儕僙僢僩
        isKnockbacking = false;
    }

    // 劅劅劅 巰朣 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    void Die()
    {
        isDead = true;
        OnDefeated?.Invoke();
        Debug.Log($"[BossHand] {gameObject.name} 寕攋両");
        gameObject.SetActive(false);
    }

    // 劅劅劅 HP僶乕峏怴 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    void UpdateHPBar()
    {
        if (hpSlider == null) return;
        hpSlider.value = currentHP / handData.maxHP;
    }

    // 劅劅劅 SE嵞惗 劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅劅
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}