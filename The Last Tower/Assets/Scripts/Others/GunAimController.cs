using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
寻找距离最近的敌人，
并让枪管图片的右侧持续朝向该敌人。

最も近い敵を検索し、
銃身画像の右側が常にその敵を向くように回転させる。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class GunAimController : MonoBehaviour
{
    [Header("References")]

    // 枪的旋转轴对象
    // 銃の回転軸オブジェクト
    public Transform gunPivot;

    // 攻击状态脚本
    // 攻撃状態スクリプト
    public AttackBlockState attackBlockState;

    [Header("Enemy Settings")]

    // 敌人的Layer
    // 敵のLayer
    public LayerMask enemyLayer;

    // 搜索敌人的最大距离
    // 敵を検索する最大距離
    public float searchRadius = 20f;

    [Header("Rotation Settings")]

    // 枪管旋转速度
    // 銃身の回転速度
    public float rotateSpeed = 360f;

    // 图片角度修正
    // 画像角度の補正
    public float angleOffset = 0f;

    // 当前目标
    // 現在のターゲット
    private Transform currentTarget;

    private void Update()
    {
        // 没有枪轴时不执行
        // 銃の回転軸がない場合は処理しない
        if (gunPivot == null)
            return;

        // 无法攻击时不寻找或瞄准敌人
        // 攻撃不可の場合は敵の検索と照準を行わない
        if (attackBlockState != null &&
            !attackBlockState.canAttack)
        {
            currentTarget = null;
            return;
        }

        FindNearestEnemy();
        RotateTowardTarget();
    }

    /// <summary>
    /// 寻找距离枪最近的敌人
    /// 銃から最も近い敵を検索する
    /// </summary>
    private void FindNearestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            gunPivot.position,
            searchRadius,
            enemyLayer
        );

        currentTarget = null;

        float nearestDistanceSqr = Mathf.Infinity;

        foreach (Collider2D enemyCollider in enemies)
        {
            if (enemyCollider == null)
                continue;

            Transform enemy = enemyCollider.transform;

            // 使用平方距离，避免频繁计算平方根
            // 平方距離を使用し、平方根計算を避ける
            float distanceSqr =
                ((Vector2)enemy.position -
                 (Vector2)gunPivot.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                currentTarget = enemy;
            }
        }
    }

    /// <summary>
    /// 让枪管右侧朝向当前目标
    /// 銃身の右側を現在のターゲットへ向ける
    /// </summary>
    private void RotateTowardTarget()
    {
        if (currentTarget == null)
            return;

        // 计算枪轴指向敌人的方向
        // 銃の回転軸から敵への方向を計算する
        Vector2 direction =
            (Vector2)currentTarget.position -
            (Vector2)gunPivot.position;

        // 将方向转换成Z轴角度
        // 方向をZ軸角度へ変換する
        float targetAngle =
            Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg
            +180f
            + angleOffset;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        // 平滑旋转到目标方向
        // ターゲット方向へ滑らかに回転する
        gunPivot.rotation = Quaternion.RotateTowards(
            gunPivot.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (gunPivot == null)
            return;

        // 绘制敌人搜索范围
        // 敵の検索範囲を描画する
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            gunPivot.position,
            searchRadius
        );

        // 绘制当前目标方向
        // 現在のターゲット方向を描画する
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                gunPivot.position,
                currentTarget.position
            );
        }
    }
}