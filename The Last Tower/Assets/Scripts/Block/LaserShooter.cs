using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
镭射炮每隔一定时间搜索最近的敌人，
向目标持续发射镭射并造成持续伤害。

如果镭射持续期间敌人死亡，
立即停止镭射特效并进入冷却时间。

レーザー砲は一定時間ごとに最も近い敵を探し、
対象へレーザーを照射して継続ダメージを与える。

レーザー照射中に敵が死亡した場合、
直ちにエフェクトを停止してクールダウンへ移行する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

----------------------------------------
*/

public class LaserShooter : MonoBehaviour
{
    [Header("References")]

    // 镭射发射位置
    // レーザーの発射位置
    [SerializeField]
    private Transform firePoint;

    // 镭射视觉特效
    // レーザーの視覚エフェクト
    [SerializeField]
    private LineRenderer laserLine;

    // 攻击方块状态
    // 攻撃ブロックの状態
    [SerializeField]
    private AttackBlockState attackState;

    [Header("Target Settings")]

    // 敌人图层
    // 敵のレイヤー
    [SerializeField]
    private LayerMask enemyLayer;

    // 搜索敌人的范围
    // 敵を検索する範囲
    [SerializeField]
    private float searchRadius = 10f;

    [Header("Laser Settings")]

    // 镭射持续时间
    // レーザーの照射時間
    [SerializeField]
    private float laserDuration = 2f;

    // 每秒伤害
    // 1秒あたりのダメージ
    [SerializeField]
    private float damagePerSecond = 20f;

    // 发射结束后的冷却时间
    // 照射終了後のクールダウン時間
    [SerializeField]
    private float cooldown = 3f;

    // 镭射目标位置的偏移
    // レーザー目標位置のオフセット
    [SerializeField]
    private Vector3 targetOffset = Vector3.zero;

    [Header("Debug")]

    // 当前是否正在发射
    // 現在レーザーを照射中か
    [SerializeField]
    private bool isFiring;

    // 当前是否处于冷却
    // 現在クールダウン中か
    [SerializeField]
    private bool isCoolingDown;

    // 当前目标
    // 現在の対象
    private Transform currentTarget;

    // 当前目标的生命脚本
    // 現在の対象のHPスクリプト
    private EnemyHealth currentEnemyHealth;

    // 镭射协程
    // レーザーコルーチン
    private Coroutine laserCoroutine;

    private void Awake()
    {
        // 未手动设置时自动获取攻击状态
        // 手動設定されていない場合は自動取得する
        if (attackState == null)
        {
            attackState = GetComponent<AttackBlockState>();
        }

        // 游戏开始时隐藏镭射
        // ゲーム開始時にレーザーを非表示にする
        SetLaserVisible(false);
    }

    private void Update()
    {
        // 正在发射或处于冷却时，不再次搜索
        // 照射中またはクールダウン中は再検索しない
        if (isFiring || isCoolingDown)
            return;

        // 方块未通电时不能攻击
        // ブロックが通電していない場合は攻撃できない
        if (attackState == null || !attackState.canAttack)
            return;

        Transform nearestEnemy = FindNearestEnemy();

        // 没有敌人时继续等待
        // 敵がいない場合は待機を続ける
        if (nearestEnemy == null)
            return;

        StartLaser(nearestEnemy);
    }

    /// <summary>
    /// 开始向指定敌人发射镭射
    /// 指定した敵へのレーザー照射を開始する
    /// </summary>
    private void StartLaser(Transform target)
    {
        if (target == null)
            return;

        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
        }

        laserCoroutine = StartCoroutine(
            LaserRoutine(target)
        );
    }

    /// <summary>
    /// 镭射发射流程
    /// レーザー照射処理
    /// </summary>
    private IEnumerator LaserRoutine(Transform target)
    {
        isFiring = true;

        currentTarget = target;

        // 优先从敌人的父物体获取生命脚本
        // 敵の親オブジェクトからHPスクリプトを優先取得する
        currentEnemyHealth =
            target.GetComponentInParent<EnemyHealth>();

        // 如果没有找到生命脚本，停止攻击并进入CD
        // HPスクリプトが見つからない場合は照射を停止してCDへ移行する
        if (currentEnemyHealth == null)
        {
            StopLaserVisual();

            isFiring = false;
            laserCoroutine = null;

            yield return StartCoroutine(
                CooldownRoutine()
            );

            yield break;
        }

        SetLaserVisible(true);

        float elapsedTime = 0f;

        while (elapsedTime < laserDuration)
        {
            // 方块失去攻击资格时停止
            // ブロックが攻撃不能になった場合は停止する
            if (attackState == null ||
                !attackState.canAttack)
            {
                break;
            }

            // 敌人被销毁时停止
            // 敵が破棄された場合は停止する
            if (currentTarget == null ||
                currentEnemyHealth == null)
            {
                break;
            }

            UpdateLaserPositions();

            // 根据时间持续造成伤害
            // 時間に応じて継続ダメージを与える
            float damageThisFrame =
                damagePerSecond * Time.deltaTime;

            currentEnemyHealth.TakeDamage(
                damageThisFrame
            );

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        StopLaserVisual();

        currentTarget = null;
        currentEnemyHealth = null;

        isFiring = false;
        laserCoroutine = null;

        // 无论正常结束还是敌人提前死亡，都进入CD
        // 正常終了でも敵が途中で死亡してもCDへ移行する
        yield return StartCoroutine(
            CooldownRoutine()
        );
    }

    /// <summary>
    /// 冷却流程
    /// クールダウン処理
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;

        yield return new WaitForSeconds(cooldown);

        isCoolingDown = false;
    }

    /// <summary>
    /// 搜索范围内距离最近的敌人
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

            EnemyHealth enemyHealth =
                enemyCollider.GetComponentInParent<EnemyHealth>();

            // 没有生命脚本的物体不视为有效敌人
            // HPスクリプトがない物体は有効な敵として扱わない
            if (enemyHealth == null)
                continue;

            Transform enemyTransform =
                enemyHealth.transform;

            float distanceSqr =
                ((Vector2)enemyTransform.position -
                 searchCenter).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = enemyTransform;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 更新镭射起点与终点
    /// レーザーの始点と終点を更新する
    /// </summary>
    private void UpdateLaserPositions()
    {
        if (laserLine == null ||
            currentTarget == null)
        {
            return;
        }

        Vector3 startPosition =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Vector3 endPosition =
            currentTarget.position +
            targetOffset;

        laserLine.SetPosition(
            0,
            startPosition
        );

        laserLine.SetPosition(
            1,
            endPosition
        );
    }

    /// <summary>
    /// 设置镭射显示状态
    /// レーザーの表示状態を設定する
    /// </summary>
    private void SetLaserVisible(bool visible)
    {
        if (laserLine == null)
            return;

        laserLine.enabled = visible;

        if (visible)
        {
            laserLine.positionCount = 2;
        }
    }

    /// <summary>
    /// 停止镭射视觉效果
    /// レーザーの視覚エフェクトを停止する
    /// </summary>
    private void StopLaserVisual()
    {
        SetLaserVisible(false);
    }

    /// <summary>
    /// 外部强制停止镭射
    /// 外部からレーザーを強制停止する
    /// </summary>
    public void StopLaser()
    {
        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }

        StopLaserVisual();

        currentTarget = null;
        currentEnemyHealth = null;

        isFiring = false;
        isCoolingDown = false;
    }

    private void OnDisable()
    {
        StopLaser();
    }

    private void OnDestroy()
    {
        StopLaser();
    }

    private void OnDrawGizmosSelected()
    {
        // 显示搜敌范围
        // 敵の検索範囲を表示する
        Gizmos.color = Color.red;

        Vector3 center =
            firePoint != null
                ? firePoint.position
                : transform.position;

        Gizmos.DrawWireSphere(
            center,
            searchRadius
        );
    }
}