using System.Collections;
using UnityEngine;

public class BlockLanding : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private Rigidbody2D rb;
    private BlockSelectionFlowManager flowManager;
    [SerializeField] private StickyBlockJoint stickyBlockJoint;

    [Header("Landing Collision")]

    [SerializeField] private LayerMask landingLayer;

    [SerializeField]
    private bool requireSupportFromBelow = true;

    [Range(0f, 1f)]
    [SerializeField]
    private float minimumSupportNormalY = 0.5f;

    private bool isLanded;

    public Transform[] childBlocks;

    public bool IsLanded => isLanded;

    [Header("Fixed Block Settings")]

    [SerializeField] private int fixedBlockSettleFrames = 3;
    [SerializeField] private float fixedBlockMaxSpeed = 0.05f;

    private bool isFixingBlock;

    [Header("Physics Material")]

    [SerializeField] private float materialChangeDelay = 0.2f;
    [SerializeField] private PhysicsMaterial2D fallingMaterial;
    [SerializeField] private PhysicsMaterial2D landMaterial;

    private Collider2D[] colliders;

    [Header("Landing Sound")]

    [SerializeField] private AudioClip landingSound;

    [Header("Rotation Lock")]

    [SerializeField] private float rotationLockDuration = 0.2f;

    [Header("Side Landing Check")]

    [SerializeField] private float sideCheckDistance = 0.05f;
    [SerializeField] private float sideCheckWidth = 0.05f;
    [SerializeField] private float sideCheckHeight = 0.45f;

    [Header("Normal Block Landing Delay")]

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

    public void SetFlowManager(BlockSelectionFlowManager manager)
    {
        flowManager = manager;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLandFromCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryLandFromCollision(collision);
    }

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

        if (!IsInLandingLayer(otherObject.layer))
            return;

        if (otherObject.transform == transform ||
            otherObject.transform.IsChildOf(transform))
        {
            return;
        }

        if (!requireSupportFromBelow)
        {
            Land();
            return;
        }

        if (HasSupportContact(collision))
        {
            Land();
            return;
        }

        if (HasSideSupport())
        {
            Land();
        }
    }

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

    private bool IsInLandingLayer(int layer)
    {
        return (landingLayer.value & (1 << layer)) != 0;
    }

    private void Land()
    {
        if (isLanded)
            return;

        isLanded = true;

        // 在统计、黏着及普通落地处理前检查教学
        // 統計・粘着・通常着地処理の前にチュートリアルを判定する
        if (BlockOperationTutorial.Instance != null &&
            BlockOperationTutorial.Instance.TryHandleLanding(this))
        {
            return;
        }

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

        if (stickyBlockJoint != null)
        {
            stickyBlockJoint.SetLanded(true);
        }

        FixedBlock fixedBlock = GetComponent<FixedBlock>();

        BlockMoveController moveController =
            GetComponent<BlockMoveController>();

        if (rb != null)
        {
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

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.useFullKinematicContacts = true;

                if (!isFixingBlock)
                {
                    StartCoroutine(FixBlockAfterSettled());
                }

                RequestNextSelection();
            }
            else
            {
                // 普通方块保留原有落地延迟
                // 通常ブロックの着地遅延処理を維持する
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
            float currentAngularSpeed =
                Mathf.Abs(rb.angularVelocity);

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

    private IEnumerator LockRotationTemporarily()
    {
        rb.angularVelocity = 0f;

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        yield return new WaitForSeconds(rotationLockDuration);

        rb.constraints &= ~RigidbodyConstraints2D.FreezeRotation;
    }

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

        RequestNextSelection();

        normalBlockLandingCoroutine = null;
    }

    private void RequestNextSelection()
    {
        // 教学成功后，暂缓普通落地导致的选牌
        // チュートリアル成功後は通常着地による次の選択を保留する
        if (BlockOperationTutorial.Instance != null &&
            BlockOperationTutorial.Instance.BlocksNextSelection(this))
        {
            return;
        }

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