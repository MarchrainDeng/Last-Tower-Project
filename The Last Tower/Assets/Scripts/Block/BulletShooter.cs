using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
令攻击方块向范围内最近的敌人射出普通子弹。
没有敌人时不会发射。

攻撃ブロックから範囲内の最も近い敵へ通常弾を発射する。
敵が存在しない場合は発射しない。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/05

---------------------------------------
*/

public class BulletShooter : MonoBehaviour
{
    [Header("References")]

    // 子弹预制体
    // 弾のプレハブ
    public GameObject bulletPrefab;

    // 子弹生成位置，不填则使用自身位置
    // 弾の生成位置。未設定の場合は自身の位置を使用する
    public Transform firePoint;

    [Header("Shoot Settings")]

    // 攻击间隔
    // 攻撃間隔
    public float shootInterval = 1f;

    // 敌人Layer
    // 敵のLayer
    public LayerMask enemyLayer;

    // 搜敌范围
    // 敵を探す範囲
    public float searchRadius = 10f;

    // 是否可以攻击
    // 攻撃可能かどうか
    private AttackBlockState attackState;

    // 发射计时器
    // 発射タイマー
    private float shootTimer = 0f;

    private void Awake()
    {
        // 获取攻击状态脚本
        // 攻撃状態スクリプトを取得する
        attackState = GetComponent<AttackBlockState>();
    }

    private void Update()
    {
        if (attackState == null || !attackState.canAttack)
        {
            shootTimer = 0f;
            return;
        }

        shootTimer += Time.deltaTime;

        if (shootTimer < shootInterval)
            return;

        // 只有成功发射后才重置计时器
        // 発射に成功した場合のみタイマーをリセットする
        if (ShootNearestEnemy())
        {
            shootTimer = 0f;
        }
    }

    /// <summary>
    /// 朝最近的敌人发射子弹
    /// 最も近い敵に向かって弾を発射する
    /// </summary>
    private bool ShootNearestEnemy()
    {
        Transform nearestEnemy = FindNearestEnemy();

        // 没有敌人时不生成子弹
        // 敵がいない場合は弾を生成しない
        if (nearestEnemy == null)
            return false;

        if (bulletPrefab == null)
        {
            Debug.LogWarning(
                "BulletPrefab没有设置。/ BulletPrefabが設定されていません。",
                gameObject
            );

            return false;
        }

        Vector3 spawnPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        // 使用枪口当前旋转生成子弹
        // 発射口の現在の回転で弾を生成する
        Quaternion spawnRotation =
            firePoint != null
                ? firePoint.rotation
                : transform.rotation;

        GameObject bulletObject = Instantiate(
            bulletPrefab,
            spawnPosition,
            spawnRotation
        );

        Bullet bullet = bulletObject.GetComponent<Bullet>();

        if (bullet != null)
        {
            // 根据你的Bullet脚本实际接口进行修改
            // Bulletスクリプトの実際の関数に合わせて変更する
            // bullet.SetTarget(nearestEnemy);
        }

        return true;
    }

    /// <summary>
    /// 寻找范围内距离最近的敌人
    /// 範囲内で最も近い敵を検索する
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Vector2 searchCenter =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Collider2D[] enemyColliders =
            Physics2D.OverlapCircleAll(
                searchCenter,
                searchRadius,
                enemyLayer
            );

        Transform nearestEnemy = null;
        float nearestDistanceSqr = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemyColliders)
        {
            if (enemyCollider == null)
                continue;

            Transform enemyTransform =
                enemyCollider.attachedRigidbody != null
                    ? enemyCollider.attachedRigidbody.transform
                    : enemyCollider.transform;

            float distanceSqr =
                ((Vector2)enemyTransform.position - searchCenter)
                .sqrMagnitude;

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
        // 敵探索範囲を表示する
        Gizmos.color = Color.red;

        Vector3 center =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Gizmos.DrawWireSphere(center, searchRadius);
    }
}