using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ボスの手1本分
/// BossManagerから左右それぞれにアタッチして使う
///
/// 【Inspectorでアサインするもの】
/// - handData   : 手のステータス設定
/// - towerHP    : タワーのHPコンポーネント
/// - towerTransform : 台座のTransform
/// - hpSlider   : この手のHPスライダー（UI）
/// </summary>
public class BossHand : MonoBehaviour
{
    [Header("── 設定 ────────────────────────")]
    public BossHandData handData;

    [Header("── 参照 ────────────────────────")]
    public TowerHP towerHP;
    public Transform towerTransform;
    public UnityEngine.UI.Slider hpSlider;
    public GameObject paintOverlay;  // ペイント演出用UI（Inspectorでアサイン）

    // ─── 内部状態 ─────────────────────────────────────────────────
    float currentHP;
    float accumulatedDamage = 0f;   // ノックバック用累積ダメージ

    bool isKnockbacking = false;
    bool isApproaching = false;
    bool isDead = false;

    Vector3 originPos;              // スポーン位置（ノックバック戻り先）
    float side;                   // 左=-1 右=1

    // 攻撃フェーズ管理
    bool isHarassPhase = true;      // true=妨害フェーズ, false=攻撃フェーズ
    int actionCount = 0;         // 現フェーズで実行したアクション数

    public bool IsDead => isDead;

    // 外部イベント
    public System.Action OnDefeated;

    // ─── 起動 ─────────────────────────────────────────────────────
    void Start()
    {
        currentHP = handData.maxHP;
        originPos = transform.position;
        side = transform.position.x < towerTransform.position.x ? 1f : -1f;

        UpdateHPBar();
        // 行動開始はBossManagerからStartBehavior()を呼んで行う
    }

    /// <summary>
    /// BossManagerから呼ぶ。ボス出現時に行動を開始する
    /// </summary>
    public void StartBehavior()
    {
        StartCoroutine(BehaviorLoop());
    }

    // ─── メインループ ─────────────────────────────────────────────
    IEnumerator BehaviorLoop()
    {
        while (!isDead)
        {
            // ノックバック中は完全に待機
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // タワーに向かって接近
            yield return StartCoroutine(Approach());
            if (isDead) yield break;

            // ノックバックが来たら接近をやり直す
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // 攻撃実行
            yield return StartCoroutine(ExecuteAction());
            if (isDead) yield break;

            // ノックバックが来たら攻撃後も待機
            while (isKnockbacking) yield return null;
            if (isDead) yield break;

            // フェーズ切り替え判定
            SwitchPhaseIfNeeded();
        }
    }

    // ─── 接近 ─────────────────────────────────────────────────────
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

    // ─── アクション選択・実行 ─────────────────────────────────────
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

    // ─── 各アクション実装 ─────────────────────────────────────────

    // パンチ：タワー周辺の地面を揺らす
    IEnumerator ActionPunch(BossActionData action)
    {
        Debug.Log($"[BossHand] Punch! ダメージ:{action.damage}");
        towerHP.TakeDamage(action.damage);

        if (towerHP.pedestalRb != null)
            yield return StartCoroutine(ShakePedestal());

        yield return new WaitForSeconds(0.5f);
    }

    // ─── 台座揺れ（TowerHP.ShakePedestalと同じ仕組み） ────────────
    IEnumerator ShakePedestal()
    {
        var pedestalRb = towerHP.pedestalRb;
        Vector2 origin = pedestalRb.position;
        float timer = 0f;

        // 揺れ中だけ移動を許可
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

        // 揺れ終わったら再び固定
        pedestalRb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // ペイント：画面に視界妨害UIを表示
    IEnumerator ActionPaint(BossActionData action)
    {
        Debug.Log("[BossHand] Paint!");

        if (paintOverlay != null)
            paintOverlay.SetActive(true);

        yield return new WaitForSeconds(handData.paintDuration);

        if (paintOverlay != null)
            paintOverlay.SetActive(false);
    }

    // ジャグリング：一番上のブロックを掴んでお手玉のように揺らす
    IEnumerator ActionJuggling(BossActionData action)
    {
        Debug.Log("[BossHand] Juggling!");

        var topBlock = GetTopBlock();
        if (topBlock == null)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // 掴む：物理を無効化して手に追従させる
        var originalBodyType = topBlock.bodyType;
        topBlock.bodyType = RigidbodyType2D.Kinematic;
        topBlock.linearVelocity = Vector2.zero;

        Vector3 grabOffset = new Vector3(0f, -handData.jugglingHoldOffsetY, 0f);

        // お手玉のように上下に揺らす
        float timer = 0f;
        while (timer < handData.jugglingDuration)
        {
            timer += Time.deltaTime;
            float bounce = Mathf.Abs(Mathf.Sin(timer * handData.jugglingFrequency)) * handData.jugglingHeight;
            Vector3 handPos = transform.position + grabOffset + Vector3.up * bounce;
            topBlock.position = handPos;
            yield return null;
        }

        // 放す：物理を元に戻す
        topBlock.bodyType = originalBodyType;

        yield return new WaitForSeconds(0.3f);
    }

    // タワーの一番上のブロックを取得（タグで検索）
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

    // 小突く：小ダメージ＋物理的に突き出して衝突させる
    IEnumerator ActionPoke(BossActionData action)
    {
        Debug.Log($"[BossHand] Poke! ダメージ:{action.damage}");

        Vector3 startPos = transform.position;
        Vector3 pokeDest = startPos + new Vector3(-side * handData.pokeDistance, 0f, 0f);

        // 突き出す
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

        // 引く
        timer = 0f;
        while (timer < handData.pokeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / handData.pokeDuration);
            transform.position = Vector3.Lerp(pokeDest, startPos, t);
            yield return null;
        }
        transform.position = startPos;
    }

    // デコピン：高ダメージ＋一番上のブロックを物理的に吹き飛ばす
    IEnumerator ActionFlick(BossActionData action)
    {
        Debug.Log($"[BossHand] Flick! ダメージ:{action.damage}");
        towerHP.TakeDamage(action.damage);

        var topBlock = GetTopBlock();
        if (topBlock != null)
        {
            // 上方向＋横方向（side逆向き）にインパルスを加える
            Vector2 flickDir = new Vector2(-side * handData.flickForceX, handData.flickForceY);
            topBlock.AddForce(flickDir, ForceMode2D.Impulse);
            topBlock.AddTorque(-side * handData.flickTorque, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ─── フェーズ切り替え ─────────────────────────────────────────
    void SwitchPhaseIfNeeded()
    {
        switch (handData.phaseOrder)
        {
            case BossPhaseOrder.HarassFirst:
                // 妨害→攻撃の順で1回ずつ
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

    // ─── アクション重み選択 ───────────────────────────────────────
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

    // ─── ダメージ受付 ────────────────────────────────────────────
    public void TakeDamage(int amount) => TakeDamage((float)amount);

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);

        Debug.Log($"[BossHand] {gameObject.name} HP : {currentHP}");
        UpdateHPBar();

        // ノックバック中は累積しない
        Debug.Log($"[BossHand] isKnockbacking={isKnockbacking} accumulated={accumulatedDamage} threshold={handData.knockbackThreshold}");
        if (!isKnockbacking)
        {
            accumulatedDamage += amount;
            Debug.Log($"[BossHand] 累積後 accumulated={accumulatedDamage}");
            if (accumulatedDamage >= handData.knockbackThreshold)
            {
                Debug.Log("[BossHand] ノックバック発動！");
                accumulatedDamage = 0f;
                isKnockbacking = true;  // コルーチン開始前に立てる
                StartCoroutine(Knockback());
            }
        }
        else
        {
            Debug.Log("[BossHand] ノックバック中のためスキップ");
        }

        if (currentHP <= 0f)
            Die();
    }

    // ─── ノックバック（山なり弧） ────────────────────────────────
    IEnumerator Knockback()
    {
        Debug.Log($"[BossHand] Knockback() 開始 isKnockbacking={isKnockbacking}");
        Vector3 startPos = transform.position;
        Vector3 knockDest = startPos + new Vector3(-side * handData.knockbackDistance, 0f, 0f);

        // 後退（山なり・固定時間）
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

        // 少し待ってから戻る
        yield return new WaitForSeconds(0.5f);

        // 戻る（直線・固定時間）
        // 戻り先は approachStopX の位置（攻撃時の定位置）
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

        accumulatedDamage = 0f;  // 終了時にリセット
        isKnockbacking = false;
    }

    // ─── 死亡 ─────────────────────────────────────────────────────
    void Die()
    {
        isDead = true;
        OnDefeated?.Invoke();
        Debug.Log($"[BossHand] {gameObject.name} 撃破！");
        gameObject.SetActive(false);
    }

    // ─── HPバー更新 ───────────────────────────────────────────────
    void UpdateHPBar()
    {
        if (hpSlider == null) return;
        hpSlider.value = currentHP / handData.maxHP;
    }
}