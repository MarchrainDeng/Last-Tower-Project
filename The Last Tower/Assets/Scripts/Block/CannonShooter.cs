using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
攻击方块在可攻击状态时，寻找最近敌人并发射加农炮弹。
炮弹只在发射瞬间锁定敌人的方向。

攻撃可能状態の時、最も近い敵を探して大砲弾を発射する。
砲弾は発射時のみ敵の方向を固定する。
----------------------------------------
*/
public class CannonShooter : MonoBehaviour
{
    [Header("References")]
    // 攻击状态脚本
    // 攻撃状態スクリプト
    [SerializeField] private AttackBlockState attackState;

    // 加农炮弹Prefab
    // 大砲弾Prefab
    [SerializeField] private GameObject cannonBulletPrefab;

    // 炮弹生成位置
    // 砲弾の生成位置
    [SerializeField] private Transform firePoint;

    [Header("Shoot Settings")]
    // 发射间隔
    // 発射間隔
    [SerializeField] private float shootInterval = 1.5f;

    // 搜敌范围
    // 敵の検索範囲
    [SerializeField] private float searchRadius = 10f;

    // 敌人Layer
    // 敵のLayer
    [SerializeField] private LayerMask enemyLayer;

    // 发射计时器
    // 発射タイマー
    private float shootTimer;

    private void Awake()
    {
        if (attackState == null)
        {
            // 自动获取同一物体上的攻击状态脚本
            // 同じオブジェクトの攻撃状態スクリプトを自動取得する
            attackState = GetComponent<AttackBlockState>();
        }
    }

    private void Update()
    {
        if (attackState == null ||
            !attackState.canAttack)
        {
            shootTimer = 0f;
            return;
        }

        shootTimer += Time.deltaTime;

        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            Shoot();
        }
    }

    /// <summary>
    /// 向最近的敌人方向发射炮弹
    /// 最も近い敵の方向へ砲弾を発射する
    /// </summary>
    private void Shoot()
    {
        Transform nearestEnemy = FindNearestEnemy();

        if (nearestEnemy == null)
            return;

        if (cannonBulletPrefab == null)
        {
            Debug.LogWarning(
                "Cannon Bullet Prefab is missing. / " +
                "大砲弾Prefabが設定されていません。"
            );

            return;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        GameObject bulletObject = Instantiate(
            cannonBulletPrefab,
            spawnPosition,
            Quaternion.identity
        );

        CannonBullet cannonBullet =
            bulletObject.GetComponent<CannonBullet>();

        if (cannonBullet == null)
        {
            Debug.LogWarning(
                "CannonBullet component is missing. / " +
                "CannonBulletコンポーネントがありません。"
            );

            Destroy(bulletObject);
            return;
        }

        // 发射时只把当前目标方向交给炮弹
        // 発射時に現在の目標方向だけを砲弾へ渡す
        cannonBullet.Initialize(nearestEnemy);
    }

    /// <summary>
    /// 寻找攻击范围内最近的敌人
    /// 攻撃範囲内で最も近い敵を探す
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            searchRadius,
            enemyLayer
        );

        Transform nearestEnemy = null;
        float nearestDistanceSqr = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            // 获取敌人父物体，避免锁定到子Collider
            // 子Colliderではなく敵の親オブジェクトを取得する
            EnemyHealth enemyHealth =
                hit.GetComponentInParent<EnemyHealth>();

            Transform enemyTransform =
                enemyHealth != null
                    ? enemyHealth.transform
                    : hit.transform;

            float distanceSqr =
                ((Vector2)enemyTransform.position -
                 (Vector2)transform.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemyTransform;
            }
        }

        return nearestEnemy;
    }

    private void OnDrawGizmosSelected()
    {
        // 显示搜敌范围
        // 敵の検索範囲を表示する
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            searchRadius
        );
    }
}
