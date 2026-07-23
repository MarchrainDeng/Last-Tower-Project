using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  FlyingBlockEnemy  ─  飛行接近 → 邪魔ブロックを上に置く
//  最初からブロックを持っている状態で登場する
// ═══════════════════════════════════════════════════════════════════
public class FlyingBlockEnemy : EnemyBase
{
    [Header("落とすブロックのPrefab")]
    public GameObject blockPrefab;

    float flightY;
    float swayTimer = 0f; // 揺れを途切れさせないための通し時間
    GameObject heldBlock; // 持っているブロック（見た目）

    protected override void OnInit()
    {
        flightY = Random.Range(stats.flightYMin, stats.flightYMax);
        transform.position = new Vector3(transform.position.x, flightY, 0f);

        // スポーンと同時に持っているブロックを生成（子として追従）
        if (blockPrefab != null)
        {
            heldBlock = Instantiate(blockPrefab, transform.position + Vector3.down * 0.5f, Quaternion.identity);
            heldBlock.transform.SetParent(transform);

            // 持っている間は物理を無効化（敵の動きにそのまま追従させる）
            var rb = heldBlock.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }

            var col = heldBlock.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false; // 持っている間は他と衝突しない
        }
    }

    protected override IEnumerator BehaviorLoop()
    {
        var flyTarget = new Vector2(
            towerTransform.position.x + Random.Range(-1f, 1f),
            flightY
        );

        // ① スポーン直後からブロックを持って重そうに揺れながら接近
        yield return StartCoroutine(MoveWhileCarrying(flyTarget));

        // ② 到達直前：最高高度まで一瞬飛び上がってから少し下がる
        yield return StartCoroutine(RiseToPeakThenSettle(flyTarget));

        // ③ その場でしばらく持ち続ける（揺れは継続）
        yield return StartCoroutine(HoldWhileCarrying(flyTarget));

        // ④ ブロックを落とす → 持ってる感の演出終了
        DropBlock();
        towerHP.TakeDamage(stats.attackDamage);

        // ⑤ 通常の離脱移動（揺れなし）
        var exit = new Vector2(transform.position.x + 20f, flightY);
        float timer = 0f;
        while (timer < 3f)
        {
            MoveToward(exit, stats.moveSpeed * stats.exitSpeedMultiplier);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // ─── スポーン〜目的地まで、重そうに揺れながら移動 ──────────────
    IEnumerator MoveWhileCarrying(Vector2 target)
    {
        while (!ReachedTarget(target, 0.4f))
        {
            swayTimer += Time.deltaTime;
            float sway = Mathf.Sin(swayTimer * stats.blockCarrySwaySpeed) * stats.blockCarrySwayAmount;

            // 目的地方向へ進みつつ、Yに揺れを足す
            Vector2 baseMove = Vector2.MoveTowards(
                transform.position, target, stats.moveSpeed * Time.deltaTime);
            transform.position = new Vector3(baseMove.x, baseMove.y + sway, transform.position.z);

            yield return null;
        }
    }

    // ─── 最高高度へ一瞬上がってから少し下がる（重いブロックを持ち上げた反動） ──
    IEnumerator RiseToPeakThenSettle(Vector2 target)
    {
        float peakY = flightY + stats.blockCarryPeakHeight;
        float settleY = flightY + stats.blockCarrySettleHeight;

        // 上昇
        float timer = 0f;
        Vector2 startPos = transform.position;
        while (timer < stats.blockCarryRiseDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / stats.blockCarryRiseDuration);
            float y = Mathf.Lerp(startPos.y, peakY, t);
            transform.position = new Vector3(target.x, y, transform.position.z);
            yield return null;
        }

        // 少し下がって落ち着く
        timer = 0f;
        while (timer < stats.blockCarrySettleDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / stats.blockCarrySettleDuration);
            float y = Mathf.Lerp(peakY, settleY, t);
            transform.position = new Vector3(target.x, y, transform.position.z);
            yield return null;
        }
    }

    // ─── ブロックを持っている間、ゆっくり重そうに上下し続ける ──────
    IEnumerator HoldWhileCarrying(Vector2 target)
    {
        float baseY = flightY + stats.blockCarrySettleHeight;
        float timer = 0f;

        while (timer < stats.blockCarryHoldDuration)
        {
            swayTimer += Time.deltaTime;
            float bounce = Mathf.Sin(swayTimer * stats.blockCarrySwaySpeed) * stats.blockCarrySwayAmount;
            transform.position = new Vector3(target.x, baseY + bounce, transform.position.z);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    void DropBlock()
    {
        if (heldBlock == null)
        {
            Debug.LogWarning("[FlyingBlockEnemy] heldBlock がありません");
            return;
        }

        // 親から切り離す
        heldBlock.transform.SetParent(null);

        // 物理を有効化して落下させる
        var rb = heldBlock.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        var col = heldBlock.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        heldBlock = null;
    }
}