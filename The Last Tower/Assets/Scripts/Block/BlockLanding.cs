using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
令方块落地后受到重力影响。

ブロックが地面に落ちた後、重力の影響を受ける。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/04

---------------------------------------
*/

public class BlockLanding : MonoBehaviour
{

    [Header("References")]
    // 方块父物体的 Rigidbody2D
    // ブロック親オブジェクトの Rigidbody2D
    public Rigidbody2D rb;

    // 所有子方块
    // すべての子ブロック
    public Transform[] childBlocks;

    [Header("Landing Check")]
    // 每个小方块的尺寸
    // 各子ブロックのサイズ
    public Vector2 blockSize = new Vector2(0.5f, 0.5f);

    // 向下检测距离
    // 下方向への判定距離
    public float checkDistance = 0.05f;

    // 可以判定为落地的 Layer
    // 着地判定対象の Layer
    public LayerMask landingLayer;

    // 是否已经落地
    // すでに着地したか
    private bool isLanded = false;

    private void Update()
    {
        // 已经落地后不再重复检测
        // 着地後は再判定しない
        if (isLanded)
            return;

        CheckLanding();
    }

    private void CheckLanding()
    {
        foreach (Transform child in childBlocks)
        {
            // 检测位置：每个子方块的正下方
            // 判定位置：各子ブロックの真下
            Vector2 checkPos = (Vector2)child.position + Vector2.down * (blockSize.y / 2f + checkDistance);

            // 检测盒：稍微窄一点，避免侧面误判
            // 判定ボックス：横幅を少し狭くし、側面の誤判定を防ぐ
            Vector2 checkSize = new Vector2(blockSize.x * 0.8f, checkDistance * 2f);

            Collider2D hit = Physics2D.OverlapBox(
                checkPos,
                checkSize,
                0f,
                landingLayer
            );

            if (hit != null)
            {
                // 避免检测到自己的子方块
                // 自分自身の子ブロックを検出しないようにする
                if (hit.transform.IsChildOf(transform))
                    continue;

                Land();
                return;
            }
        }
    }

    private void Land()
    {
        isLanded = true;

        // 落地后启用重力
        // 着地後、重力を有効にする
        rb.gravityScale = 1f;

        BlockMoveController moveController = GetComponent<BlockMoveController>();

        if (moveController != null)
        {
            moveController.enabled = false;

            Debug.Log("false");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (childBlocks == null)
            return;

        Gizmos.color = Color.green;

        foreach (Transform child in childBlocks)
        {
            if (child == null)
                continue;

            Vector2 checkPos = (Vector2)child.position + Vector2.down * (blockSize.y / 2f + checkDistance);
            Vector2 checkSize = new Vector2(blockSize.x * 0.8f, checkDistance * 2f);

            Gizmos.DrawWireCube(checkPos, checkSize);
        }
    }
}
