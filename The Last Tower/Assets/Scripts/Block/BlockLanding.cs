using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
方块的碰撞体接触地面或其他方块后，判定为落地。
落地后使方块受到重力影响。

ブロックのColliderが地面または他のブロックに接触すると、
着地したと判定する。
着地後、ブロックを重力の影響下に置く。

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

    // 方块根对象的Rigidbody2D
    // ブロックのルートオブジェクトのRigidbody2D
    [SerializeField]
    private Rigidbody2D rb;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    private BlockSelectionFlowManager flowManager;

    // 黏着方块功能
    // 粘着ブロック機能
    [SerializeField]
    private StickyBlockJoint stickyBlockJoint;

    [Header("Landing Collision")]

    // 可以判定为落地的Layer
    // 着地判定対象のLayer
    [SerializeField]
    private LayerMask landingLayer;

    // 是否要求碰撞来自方块下方
    // 下方向からの衝突のみ着地と判定するか
    [SerializeField]
    private bool requireSupportFromBelow = true;

    // 接触法线的最低Y值
    // 接触法線の最小Y値
    [Range(0f, 1f)]
    [SerializeField]
    private float minimumSupportNormalY = 0.5f;

    // 是否已经落地
    // すでに着地したか
    private bool isLanded;

    public Transform[] childBlocks;

    // 对外提供只读落地状态
    // 外部へ読み取り専用の着地状態を提供する
    public bool IsLanded => isLanded;

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

    [Header("Physics Material")]

    // 落地后多久切换材质
    // 着地後、何秒で材質を切り替えるか
    [SerializeField]
    private float materialChangeDelay = 0.2f;

    // 下落时使用的物理材质
    // 落下中に使用するPhysics Material
    [SerializeField]
    private PhysicsMaterial2D fallingMaterial;

    // 落地后使用的物理材质
    // 着地後に使用するPhysics Material
    [SerializeField]
    private PhysicsMaterial2D landMaterial;

    private Collider2D[] colliders;

    [Header("Landing Sound")]

    // 方块落地音效
    // ブロック着地効果音
    [SerializeField]
    private AudioClip landingSound;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (stickyBlockJoint == null)
        {
            stickyBlockJoint =
                GetComponent<StickyBlockJoint>();
        }

        colliders =
            GetComponentsInChildren<Collider2D>(true);

        // 游戏开始时使用下落材质
        // ゲーム開始時に落下用マテリアルを設定する
        foreach (Collider2D currentCollider in colliders)
        {
            if (currentCollider != null)
            {
                currentCollider.sharedMaterial =
                    fallingMaterial;
            }
        }
    }

    /// <summary>
    /// 设置流程管理器
    /// フローマネージャーを設定する
    /// </summary>
    public void SetFlowManager(
        BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }

    /// <summary>
    /// 碰撞开始时检查是否落地
    /// 衝突開始時に着地を確認する
    /// </summary>
    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        TryLandFromCollision(collision);
    }

    /// <summary>
    /// 持续碰撞时再次检查
    /// 接触中に再度着地を確認する
    /// </summary>
    private void OnCollisionStay2D(
        Collision2D collision)
    {
        TryLandFromCollision(collision);
    }

    /// <summary>
    /// 根据碰撞信息尝试判定落地
    /// 衝突情報から着地判定を行う
    /// </summary>
    private void TryLandFromCollision(
        Collision2D collision)
    {
        if (isLanded)
            return;

        if (collision == null)
            return;

        GameObject otherObject =
            collision.gameObject;

        // 只检测指定Layer
        // 指定Layerのみ判定する
        if (!IsInLandingLayer(otherObject.layer))
            return;

        // 防止错误识别自己的子碰撞体
        // 自身の子Colliderを誤判定しない
        if (otherObject.transform == transform ||
            otherObject.transform.IsChildOf(transform))
        {
            return;
        }

        // 不要求从下方支撑时，只要碰撞就算落地
        // 下方向の支持を要求しない場合、
        // 衝突した時点で着地と判定する
        if (!requireSupportFromBelow)
        {
            Land();
            return;
        }

        // 检查是否存在来自下方的支撑接触
        // 下方向から支持されている接触があるか確認する
        if (HasSupportContact(collision))
        {
            Land();
        }
    }

    /// <summary>
    /// 检查碰撞接触面是否在当前方块下方
    /// 衝突接触面が現在のブロックの下側にあるか確認する
    /// </summary>
    private bool HasSupportContact(
        Collision2D collision)
    {
        for (int i = 0;
             i < collision.contactCount;
             i++)
        {
            ContactPoint2D contact =
                collision.GetContact(i);

            /*
             * Collision2D中的normal方向，
             * 是从对方碰撞体指向当前碰撞体。
             *
             * Collision2Dのnormalは、
             * 相手Colliderから現在のColliderへ向く。
             *
             * 因此下方物体支撑当前方块时，
             * normal.y通常为正数。
             */
            if (contact.normal.y >=
                minimumSupportNormalY)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断对象是否属于落地Layer
    /// オブジェクトが着地対象Layerに含まれるか確認する
    /// </summary>
    private bool IsInLandingLayer(int layer)
    {
        return (
            landingLayer.value &
            (1 << layer)
        ) != 0;
    }

    /// <summary>
    /// 执行落地处理
    /// 着地処理を実行する
    /// </summary>
    private void Land()
    {
        // 防止重复执行落地逻辑
        // 着地処理の重複実行を防止する
        if (isLanded)
            return;

        isLanded = true;

        // 播放方块落地音效
        // ブロック着地効果音を再生する
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(landingSound);
        }

        if (stickyBlockJoint != null)
        {
            // 通知黏着方块已经落地
            // 粘着ブロックへ着地を通知する
            stickyBlockJoint.SetLanded(true);
        }

        if (rb != null)
        {
            // 清除当前速度
            // 現在の速度をリセットする
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.mass = 10f;

            StartCoroutine(ChangeMaterialLater());

            PlayCameraShake();

            // 判断是否为落地后固定的特殊方块
            // 着地後に固定される特殊ブロックか確認する
            FixedBlock fixedBlock =
                GetComponent<FixedBlock>();

            if (fixedBlock != null)
            {
                // 先保持Dynamic，让物理引擎解除轻微重叠
                // 一時的にDynamicを維持し、
                // わずかな重なりを解消させる
                rb.bodyType =
                    RigidbodyType2D.Dynamic;

                rb.gravityScale = 0f;
                rb.freezeRotation = true;
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
                // 普通方块落地后受到重力影响
                // 通常ブロックは着地後に重力の影響を受ける
                rb.bodyType =
                    RigidbodyType2D.Dynamic;

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
        // フローマネージャーへカード選択再開を通知する
        if (flowManager != null)
        {
            flowManager.OnCurrentBlockLanded();
        }
        else
        {
            Debug.LogWarning(
                "Flow Manager is missing. / " +
                "フローマネージャーが設定されていません。",
                this
            );
        }
    }

    /// <summary>
    /// 播放相机震动
    /// カメラシェイクを再生する
    /// </summary>
    private void PlayCameraShake()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        CameraShake cameraShake =
            mainCamera.GetComponent<CameraShake>();

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }
    }

    /// <summary>
    /// 等待特殊方块稳定后切换为Kinematic
    /// 特殊ブロックが安定してからKinematicへ変更する
    /// </summary>
    private IEnumerator FixBlockAfterSettled()
    {
        if (rb == null)
            yield break;

        isFixingBlock = true;

        int stableFrameCount = 0;

        while (stableFrameCount <
               fixedBlockSettleFrames)
        {
            yield return new WaitForFixedUpdate();

            if (rb == null)
            {
                isFixingBlock = false;
                yield break;
            }

            float currentSpeed =
                rb.linearVelocity.magnitude;

            float currentAngularSpeed =
                Mathf.Abs(rb.angularVelocity);

            if (currentSpeed <= fixedBlockMaxSpeed &&
                currentAngularSpeed <=
                fixedBlockMaxSpeed)
            {
                stableFrameCount++;
            }
            else
            {
                stableFrameCount = 0;
            }
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.bodyType =
            RigidbodyType2D.Kinematic;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;

        isFixingBlock = false;
    }

    /// <summary>
    /// 延迟切换物理材质
    /// 遅延してPhysics Materialを切り替える
    /// </summary>
    private IEnumerator ChangeMaterialLater()
    {
        yield return new WaitForSeconds(
            materialChangeDelay
        );

        foreach (Collider2D currentCollider in colliders)
        {
            if (currentCollider != null)
            {
                currentCollider.sharedMaterial =
                    landMaterial;
            }
        }

        if (rb != null)
        {
            rb.sharedMaterial = landMaterial;
        }
    }
}