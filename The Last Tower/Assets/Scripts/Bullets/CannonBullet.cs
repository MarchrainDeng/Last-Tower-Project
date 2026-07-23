using System.Collections.Generic;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
加农炮弹在生成时锁定目标方向，之后沿直线飞行，不再追踪敌人。
命中敌人后发生爆炸，对爆炸范围内的所有敌人造成伤害。

大砲弾は生成時に目標方向を固定し、
その後は敵を追尾せず直線移動する。
敵に命中すると爆発し、爆発範囲内のすべての敵にダメージを与える。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class CannonBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    // 炮弹移动速度
    // 砲弾の移動速度
    [SerializeField] private float moveSpeed = 5f;

    // 爆炸伤害
    // 爆発ダメージ
    [SerializeField] private int damage = 20;

    // 炮弹最大存在时间
    // 砲弾の最大生存時間
    [SerializeField] private float lifeTime = 5f;

    [Header("Explosion Settings")]
    // 爆炸半径
    // 爆発半径
    [SerializeField] private float explosionRadius = 1.5f;

    // 敌人的Layer
    // 敵のLayer
    [SerializeField] private LayerMask enemyLayer;

    // 是否让炮弹朝向飞行方向
    // 砲弾を飛行方向へ向けるか
    [SerializeField] private bool rotateToDirection = true;

    // 飞行方向
    // 飛行方向
    private Vector2 moveDirection;

    // 是否已经初始化
    // 初期化済みか
    private bool isInitialized;

    // 是否已经爆炸
    // すでに爆発したか
    private bool hasExploded;

    private void Start()
    {
        // 防止没有命中时永久存在
        // 命中しなかった場合に永久に残らないようにする
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!isInitialized || hasExploded)
            return;

        // 沿生成时确定的方向直线飞行
        // 生成時に決定した方向へ直線移動する
        transform.position +=
            (Vector3)moveDirection *
            moveSpeed *
            Time.deltaTime;
    }

    /// <summary>
    /// 根据目标设置初始飞行方向
    /// 目標から初期飛行方向を設定する
    /// </summary>
    public void Initialize(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning(
                "Cannon bullet target is missing. / " +
                "大砲弾の目標が設定されていません。"
            );

            Destroy(gameObject);
            return;
        }

        // 只在生成时计算一次方向
        // 生成時に一度だけ方向を計算する
        moveDirection =
            ((Vector2)target.position -
             (Vector2)transform.position).normalized;

        isInitialized = true;

        if (rotateToDirection)
        {
            RotateBullet();
        }
    }

    /// <summary>
    /// 让炮弹朝向飞行方向
    /// 砲弾を飛行方向へ向ける
    /// </summary>
    private void RotateBullet()
    {
        float angle = Mathf.Atan2(
            moveDirection.y,
            moveDirection.x
        ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded)
            return;

        // 只有直接命中敌人才爆炸
        // 敵に直接命中した場合のみ爆発する
        if (!other.CompareTag("Enemy"))
            return;

        Explode();
    }

    /// <summary>
    /// 爆炸并对范围内所有敌人造成伤害
    /// 爆発し、範囲内のすべての敵へダメージを与える
    /// </summary>
    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            explosionRadius,
            enemyLayer
        );

        // 防止同一个敌人的多个Collider导致重复受伤
        // 同じ敵の複数Colliderによる重複ダメージを防ぐ
        HashSet<EnemyHealth> damagedEnemies =
            new HashSet<EnemyHealth>();

        // 防止同一个Boss部位被重复伤害
        // 同じBoss部位への重複ダメージを防ぐ
        HashSet<BossHand> damagedBossHands =
            new HashSet<BossHand>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyHealth health =
                hit.GetComponentInParent<EnemyHealth>();

            if (health != null &&
                damagedEnemies.Add(health))
            {
                // 对普通敌人造成范围伤害
                // 通常の敵に範囲ダメージを与える
                health.TakeDamage(damage);
            }

            BossHand bossHand =
                hit.GetComponentInParent<BossHand>();

            if (bossHand != null &&
                damagedBossHands.Add(bossHand))
            {
                // 对Boss手部造成范围伤害
                // Bossの手に範囲ダメージを与える
                bossHand.TakeDamage(damage);
            }
        }

        // TODO：在这里生成爆炸特效和音效
        // TODO：ここで爆発エフェクトと効果音を生成する

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // 显示爆炸范围
        // 爆発範囲を表示する
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );
    }
}
