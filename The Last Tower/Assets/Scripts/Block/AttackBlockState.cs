using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
令攻击方块和已充能方块接触时，进入可以攻击的状态。

攻撃ブロックと充能済みブロックが接触すると、攻撃可能な状態になります。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/05

---------------------------------------
*/

public class AttackBlockState : MonoBehaviour
{
    [Header("Attack State")]
    // 是否可以攻击
    // 攻撃可能かどうか
    public bool canAttack = false;

    [Header("Check Settings")]
    // 子方块尺寸
    // 子ブロックのサイズ
    public Vector2 cellSize = new Vector2(0.5f, 0.5f);

    // 检测距离
    // 判定距離
    public float checkDistance = 0.05f;

    // 已充能方块Layer
    // 通電済みブロックのLayer
    public LayerMask poweredBlockLayer;

    // 所有子方块
    // すべての子ブロック
    private Transform[] childCells;

    private void Awake()
    {
        // 自动获取所有子物体作为子方块
        // すべての子オブジェクトを子ブロックとして自動取得する
        childCells = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            childCells[i] = transform.GetChild(i);
        }
    }

    private void Update()
    {
        CheckAttackState();
    }

    /// <summary>
    /// 检测是否接触已充能方块
    /// 通電済みブロックに接触しているか判定する
    /// </summary>
    private void CheckAttackState()
    {
        bool foundPoweredBlock = false;

        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Transform cell in childCells)
        {
            foreach (Vector2 dir in directions)
            {
                Vector2 checkPos = (Vector2)cell.position + dir * (cellSize.x / 2f + checkDistance);

                Vector2 checkSize;

                if (dir == Vector2.left || dir == Vector2.right)
                {
                    checkSize = new Vector2(checkDistance * 2f, cellSize.y * 0.8f);
                }
                else
                {
                    checkSize = new Vector2(cellSize.x * 0.8f, checkDistance * 2f);
                }

                Collider2D hit = Physics2D.OverlapBox(
                    checkPos,
                    checkSize,
                    0f,
                    poweredBlockLayer
                );

                if (hit == null)
                    continue;

                // 避免检测到自己
                // 自分自身を検出しないようにする
                if (hit.transform.IsChildOf(transform))
                    continue;

                foundPoweredBlock = true;
                break;
            }

            if (foundPoweredBlock)
                break;
        }

        SetAttackState(foundPoweredBlock);
    }

    /// <summary>
    /// 设置攻击状态
    /// 攻撃状態を設定する
    /// </summary>
    private void SetAttackState(bool state)
    {
        if (canAttack == state)
            return;

        canAttack = state;

        if (canAttack)
        {
            Debug.Log("Attack Block Ready / 攻撃ブロック起動");
        }
        else
        {
            Debug.Log("Attack Block Disabled / 攻撃ブロック停止");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Transform cell in transform)
        {
            foreach (Vector2 dir in directions)
            {
                Vector2 checkPos = (Vector2)cell.position + dir * (cellSize.x / 2f + checkDistance);

                Vector2 checkSize;

                if (dir == Vector2.left || dir == Vector2.right)
                {
                    checkSize = new Vector2(checkDistance * 2f, cellSize.y * 0.8f);
                }
                else
                {
                    checkSize = new Vector2(cellSize.x * 0.8f, checkDistance * 2f);
                }

                Gizmos.DrawWireCube(checkPos, checkSize);
            }
        }
    }
}
