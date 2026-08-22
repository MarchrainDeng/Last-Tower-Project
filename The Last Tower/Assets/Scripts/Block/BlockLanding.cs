using System.Collections;
using UnityEngine.InputSystem;
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

    [Header("Landing Slide")]

    [SerializeField]
    private float landingVelocityRetention = 0.4f;

    [SerializeField]
    private float landingDeceleration = 8f;

    [SerializeField]
    private float stopVelocityThreshold = 0.05f;

    private Coroutine landingSlideCoroutine;

    [Header("Landing")]

    [SerializeField]
    private float rotationLockDuration = 0.2f;

    [Header("Landing Input Push")]

    [SerializeField]
    private float landingPushForce = 0.25f;

    [SerializeField]
    private float landingInputDeadZone = 0.2f;

    [SerializeField]
    private float maximumLandingPushSpeed = 1f;

    [Header("Side Landing Check")]

    // 左右检测距离
    // 左右方向の判定距離
    [SerializeField]
    private float sideCheckDistance = 0.05f;

    // 左右检测盒宽度
    // 左右判定ボックスの幅
    [SerializeField]
    private float sideCheckWidth = 0.05f;

    // 左右检测盒高度
    // 左右判定ボックスの高さ
    [SerializeField]
    private float sideCheckHeight = 0.45f;

    [Header("Normal Block Landing Delay")]

    // 普通方块落地后，延迟多久启用重力并关闭操作
    // 通常ブロック着地後、重力を有効化して操作を停止するまでの時間
    [SerializeField]
    private float normalBlockLandingDelay = 0.2f;

    private Coroutine normalBlockLandingCoroutine;

    [Header("Rotation Lock")]

    // 落地后锁定旋转时间
    // 着地後の回転固定時間
    [SerializeField]
    private float rotationLockTime = 0.2f;

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

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;

            // 当前速度
            Vector2 velocity = rb.linearVelocity;

            // 去掉朝法线方向的速度
            float dot = Vector2.Dot(velocity, -normal);

            if (dot > 0f)
            {
                velocity += normal * dot;
            }

            rb.linearVelocity = velocity;
        }


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
            return;
        }

        // 新增：左右方向检测
        // 追加：左右方向の判定
        if (HasSideSupport())
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

        // 追加：配置したブロック数をカウント
        if (GameStatsManager.Instance != null)
            GameStatsManager.Instance.OnBlockPlaced();

        // 播放方块落地音效
        // ブロック着地効果音を再生する
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(landingSound);
        }

        GamepadVibrationManager.Instance?.PlayVibration(
            0.5f,
            0.9f,
            0.15f
        );

        if (stickyBlockJoint != null)
        {
            // 通知黏着方块已经落地
            // 粘着ブロックへ着地を通知する
            stickyBlockJoint.SetLanded(true);
        }

        FixedBlock fixedBlock =
            GetComponent<FixedBlock>();

        BlockMoveController moveController =
            GetComponent<BlockMoveController>();

        if (rb != null)
        {
            // 清除当前物理速度
            // 現在の物理速度をリセットする
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            //rb.mass = 10f;

            StartCoroutine(ChangeMaterialLater());

            PlayCameraShake();

            if (fixedBlock != null)
            {
                // Fixed方块立即停止操作
                // Fixedブロックは直ちに操作を停止する
                if (moveController != null)
                {
                    moveController.enabled = false;
                }

                // Fixed方块暂时保持Dynamic
                // Fixedブロックは一時的にDynamicを維持する
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.useFullKinematicContacts = true;

                if (!isFixingBlock)
                {
                    StartCoroutine(
                        FixBlockAfterSettled()
                    );
                }

                // Fixed方块立即开启下一次卡牌选择
                // Fixedブロックは直ちに次のカード選択を開始する
                RequestNextSelection();
            }
            else
            {
                // 普通方块延迟期间暂时不受重力影响，
                // 并继续保留玩家操作
                // 通常ブロックは遅延中、一時的に重力を無効化し、
                // プレイヤー操作を維持する
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;

                if (normalBlockLandingCoroutine != null)
                {
                    StopCoroutine(
                        normalBlockLandingCoroutine
                    );
                }

                StartCoroutine(LockRotationTemporarily());

                normalBlockLandingCoroutine =
                    StartCoroutine(
                        HandleNormalBlockLandingDelay(
                            moveController
                        )
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
            rb.mass = 10f;
        }
    }

    private IEnumerator SlowDownAfterLanding()
    {
        while (Mathf.Abs(rb.linearVelocity.x) > stopVelocityThreshold)
        {
            Vector2 velocity = rb.linearVelocity;

            // 让水平速度稳定地接近0
            // 水平方向の速度を徐々に0へ近づける
            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0f,
                landingDeceleration * Time.fixedDeltaTime
            );

            velocity.y = 0f;
            rb.linearVelocity = velocity;

            yield return new WaitForFixedUpdate();
        }

        // 最后彻底停止，避免极小速度残留
        // 最後に完全停止して微小な速度を残さない
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        landingSlideCoroutine = null;
    }

    /// <summary>
    /// 落地后暂时锁定旋转，避免碰撞造成轻微弹动
    /// 着地後しばらく回転を固定し、衝突による微小な揺れを防ぐ
    /// </summary>
    private IEnumerator LockRotationTemporarily()
    {
        // 清除角速度
        // 角速度をリセットする
        rb.angularVelocity = 0f;

        // 锁定Z轴旋转
        // Z軸回転を固定する
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        yield return new WaitForSeconds(rotationLockDuration);

        // 恢复原来的旋转限制
        // 元の回転制限を戻す
        rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>
    /// 根据落地瞬间的输入方向给予轻微水平推力
    /// 着地した瞬間の入力方向へ軽い水平力を加える
    /// </summary>
    private void ApplyLandingInputPush()
    {
        if (rb == null)
            return;

        float input = 0f;

        // 手柄左摇杆输入
        // ゲームパッドの左スティック入力
        if (Gamepad.current != null)
        {
            input = Gamepad.current.leftStick.x.ReadValue();
        }

        // 键盘输入优先
        // キーボード入力を優先する
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                input = -1f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                input = 1f;
            }
        }

        // 死区处理
        // デッドゾーン処理
        if (Mathf.Abs(input) < landingInputDeadZone)
        {
            input = 0f;
        }

        if (input == 0f)
            return;

        // 添加轻微水平冲量
        // 軽い水平方向のインパルスを加える
        rb.AddForce(
            Vector2.right * input * landingPushForce,
            ForceMode2D.Impulse
        );

        // 限制落地后的最大水平速度
        // 着地後の最大水平速度を制限する
        Vector2 velocity = rb.linearVelocity;

        velocity.x = Mathf.Clamp(
            velocity.x,
            -maximumLandingPushSpeed,
            maximumLandingPushSpeed
        );

        velocity.y = 0f;

        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// 检测左右方向一定距离内是否存在方块
    /// 左右方向の一定距離内にブロックがあるか確認する
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
            Collider2D leftHit =
                Physics2D.OverlapBox(
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
            Collider2D rightHit =
                Physics2D.OverlapBox(
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
    /// 延迟启用普通方块重力、关闭操作并开启下一次卡牌选择
    /// 通常ブロックの重力有効化、操作停止、
    /// 次回カード選択の開始を遅延する
    /// </summary>
    private IEnumerator HandleNormalBlockLandingDelay(
        BlockMoveController moveController)
    {
        yield return new WaitForSeconds(
            normalBlockLandingDelay
        );

        if (rb != null)
        {
            // 延迟结束后启用重力
            // 遅延終了後に重力を有効化する
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }

        if (moveController != null)
        {
            // 延迟结束后关闭玩家操作
            // 遅延終了後にプレイヤー操作を停止する
            moveController.enabled = false;
        }

        // 延迟结束后开启下一次卡牌选择
        // 遅延終了後に次のカード選択を開始する
        RequestNextSelection();

        normalBlockLandingCoroutine = null;
    }

    /// <summary>
    /// 通知流程管理器开启下一次卡牌选择
    /// フローマネージャーへ次のカード選択開始を通知する
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