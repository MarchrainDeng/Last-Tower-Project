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
    public int damage = 10;
    public float moveSpeed = 5f;

    private Transform target;

    void Start()
    {
        FindNearestEnemy();
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float nearestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }

        target = nearestEnemy;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Debug.Log("bullet enter enemy");

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
