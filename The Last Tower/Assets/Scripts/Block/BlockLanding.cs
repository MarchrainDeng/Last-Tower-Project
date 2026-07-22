using System.Collections;
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

    [Header("Fixed Block Settings")]

    // 特殊方块落地后需要稳定的物理帧数
    // 特殊ブロック着地後に安定を待つ物理フレーム数
    [SerializeField]
    private int fixedBlockSettleFrames = 3;

    // 判断方块已经稳定的最大速度
    // ブロックが安定したと判断する最大速度
    [SerializeField]
    private float fixedBlockMaxSpeed = 0.05f;

    // 是否正在进行固定流程
    // 固定処理中かどうか
    private bool isFixingBlock;

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
        // 防止重复执行落地逻辑
        // 着地処理の重複実行を防止する
        if (isLanded)
            return;

        isLanded = true;

        if (rb != null)
        {
            // 清除当前速度
            // 現在の速度をリセットする
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // 播放相机震动
            // カメラシェイクを再生する
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                CameraShake cameraShake =
                    mainCamera.GetComponent<CameraShake>();

                if (cameraShake != null)
                {
                    cameraShake.Shake();
                }
            }

            // 判断是否为落地后固定的特殊方块
            // 着地後に固定される特殊ブロックか判定する
            FixedBlock fixedBlock = GetComponent<FixedBlock>();

            if (fixedBlock != null)
            {
                // 特殊方块先保持Dynamic，
                // 让物理引擎有时间解除与下方方块的轻微重叠
                // 特殊ブロックは一時的にDynamicを維持し、
                // 下のブロックとのわずかな重なりを物理計算で解消させる
                rb.bodyType = RigidbodyType2D.Dynamic;

                // 无重力方块不受重力影响
                // 無重力ブロックは重力の影響を受けない
                rb.gravityScale = 0f;

                // 防止稳定期间旋转
                // 安定待機中の回転を防止する
                rb.freezeRotation = true;

                // 提高Kinematic与其他刚体的接触稳定性
                // Kinematicと他のRigidbodyとの接触を安定させる
                rb.useFullKinematicContacts = true;

                if (!isFixingBlock)
                {
                    StartCoroutine(
                        FixBlockAfterSettled()
                    );
                }
            }
            else
            {
                // 普通方块：落地后受到重力影响
                // 通常ブロック：着地後に重力の影響を受ける
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
            }
        }

        BlockMoveController moveController =
            GetComponent<BlockMoveController>();

        if (moveController != null)
        {
            moveController.enabled = false;
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
                "Flow Manager is missing. / " +
                "フローマネージャーが設定されていません。"
            );
        }
    }

    /// <summary>
    /// 等待特殊方块稳定后再切换为Kinematic
    /// 特殊ブロックが安定してからKinematicへ変更する
    /// </summary>
    private IEnumerator FixBlockAfterSettled()
    {
        if (rb == null)
            yield break;

        isFixingBlock = true;

        int stableFrameCount = 0;

        while (stableFrameCount < fixedBlockSettleFrames)
        {
            // 等待下一次物理解算
            // 次の物理計算まで待機する
            yield return new WaitForFixedUpdate();

            if (rb == null)
            {
                isFixingBlock = false;
                yield break;
            }

            // 检查当前线速度与角速度
            // 現在の線速度と角速度を確認する
            float currentSpeed =
                rb.linearVelocity.magnitude;

            float currentAngularSpeed =
                Mathf.Abs(rb.angularVelocity);

            // 连续多个物理帧速度足够小时，才视为稳定
            // 複数の物理フレームで速度が十分小さい場合のみ
            // 安定したと判断する
            if (currentSpeed <= fixedBlockMaxSpeed &&
                currentAngularSpeed <= fixedBlockMaxSpeed)
            {
                stableFrameCount++;
            }
            else
            {
                stableFrameCount = 0;
            }
        }

        // 切换前再次清除残留速度
        // 切り替え前に残留速度を再度リセットする
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 固定为Kinematic，同时保留Collider，
        // 因此依然可以托举其他方块
        // Colliderを維持したままKinematicに固定するため、
        // 他のブロックを支えることができる
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;

        isFixingBlock = false;
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
