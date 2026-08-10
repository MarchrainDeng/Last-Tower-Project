using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
在指定X范围内随机生成Trigger物体。
生成位置及其周围指定格数内不能存在已落地方块。
Trigger被触发后，等待指定时间再次生成。

指定したX範囲内にTriggerオブジェクトをランダム生成する。
生成位置およびその周囲の指定マス数以内に、
着地済みブロックが存在しない場所のみ使用する。
Trigger発動後、指定時間経過後に再生成する。

【追加 / 追加】
Trigger発動時、両手（BossHand）に
攻撃キャンセル＋ノックバックを発動させる。
----------------------------------------
*/

public class RandomTriggerSpawner : MonoBehaviour
{
    [Header("Prefab")]

    // 要生成的Trigger预制体
    // 生成するTriggerプレハブ
    [SerializeField]
    private GameObject triggerPrefab;

    [Header("Spawn Range")]

    // X坐标生成范围
    // X座標の生成範囲
    [SerializeField]
    private float minX = -3f;

    [SerializeField]
    private float maxX = 3f;

    // 生成Y坐标
    // 生成Y座標
    [SerializeField]
    private float spawnY = 0f;

    [Header("Grid Settings")]

    // 一格的大小
    // 1マスのサイズ
    [SerializeField]
    private float gridSize = 0.5f;

    // 周围需要空出的格数
    // 周囲に空ける必要があるマス数
    [SerializeField]
    private int emptyGridRadius = 2;

    [Header("Trigger Size")]

    // Trigger正方形自身的大小
    // Trigger正方形自体のサイズ
    [SerializeField]
    private Vector2 triggerSize =
        new Vector2(0.5f, 0.5f);

    [Header("Block Check")]

    // 方块所在Layer
    // ブロックのLayer
    [SerializeField]
    private LayerMask blockLayer;

    // 随机寻找位置的最大尝试次数
    // ランダム位置探索の最大試行回数
    [SerializeField]
    private int maxSpawnAttempts = 50;

    [Header("Respawn")]

    // Trigger触发后多久重新生成
    // Trigger発動後、再生成までの時間
    [SerializeField]
    private float respawnDelay = 3f;

    [Header("Debug")]

    [SerializeField]
    private bool isSpawningEnabled;

    private GameObject currentTrigger;
    private Coroutine respawnCoroutine;

    [Header("Vertical Spawn")]

    // Trigger距离支撑物顶部的最小高度
    [SerializeField]
    private float minHeightAboveSurface = 0.5f;

    // Trigger距离支撑物顶部的最大高度
    [SerializeField]
    private float maxHeightAboveSurface = 2f;

    // 从多高的位置向下检测
    [SerializeField]
    private float raycastStartY = 20f;

    // 向下检测距离
    [SerializeField]
    private float raycastDistance = 40f;

    // 地面和方块所在Layer
    [SerializeField]
    private LayerMask surfaceLayer;

    [Header("Boss Hands")]

    // Trigger発動時に攻撃キャンセル＋ノックバックさせる両手
    [SerializeField]
    private BossHand leftHand;

    [SerializeField]
    private BossHand rightHand;

    /// <summary>
    /// 开始生成Trigger
    /// Trigger生成を開始する
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawningEnabled)
            return;

        isSpawningEnabled = true;

        TrySpawnTrigger();
    }

    /// <summary>
    /// 停止生成Trigger
    /// Trigger生成を停止する
    /// </summary>
    public void StopSpawning()
    {
        isSpawningEnabled = false;

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        if (currentTrigger != null)
        {
            Destroy(currentTrigger);
            currentTrigger = null;
        }
    }

    /// <summary>
    /// 尝试生成Trigger
    /// Trigger生成を試みる
    /// </summary>
    private void TrySpawnTrigger()
    {
        if (!isSpawningEnabled)
            return;

        if (currentTrigger != null)
            return;

        for (int i = 0;
     i < maxSpawnAttempts;
     i++)
        {
            Vector2 spawnPosition;

            // 找不到有效的支撑面时换一个位置
            if (!TryGetRandomSpawnPosition(
                out spawnPosition))
            {
                continue;
            }

            // 检查Trigger覆盖范围和周围安全范围
            if (!IsSpawnPositionValid(
                spawnPosition))
            {
                continue;
            }

            SpawnTrigger(spawnPosition);
            return;
        }

        Debug.LogWarning(
            "没有找到可以生成Trigger的位置。"
        );

        // 当前没有合法位置时，稍后再次尝试
        // 現在有効な位置がない場合、後で再試行する
        respawnCoroutine =
            StartCoroutine(
                RetrySpawnRoutine()
            );
    }

    /// <summary>
    /// 获取随机生成位置
    /// ランダム生成位置を取得する
    /// </summary>
    private bool TryGetRandomSpawnPosition(
    out Vector2 spawnPosition)
    {
        spawnPosition = Vector2.zero;

        // 随机X
        float randomX =
            Random.Range(minX, maxX);

        // 对齐到网格
        randomX =
            Mathf.Round(randomX / gridSize)
            * gridSize;

        // 从上方向下寻找地面或已落地方块
        Vector2 rayStart =
            new Vector2(
                randomX,
                raycastStartY
            );

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                rayStart,
                Vector2.down,
                raycastDistance,
                surfaceLayer
            );

        float highestSurfaceY =
            float.MinValue;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            BlockLanding blockLanding =
                hit.collider
                    .GetComponentInParent<BlockLanding>();

            // 如果是方块，只允许已落地的方块作为基准
            if (blockLanding != null)
            {
                if (!blockLanding.IsLanded)
                    continue;
            }

            float surfaceY =
                hit.collider.bounds.max.y;

            if (surfaceY > highestSurfaceY)
            {
                highestSurfaceY =
                    surfaceY;
            }
        }

        // 该X下面什么都没找到
        if (highestSurfaceY ==
            float.MinValue)
        {
            return false;
        }

        // 在支撑面上方随机高度
        float randomHeight =
            Random.Range(
                minHeightAboveSurface,
                maxHeightAboveSurface
            );

        float spawnY =
            highestSurfaceY +
            randomHeight;

        // 如果希望Y也对齐0.5格
        spawnY =
            Mathf.Round(
                spawnY / gridSize
            ) * gridSize;

        spawnPosition =
            new Vector2(
                randomX,
                spawnY
            );

        return true;
    }

    /// <summary>
    /// 判断当前位置能否生成
    /// 現在位置に生成可能か判定する
    /// </summary>
    private bool IsSpawnPositionValid(
        Vector2 position)
    {
        /*
         * Trigger自身の範囲 +
         * 周囲の安全距離。
         *
         * 例如：
         * Trigger = 0.5
         * emptyGridRadius = 2
         *
         * 周围额外检测：
         * 2 × 0.5 = 1.0
         */

        float extraDistance =
            emptyGridRadius * gridSize;

        Vector2 checkSize =
            triggerSize +
            Vector2.one *
            extraDistance * 2f;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                position,
                checkSize,
                0f,
                blockLayer
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BlockLanding block =
                hit.GetComponentInParent<BlockLanding>();

            if (block == null)
                continue;

            // 只阻止已经落地的方块
            // 着地済みブロックのみ生成を阻止する
            if (block.IsLanded)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 生成Trigger
    /// Triggerを生成する
    /// </summary>
    private void SpawnTrigger(
        Vector2 position)
    {
        currentTrigger =
            Instantiate(
                triggerPrefab,
                position,
                Quaternion.identity
            );

        RandomTriggerArea triggerArea =
            currentTrigger
                .GetComponent<RandomTriggerArea>();

        if (triggerArea != null)
        {
            triggerArea.SetSpawner(this);
        }
        else
        {
            Debug.LogWarning(
                "Trigger Prefab没有RandomTriggerArea脚本。"
            );
        }
    }

    /// <summary>
    /// Trigger被触发时调用
    /// Trigger発動時に呼び出す
    /// </summary>
    public void OnTriggerActivated(
        RandomTriggerArea trigger)
    {
        if (currentTrigger != null &&
            currentTrigger == trigger.gameObject)
        {
            currentTrigger = null;
        }

        // どちらの手をキャンセル＋ノックバックさせるか判定する
        BossHand targetHand = DetermineTargetHand(trigger.transform.position);
        if (targetHand != null)
            targetHand.CancelAndKnockback();

        if (!isSpawningEnabled)
            return;

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
        }

        respawnCoroutine =
            StartCoroutine(
                RespawnRoutine()
            );
    }

    /// <summary>
    /// キャンセル対象の手を1つ決定する
    /// - どちらかがチャージ中ならそちらを優先
    /// - 両方チャージ中なら先にチャージを始めた方
    /// - どちらもチャージ中でなければ、トリガー位置に近い方
    /// </summary>
    private BossHand DetermineTargetHand(Vector3 triggerPosition)
    {
        bool leftAlive = leftHand != null && !leftHand.IsDead;
        bool rightAlive = rightHand != null && !rightHand.IsDead;

        if (!leftAlive && !rightAlive)
            return null;
        if (leftAlive && !rightAlive)
            return leftHand;
        if (!leftAlive && rightAlive)
            return rightHand;

        bool leftCharging = leftHand.IsCharging;
        bool rightCharging = rightHand.IsCharging;

        // 片方だけチャージ中 → そちらを優先
        if (leftCharging && !rightCharging)
            return leftHand;
        if (!leftCharging && rightCharging)
            return rightHand;

        // 両方チャージ中 → 先に始めた方を優先
        if (leftCharging && rightCharging)
        {
            return leftHand.ChargeStartTime <= rightHand.ChargeStartTime
                ? leftHand
                : rightHand;
        }

        // どちらもチャージ中でない → トリガー位置に近い方
        float leftDist = Vector3.Distance(leftHand.transform.position, triggerPosition);
        float rightDist = Vector3.Distance(rightHand.transform.position, triggerPosition);
        return leftDist <= rightDist ? leftHand : rightHand;
    }

    /// <summary>
    /// 等待后重新生成
    /// 待機後に再生成する
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(
            respawnDelay
        );

        respawnCoroutine = null;

        TrySpawnTrigger();
    }

    /// <summary>
    /// 找不到合法位置时稍后重试
    /// 有効位置がない場合に後で再試行する
    /// </summary>
    private IEnumerator RetrySpawnRoutine()
    {
        yield return new WaitForSeconds(
            0.5f
        );

        respawnCoroutine = null;

        TrySpawnTrigger();
    }

    private void OnDrawGizmosSelected()
    {
        float extraDistance =
            emptyGridRadius * gridSize;

        Vector2 checkSize =
            triggerSize +
            Vector2.one *
            extraDistance * 2f;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            new Vector3(
                0f,
                spawnY,
                0f
            ),
            new Vector3(
                maxX - minX,
                checkSize.y,
                0f
            )
        );
    }
}