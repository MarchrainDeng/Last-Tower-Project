using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  FlyingBlockEnemy  ─  飛行接近 → 邪魔ブロックを上に置く
// ═══════════════════════════════════════════════════════════════════
public class FlyingBlockEnemy : EnemyBase
{
    [Header("落とすブロックのPrefab")]
    public GameObject blockPrefab;

    float flightY;

    protected override void OnInit()
    {
        flightY = Random.Range(stats.flightYMin, stats.flightYMax);
        transform.position = new Vector3(transform.position.x, flightY, 0f);
    }

    protected override IEnumerator BehaviorLoop()
    {
        var flyTarget = new Vector2(
            towerTransform.position.x + Random.Range(-1f, 1f),
            flightY
        );
        while (!ReachedTarget(flyTarget, 0.4f))
        {
            MoveToward(flyTarget, stats.moveSpeed);
            yield return null;
        }

        DropBlock();
        towerHP.TakeDamage(stats.attackDamage);

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

    void DropBlock()
    {
        if (blockPrefab == null)
        {
            Debug.LogWarning("[FlyingBlockEnemy] blockPrefab が設定されていません");
            return;
        }
        Instantiate(blockPrefab, transform.position + Vector3.down * 0.5f, Quaternion.identity);
    }
}