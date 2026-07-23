using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
处理普通子弹的逻辑与事件

普通弾丸に関するイベントを処理する

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/08

---------------------------------------
*/

public class Bullet : MonoBehaviour
{
    [Header("Bullet Setting")]
    // 子弹伤害
    // 弾のダメージ
    public int damage = 10;

    // 子弹移动速度
    // 弾の移動速度
    public float moveSpeed = 5f;

    // 当前追踪目标
    // 現在追跡中の目標
    private Transform target;

    // 最后一次有效的飞行方向
    // 最後に有効だった飛行方向
    private Vector3 lastDirection = Vector3.right;

    private void Start()
    {
        FindNearestEnemy();
    }

    private void Update()
    {
        if (target != null)
        {
            // 目标存在时，持续计算追踪方向
            // 目標が存在する間、追跡方向を更新する
            lastDirection =
                (target.position - transform.position).normalized;
        }

        // 目标死亡后，继续沿最后方向直线飞行
        // 目標が死亡した後も、最後の方向へ直進する
        transform.position +=
            lastDirection *
            moveSpeed *
            Time.deltaTime;
    }

    /// <summary>
    /// 寻找距离当前子弹最近的敌人
    /// 現在の弾から最も近い敵を探す
    /// </summary>
    private void FindNearestEnemy()
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        float nearestDistanceSqr = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distanceSqr =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemy.transform;
            }
        }

        target = nearestEnemy;

        // 初次找到目标时，立即保存初始飞行方向
        // 最初に目標を見つけた時、初期飛行方向を保存する
        if (target != null)
        {
            lastDirection =
                (target.position - transform.position).normalized;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        EnemyHealth health = other.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        BossHand bossHand = other.GetComponent<BossHand>();
        if (bossHand != null)
        {
            bossHand.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
    }

}
