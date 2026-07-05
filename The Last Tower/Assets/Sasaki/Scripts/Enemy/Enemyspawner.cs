using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("参照")]
    public TowerHP towerHP;
    public Transform towerObject;   // 台座のTransform
    public Transform spawnPointL;   // 敵拠点（左）
    public Transform spawnPointR;   // 敵拠点（右）

    [Header("スポーン設定")]
    public float spawnInterval = 4f;

    [Header("敵設定")]
    public List<EnemyStats> enemyStatsList = new();

    void Start()
    {
        //Debug.Log("[EnemySpawner] Start");
        //Debug.Log($"[EnemySpawner] towerHP={towerHP}, towerObject={towerObject}, spawnPointL={spawnPointL}, spawnPointR={spawnPointR}, listCount={enemyStatsList.Count}");
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        //Debug.Log("[EnemySpawner] SpawnLoop 開始");
        while (!towerHP.IsDead)
        {
            Debug.Log("[EnemySpawner] SpawnEnemy 呼ぶ");
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
        //Debug.Log("[EnemySpawner] SpawnLoop 終了（IsDead=true）");
    }

    void SpawnEnemy()
    {
        var stats = enemyStatsList[Random.Range(0, enemyStatsList.Count)];
        //Debug.Log($"[EnemySpawner] 生成: {stats.type}");

        // 左右どちらかのスポーンポイントをランダムで選ぶ
        var spawn = Random.value < 0.5f ? spawnPointL : spawnPointR;
        var go = new GameObject($"Enemy_{stats.type}");
        go.transform.position = spawn.position;

        go.tag = "Enemy";

        EnemyBase enemy = stats.type switch
        {
            EnemyType.Melee => go.AddComponent<MeleeEnemy>(),
            EnemyType.FlyingBlock => go.AddComponent<FlyingBlockEnemy>(),
            EnemyType.FlyingBeam => go.AddComponent<FlyingBeamEnemy>(),
            _ => go.AddComponent<MeleeEnemy>()
        };

        enemy.Init(stats, towerHP, towerObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (spawnPointL != null) Gizmos.DrawWireSphere(spawnPointL.position, 0.5f);
        if (spawnPointR != null) Gizmos.DrawWireSphere(spawnPointR.position, 0.5f);
    }
}