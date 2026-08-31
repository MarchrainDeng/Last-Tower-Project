using System.Collections.Generic;
using UnityEngine;

public class AttackSpeedBoostBlock : MonoBehaviour
{
    [Header("Boost Settings")]

    // 攻击速度倍率
    // 攻撃速度倍率
    [SerializeField]
    private float speedMultiplier = 2f;

    [Header("Detection Settings")]

    // 检测范围
    // 検出範囲
    [SerializeField]
    private Vector2 detectionSize =
        new Vector2(1.1f, 1.1f);

    // 攻击方块Layer
    // 攻撃ブロックのLayer
    [SerializeField]
    private LayerMask attackBlockLayer;

    private readonly HashSet<IAttackSpeedBoostable>
        boostedTargets = new();

    private void FixedUpdate()
    {
        UpdateBoostTargets();
    }

    private void UpdateBoostTargets()
    {
        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                transform.position,
                detectionSize,
                0f,
                attackBlockLayer
            );

        HashSet<IAttackSpeedBoostable>
            currentTargets = new();

        foreach (Collider2D hit in hits)
        {
            IAttackSpeedBoostable target =
                hit.GetComponentInParent<
                    IAttackSpeedBoostable>();

            if (target == null)
                continue;

            currentTargets.Add(target);

            // 新接触的攻击方块
            // 新しく接触した攻撃ブロック
            if (!boostedTargets.Contains(target))
            {
                target.AddAttackSpeedBoost(
                    speedMultiplier
                );
            }
        }

        // 检查已经离开的攻击方块
        // 離れた攻撃ブロックを確認する
        foreach (IAttackSpeedBoostable target
                 in boostedTargets)
        {
            if (!currentTargets.Contains(target))
            {
                target.RemoveAttackSpeedBoost(
                    speedMultiplier
                );
            }
        }

        boostedTargets.Clear();

        foreach (IAttackSpeedBoostable target
                 in currentTargets)
        {
            boostedTargets.Add(target);
        }
    }

    private void OnDisable()
    {
        // 方块被销毁时解除所有加速
        // ブロック破棄時に全ての強化を解除する
        foreach (IAttackSpeedBoostable target
                 in boostedTargets)
        {
            target?.RemoveAttackSpeedBoost(
                speedMultiplier
            );
        }

        boostedTargets.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(
            transform.position,
            detectionSize
        );
    }
}