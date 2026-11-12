using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
方块接触地面或其他方块后判定落地。
普通方块延迟启用重力，固定方块稳定后切换为Kinematic。
同时处理落地音效、震动、物理材质和下一次卡牌选择。

ブロックが地面または他のブロックに接触すると着地判定する。
通常ブロックは遅延して重力を有効化し、
固定ブロックは安定後にKinematicへ切り替える。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/04
----------------------------------------
*/

public class BlockLanding : MonoBehaviour
{
    [Header("References")]

    // 方块根对象的刚体
    // ブロックのルートオブジェクトのRigidbody2D
    [SerializeField] private Rigidbody2D rb;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    private BlockSelectionFlowManager flowManager;

    // 黏着方块功能
    // 粘着ブロック機能
    [SerializeField] private StickyBlockJoint stickyBlockJoint;


    [Header("Landing Collision")]

    // 可以判定为落地的Layer
    // 着地判定対象のLayer
    [SerializeField] private LayerMask landingLayer;

    // 是否要求碰撞来自方块下方
    // 下方向からの衝突のみ着地と判定するか
    [SerializeField] private bool requireSupportFromBelow = true;

    // 接触法线的最低Y值
    // 接触法線の最小Y値
    [Range(0f, 1f)]
    [SerializeField] private float minimumSupportNormalY = 0.5f;

    // 是否已经落地
    // すでに着地したか
    private bool isLanded;

    public Transform[] childBlocks;

    // 对外提供只读落地状态
    // 外部へ読み取り専用の着地状態を提供する
    public bool IsLanded => isLanded;


    [Header("Fixed Block Settings")]

    // 固定方块稳定所需物理帧数
    // 固定ブロックの安定判定に必要な物理フレーム数
    [SerializeField] private int fixedBlockSettleFrames = 3;

    // 判断稳定的最大速度
    // 安定したと判断する最大速度
    [SerializeField] private float fixedBlockMaxSpeed = 0.05f;

    private bool isFixingBlock;


    [Header("Physics Material")]

    // 落地后切换材质的延迟
    // 着地後のマテリアル切り替え遅延
    [SerializeField] private float materialChangeDelay = 0.2f;

    [SerializeField] private PhysicsMaterial2D fallingMaterial;
    [SerializeField] private PhysicsMaterial2D landMaterial;

    private Collider2D[] colliders;


    [Header("Landing Sound")]

    [SerializeField] private AudioClip landingSound;


    [Header("Rotation Lock")]

    // 落地后暂时锁定旋转的时间
    // 着地後に回転を一時固定する時間
    [SerializeField] private float rotationLockDuration = 0.2f;


    [Header("Side Landing Check")]

    // 左右检测距离及范围
    // 左右方向の判定距離と範囲
    [SerializeField] private float sideCheckDistance = 0.05f;
    [SerializeField] private float sideCheckWidth = 0.05f;
    [SerializeField] private float sideCheckHeight = 0.45f;


    [Header("Normal Block Landing Delay")]

    // 普通方块落地后的操作保留时间
    // 通常ブロック着地後の操作継続時間
    [SerializeField] private float normalBlockLandingDelay = 0.2f;

    private Coroutine normalBlockLandingCoroutine;


    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (stickyBlockJoint == null)
        {
            stickyBlockJoint = GetComponent<StickyBlockJoint>();
        }

        colliders = GetComponentsInChildren<Collider2D>(true);

        // 初始使用下落物理材质
        // 初期状態で落下用マテリアルを設定する
        foreach (Collider2D currentCollider in colliders)
        {
            if (currentCollider != null)
            {
                currentCollider.sharedMaterial = fallingMaterial;
            }
        }
    }


    /// <summary>
    /// 设置流程管理器
    /// フローマネージャーを設定する
    /// </summary>
    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }


    /// <summary>
    /// 碰撞开始时检查落地
    /// 衝突開始時に着地を確認する
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLandFromCollision(collision);
    }


    /// <summary>
    /// 持续碰撞时检查落地
    /// 接触中に着地を確認する
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryLandFromCollision(collision);
    }


    /// <summary>
    /// 根据碰撞信息尝试判定落地
    /// 衝突情報から着地判定を行う
    /// </summary>
    private void TryLandFromCollision(Collision2D collision)
    {
        if (isLanded || collision == null)
            return;

        // 去除朝向接触面的速度
        // 接触面方向の速度成分を除去する
        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;
            Vector2 velocity = rb.linearVelocity;

            float dot = Vector2.Dot(velocity, -normal);

            if (dot > 0f)
            {
                velocity += normal * dot;
            }

            rb.linearVelocity = velocity;
        }

        GameObject otherObject = collision.gameObject;

        // 只检测指定Layer
        // 指定Layerのみ判定する
        if (!IsInLandingLayer(otherObject.layer))
            return;

        // 排除自身及子碰撞体
        // 自身と子Colliderを除外する
        if (otherObject.transform == transform ||
            otherObject.transform.IsChildOf(transform))
        {
            return;
        }

        // 不要求下方支撑时直接判定落地
        // 下方向の支持が不要な場合は着地とする
        if (!requireSupportFromBelow)
        {
            Land();
            return;
        }

        // 检查下方支撑
        // 下方向の支持を確認する
        if (HasSupportContact(collision))
        {
            Land();
            return;
        }

        // 检查左右方向支撑
        // 左右方向の支持を確認する
        if (HasSideSupport())
        {
            Land();
        }
    }


    /// <summary>
    /// 检查接触面是否提供下方支撑
    /// 接触面が下方向から支持しているか確認する
    /// </summary>
    private bool HasSupportContact(Collision2D collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.normal.y >= minimumSupportNormalY)
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// 判断对象是否属于落地Layer
    /// 着地対象Layerに含まれるか確認する
    /// </summary>
    private bool IsInLandingLayer(int layer)
    {
        return (landingLayer.value & (1 << layer)) != 0;
    }


    /// <summary>
    /// 执行落地处理
    /// 着地処理を実行する
    /// </summary>
    private void Land()
    {
        if (isLanded)
            return;

        isLanded = true;

        // 统计放置方块数量
        // 配置したブロック数をカウントする
        if (GameStatsManager.Instance != null)
        {
            GameStatsManager.Instance.OnBlockPlaced();
        }

        // 落地音效
        // 着地効果音
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(landingSound);
        }

        // 手柄震动
        // ゲームパッド振動
        GamepadVibrationManager.Instance?.PlayVibration(
            0.5f, 0.9f, 0.15f
        );

        // 通知黏着方块
        // 粘着ブロックへ通知する
        if (stickyBlockJoint != null)
        {
            stickyBlockJoint.SetLanded(true);
        }

        FixedBlock fixedBlock = GetComponent<FixedBlock>();

        BlockMoveController moveController =
            GetComponent<BlockMoveController>();

        if (rb != null)
        {
            // 清除物理速度
            // 物理速度をリセットする
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            StartCoroutine(ChangeMaterialLater());

            PlayCameraShake();

            if (fixedBlock != null)
            {
                // 固定方块立即停止操作
                // 固定ブロックは直ちに操作を停止する
                if (moveController != null)
                {
                    moveController.enabled = false;
                }

                // 暂时保持Dynamic，等待稳定
                // 安定するまでDynamicを維持する
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.useFullKinematicContacts = true;

                if (!isFixingBlock)
                {
                    StartCoroutine(FixBlockAfterSettled());
                }

                // 立即开始下一次选择
                // 直ちに次のカード選択を開始する
                RequestNextSelection();
            }
            else
            {
                // 普通方块延迟启用重力，期间保留操作
                // 通常ブロックは遅延中に操作を維持する
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;

                if (normalBlockLandingCoroutine != null)
                {
                    StopCoroutine(normalBlockLandingCoroutine);
                }

                StartCoroutine(LockRotationTemporarily());

                normalBlockLandingCoroutine =
                    StartCoroutine(
                        HandleNormalBlockLandingDelay(moveController)
                    );
            }
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
    /// 等待固定方块稳定后切换为Kinematic
    /// 固定ブロックが安定してからKinematicへ変更する
    /// </summary>
    private IEnumerator FixBlockAfterSettled()
    {
        if (rb == null)
            yield break;

        isFixingBlock = true;

        int stableFrameCount = 0;

        while (stableFrameCount < fixedBlockSettleFrames)
        {
            yield return new WaitForFixedUpdate();

            if (rb == null)
            {
                isFixingBlock = false;
                yield break;
            }

            float currentSpeed = rb.linearVelocity.magnitude;
            float currentAngularSpeed = Mathf.Abs(rb.angularVelocity);

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

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.bodyType = RigidbodyType2D.Kinematic;
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
        yield return new WaitForSeconds(materialChangeDelay);

        foreach (Collider2D currentCollider in colliders)
        {
            if (currentCollider != null)
            {
                currentCollider.sharedMaterial = landMaterial;
            }
        }

        if (rb != null)
        {
            rb.sharedMaterial = landMaterial;
            rb.mass = 10f;
        }
    }


    /// <summary>
    /// 落地后暂时锁定旋转
    /// 着地後に一時的に回転を固定する
    /// </summary>
    private IEnumerator LockRotationTemporarily()
    {
        rb.angularVelocity = 0f;

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        yield return new WaitForSeconds(rotationLockDuration);

        rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
    }


    /// <summary>
    /// 检测左右方向是否存在方块
    /// 左右方向にブロックがあるか確認する
    /// </summary>
    private bool HasSideSupport()
    {
        foreach (Transform child in childBlocks)
        {
            if (child == null)
                continue;

            Vector2 leftCenter =
                (Vector2)child.position +
                Vector2.left * (0.25f + sideCheckDistance);

            Vector2 rightCenter =
                (Vector2)child.position +
                Vector2.right * (0.25f + sideCheckDistance);

            Vector2 boxSize = new Vector2(
                sideCheckWidth,
                sideCheckHeight
            );

            // 左侧检测
            // 左側の判定
            Collider2D leftHit = Physics2D.OverlapBox(
                leftCenter,
                boxSize,
                0f,
                landingLayer
            );

            if (leftHit != null &&
                leftHit.transform != transform &&
                !leftHit.transform.IsChildOf(transform))
            {
                return true;
            }

            // 右侧检测
            // 右側の判定
            Collider2D rightHit = Physics2D.OverlapBox(
                rightCenter,
                boxSize,
                0f,
                landingLayer
            );

            if (rightHit != null &&
                rightHit.transform != transform &&
                !rightHit.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// 普通方块落地后延迟启用重力并关闭操作
    /// 通常ブロックの重力有効化と操作停止を遅延する
    /// </summary>
    private IEnumerator HandleNormalBlockLandingDelay(
        BlockMoveController moveController)
    {
        yield return new WaitForSeconds(normalBlockLandingDelay);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }

        if (moveController != null)
        {
            moveController.enabled = false;
        }

        // 开始下一次卡牌选择
        // 次のカード選択を開始する
        RequestNextSelection();

        normalBlockLandingCoroutine = null;
    }


    /// <summary>
    /// 通知流程管理器开始下一次选择
    /// フローマネージャーへ次の選択開始を通知する
    /// </summary>
    private void RequestNextSelection()
    {
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
    /// Scene视图显示侧面检测范围
    /// Sceneビューに側面判定範囲を表示する
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (childBlocks == null)
            return;

        Gizmos.color = Color.cyan;

        foreach (Transform child in childBlocks)
        {
            if (child == null)
                continue;

            Vector2 leftCenter =
                (Vector2)child.position +
                Vector2.left * (0.25f + sideCheckDistance);

            Vector2 rightCenter =
                (Vector2)child.position +
                Vector2.right * (0.25f + sideCheckDistance);

            Vector2 size = new Vector2(
                sideCheckWidth,
                sideCheckHeight
            );

            Gizmos.DrawWireCube(leftCenter, size);
            Gizmos.DrawWireCube(rightCenter, size);
        }
    }
}