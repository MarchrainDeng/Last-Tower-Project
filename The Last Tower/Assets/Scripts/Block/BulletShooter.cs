using UnityEngine;

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
            return;

        shootTimer += Time.deltaTime;

        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            ShootNearestEnemy();
        }
    }

    /// <summary>
    /// 朝最近的敌人发射子弹
    /// 最も近い敵に向かって弾を発射する
    /// </summary>
    private void ShootNearestEnemy()
    {
        Debug.Log("shoot");

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
    }

    private void OnDrawGizmosSelected()
    {
        // 显示搜敌范围
        // 敵探索範囲を表示する
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}
