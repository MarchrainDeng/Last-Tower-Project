using System.Collections;
using UnityEngine;

public class LaserShooter : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private LineRenderer laserLine;

    [SerializeField]
    private AttackBlockState attackState;

    [Header("Target Settings")]

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField]
    private float searchRadius = 10f;

    [Header("Laser Settings")]

    [SerializeField]
    private float laserDuration = 2f;

    [SerializeField]
    private float damagePerSecond = 20f;

    [SerializeField]
    private float cooldown = 3f;

    [SerializeField]
    private Vector3 targetOffset = Vector3.zero;

    [Header("Debug")]

    [SerializeField]
    private bool isFiring;

    [SerializeField]
    private bool isCoolingDown;

    // 当前目标
    private Transform currentTarget;

    // 普通敌人的生命脚本
    private EnemyHealth currentEnemyHealth;

    // Boss手的生命脚本
    private BossHand currentBossHand;

    // 镭射协程
    private Coroutine laserCoroutine;

    [Header("Laser Sound")]

    [SerializeField]
    private AudioSource laserAudioSource;

    [SerializeField]
    private AudioClip laserLoopSound;

    private void Awake()
    {
        if (laserAudioSource == null)
        {
            laserAudioSource =
                GetComponent<AudioSource>();
        }

        if (laserAudioSource != null)
        {
            laserAudioSource.playOnAwake = false;
            laserAudioSource.loop = true;
            laserAudioSource.spatialBlend = 0f;
        }

        if (attackState == null)
        {
            attackState =
                GetComponent<AttackBlockState>();
        }

        SetLaserVisible(false);
    }

    private void Update()
    {
        // 发射中或冷却中时不重新搜索
        if (isFiring || isCoolingDown)
            return;

        // 当前攻击方块无法攻击
        if (attackState == null ||
            !attackState.canAttack)
        {
            return;
        }

        Transform nearestEnemy =
            FindNearestEnemy();

        if (nearestEnemy == null)
            return;

        StartLaser(nearestEnemy);
    }

    /// <summary>
    /// 开始向指定目标发射镭射
    /// </summary>
    private void StartLaser(Transform target)
    {
        if (target == null)
            return;

        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }

        laserCoroutine =
            StartCoroutine(LaserRoutine(target));
    }

    /// <summary>
    /// 镭射发射流程
    /// </summary>
    private IEnumerator LaserRoutine(
        Transform target)
    {
        isFiring = true;
        currentTarget = target;

        // 尝试取得普通敌人组件
        currentEnemyHealth =
            target.GetComponentInParent<EnemyHealth>();

        // 尝试取得Boss手组件
        currentBossHand =
            target.GetComponentInParent<BossHand>();

        // 两种生命组件都不存在时，不是有效目标
        if (currentEnemyHealth == null &&
            currentBossHand == null)
        {
            FinishLaser();

            yield return StartCoroutine(
                CooldownRoutine()
            );

            yield break;
        }

        // Boss已经死亡时不攻击
        if (currentBossHand != null &&
            currentBossHand.IsDead)
        {
            FinishLaser();

            yield return StartCoroutine(
                CooldownRoutine()
            );

            yield break;
        }

        SetLaserVisible(true);
        StartLaserSound();

        // 追加：Boss手が対象の場合、この照射を「被弾1回」として扱う
        if (currentBossHand != null)
            currentBossHand.SetContinuousDamageState(true);

        float elapsedTime = 0f;

        while (elapsedTime < laserDuration)
        {
            // 攻击方块断电或失去攻击资格
            if (attackState == null ||
                !attackState.canAttack)
            {
                break;
            }

            // 目标对象被销毁
            if (currentTarget == null)
            {
                break;
            }

            // 检查当前目标是否仍然有效
            if (!IsCurrentTargetAlive())
            {
                break;
            }

            UpdateLaserPositions();

            float damageThisFrame =
                damagePerSecond * Time.deltaTime;

            DamageCurrentTarget(
                damageThisFrame
            );

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        FinishLaser();

        // 正常结束或目标死亡后都进入冷却
        yield return StartCoroutine(
            CooldownRoutine()
        );
    }

    /// <summary>
    /// 判断当前目标是否存活
    /// </summary>
    private bool IsCurrentTargetAlive()
    {
        // 普通敌人
        if (currentEnemyHealth != null)
        {
            /*
             * 如果EnemyHealth在死亡时会销毁对象，
             * Unity会自动让这个引用变为null。
             * 如果EnemyHealth有公开死亡状态，
             * 也可以在这里追加判断。
             */
            return true;
        }

        // Boss手
        if (currentBossHand != null)
        {
            return !currentBossHand.IsDead;
        }

        return false;
    }

    /// <summary>
    /// 对当前目标造成伤害
    /// </summary>
    private void DamageCurrentTarget(
        float damage)
    {
        if (currentEnemyHealth != null)
        {
            currentEnemyHealth.TakeDamage(
                damage
            );

            return;
        }

        if (currentBossHand != null &&
            !currentBossHand.IsDead)
        {
            currentBossHand.TakeDamage(
                damage
            );
        }
    }

    /// <summary>
    /// 结束当前镭射
    /// </summary>
    private void FinishLaser()
    {
        StopLaserVisual();

        // 追加：照射終了を通知（被弾カウントを解除）
        if (currentBossHand != null)
            currentBossHand.SetContinuousDamageState(false);

        currentTarget = null;
        currentEnemyHealth = null;
        currentBossHand = null;

        isFiring = false;
        laserCoroutine = null;
    }

    /// <summary>
    /// 冷却流程
    /// </summary>
    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;

        yield return new WaitForSeconds(
            cooldown
        );

        isCoolingDown = false;
    }

    /// <summary>
    /// 搜索范围内距离最近的有效目标
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
        float nearestDistanceSqr =
            Mathf.Infinity;

        foreach (Collider2D enemyCollider
                 in enemyColliders)
        {
            if (enemyCollider == null)
                continue;

            EnemyHealth enemyHealth =
                enemyCollider
                    .GetComponentInParent<EnemyHealth>();

            BossHand bossHand =
                enemyCollider
                    .GetComponentInParent<BossHand>();

            Transform targetTransform = null;

            // 普通敌人
            if (enemyHealth != null)
            {
                targetTransform =
                    enemyHealth.transform;
            }
            // Boss手
            else if (bossHand != null &&
                     !bossHand.IsDead)
            {
                targetTransform =
                    bossHand.transform;
            }

            if (targetTransform == null)
                continue;

            float distanceSqr =
                ((Vector2)targetTransform.position -
                 searchCenter).sqrMagnitude;

            if (distanceSqr <
                nearestDistanceSqr)
            {
                nearestDistanceSqr =
                    distanceSqr;

                nearestEnemy =
                    targetTransform;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// 更新镭射的起点与终点
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
    /// </summary>
    private void SetLaserVisible(
        bool visible)
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
    /// </summary>
    private void StopLaserVisual()
    {
        StopLaserSound();
        SetLaserVisible(false);
    }

    /// <summary>
    /// 从外部强制停止镭射
    /// </summary>
    public void StopLaser()
    {
        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }

        StopLaserVisual();

        // 追加：強制停止時も照射終了を通知
        if (currentBossHand != null)
            currentBossHand.SetContinuousDamageState(false);

        currentTarget = null;
        currentEnemyHealth = null;
        currentBossHand = null;

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

    /// <summary>
    /// 开始播放镭射循环音效
    /// </summary>
    private void StartLaserSound()
    {
        if (laserAudioSource == null ||
            laserLoopSound == null)
        {
            return;
        }

        if (laserAudioSource.isPlaying)
            return;

        laserAudioSource.clip =
            laserLoopSound;

        laserAudioSource.loop = true;
        laserAudioSource.Play();
    }

    /// <summary>
    /// 停止镭射循环音效
    /// </summary>
    public void StopLaserSound()
    {
        if (laserAudioSource == null)
            return;

        if (laserAudioSource.isPlaying)
        {
            laserAudioSource.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
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