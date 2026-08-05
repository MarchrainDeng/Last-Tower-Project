using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  MeleeEnemy  ─  地上を歩いて台座下まで移動 → 塔を殴る
// ═══════════════════════════════════════════════════════════════════
public class MeleeEnemy : EnemyBase
{
    [Header("── アニメーション ────────────────")]
    public Animator animator;
    public string attackAnimBool = "Attack";

    protected override IEnumerator BehaviorLoop()
    {
        // スポーン側に応じて台座から stopX の距離で停止
        float side = transform.position.x < towerTransform.position.x ? -1f : 1f;
        var dest = new Vector2(towerTransform.position.x + side * stats.stopX, transform.position.y);
        while (!ReachedTarget(dest, 0.4f))
        {
            MoveToward(dest, stats.moveSpeed);
            yield return null;
        }

        while (!towerHP.IsDead)
        {
            if (animator != null)
                animator.SetBool(attackAnimBool, true);

            towerHP.TakeDamage(stats.attackDamage);
            PlayAttackSE();

            yield return new WaitForSeconds(stats.attackRate);

            if (animator != null)
                animator.SetBool(attackAnimBool, false);
        }
    }
}