using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("── 参照 ────────────────────────")]
    public TowerHP towerHP;
    public Transform towerObject;   // 台座のTransform
    public Transform spawnPointL;   // 敵拠点（左）
    public Transform spawnPointR;   // 敵拠点（右）

    [Header("── 敵Prefab ───────────────────")]
    public List<EnemySpawnEntry> enemyEntries = new();

    [Header("── スポーン間隔 ────────────────")]
    public float initialInterval = 4f;   // 最初のスポーン間隔（秒）
    public float minInterval = 1f;   // 最小スポーン間隔（秒）
    public float intervalDecreaseRate = 0.1f; // 1秒ごとに縮まる量（秒）

    // ─── 内部 ─────────────────────────────────────────────────────
    float elapsedTime = 0f;
    float currentInterval => Mathf.Max(minInterval, initialInterval - elapsedTime * intervalDecreaseRate);

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    void Update()
    {
        if (!towerHP.IsDead)
            elapsedTime += Time.deltaTime;
    }

    IEnumerator SpawnLoop()
    {
        while (!towerHP.IsDead)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(currentInterval);
        }
    }

    void SpawnEnemy()
    {
        var entry = SelectEntry();
        if (entry == null || entry.prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Prefabが設定されていません");
            return;
        }

        // 左右ランダムでスポーン
        var spawnTr = Random.value < 0.5f ? spawnPointL : spawnPointR;
        var go = Instantiate(entry.prefab, spawnTr.position, Quaternion.identity);

        // デバッグ：アタッチされているコンポーネントを全表示
        foreach (var mb in go.GetComponents<MonoBehaviour>())
            Debug.Log($"[EnemySpawner] コンポーネント: {mb.GetType().Name}");

        // EnemyBaseを取得してInit
        EnemyBase enemy = null;
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb is EnemyBase eb) { enemy = eb; break; }
        }
        if (enemy == null)
        {
            Debug.LogWarning($"[EnemySpawner] {entry.prefab.name} に EnemyBase がありません");
            return;
        }

        var stats = go.GetComponent<EnemyStatsHolder>();
        if (stats == null)
        {
            Debug.LogWarning($"[EnemySpawner] {entry.prefab.name} に EnemyStatsHolder がありません");
            return;
        }

        enemy.Init(stats.stats, towerHP, towerObject);
    }

    // 重みに基づいてランダム選択
    EnemySpawnEntry SelectEntry()
    {
        float total = 0f;
        foreach (var e in enemyEntries) total += e.spawnWeight;

        float rand = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var e in enemyEntries)
        {
            cumulative += e.spawnWeight;
            if (rand <= cumulative) return e;
        }
        return enemyEntries.Count > 0 ? enemyEntries[^1] : null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (spawnPointL != null) Gizmos.DrawWireSphere(spawnPointL.position, 0.5f);
        if (spawnPointR != null) Gizmos.DrawWireSphere(spawnPointR.position, 0.5f);
    }
}