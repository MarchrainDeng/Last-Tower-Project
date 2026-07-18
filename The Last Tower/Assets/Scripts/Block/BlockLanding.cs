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

    // 对外提供只读的落地状态
    // 外部へ読み取り専用の着地状態を提供する
    public bool IsLanded => isLanded;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    private BlockSelectionFlowManager flowManager;

    private void Update()
    {
        // 已经落地后不再重复检测
        // 着地後は再判定しない
        if (isLanded)
            return;

        CheckLanding();
    }

    /// <summary>
    /// 设置流程管理器
    /// フローマネージャーを設定する
    /// </summary>
    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }

    private void CheckLanding()
    {
        foreach (Transform child in childBlocks)
        {
            if (child == null)
                continue;

            // 子方块底部中心位置
            // 子ブロック底面の中心位置
            Vector2 bottomPosition =
                (Vector2)child.position +
                Vector2.down * (blockSize.y * 0.5f);

            // 底部检测盒
            // 底面判定ボックス
            Vector2 checkSize = new Vector2(
                blockSize.x * 0.95f,
                checkDistance * 2f
            );

            // 检测底部区域内的所有Collider
            // 底面範囲内にあるすべてのColliderを検出する
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                bottomPosition,
                checkSize,
                0f,
                landingLayer
            );

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                // 排除当前方块自身以及自身的子方块
                // 現在のブロック自身と子ブロックを除外する
                if (hit.transform == transform ||
                    hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                // 检测到地面或其他已落地方块
                // 地面または他の着地済みブロックを検出した
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
        if (rb != null)
        {
            // 清除当前速度
            // 現在の速度をリセットする
            rb.linearVelocity = Vector2.zero;

            Camera.main.GetComponent<CameraShake>().Shake();


            // 判断是否为落地后固定的特殊方块
            // 着地後に固定される特殊ブロックか判定する
            FixedBlock fixedBlock = GetComponent<FixedBlock>();

            if (fixedBlock != null)
            {
                // 特殊方块：改为Kinematic，不受重力和碰撞推力影响
                // 特殊ブロック：Kinematicに変更し、重力や衝突力の影響を受けない
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;

                // 防止物理旋转
                // 物理回転を防止する
                rb.freezeRotation = true;
            }
            else
            {
                // 普通方块：落地后受到重力影响
                // 通常ブロック：着地後に重力の影響を受ける
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
            }
        }

        BlockMoveController moveController = GetComponent<BlockMoveController>();

        if (moveController != null)
        {
            moveController.enabled = false;

            //Debug.Log("false");
        }

        // 通知流程管理器重新开启卡牌选择
        // フローマネージャーにカード選択の再開を通知する
        if (flowManager != null)
        {
            flowManager.OnCurrentBlockLanded();
        }
        else
        {
            Debug.LogWarning(
                "Flow Manager is missing. / フローマネージャーが設定されていません。"
            );
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
