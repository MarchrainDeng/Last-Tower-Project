using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StickyBlockJoint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Sticky Settings")]
    [SerializeField] private LayerMask stickyTargetLayers;
    [SerializeField] private bool stickyOnlyAfterLanding = true;
    [SerializeField] private float stickyDelay = 0f;

    [Header("Joint Settings")]
    [SerializeField] private bool enableCollisionBetweenConnectedBlocks = false;
    [SerializeField] private float breakForce = Mathf.Infinity;
    [SerializeField] private float breakTorque = Mathf.Infinity;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;

    private bool hasLanded;

    // 记录已经连接的刚体，避免重复添加Joint
    // 接続済みのRigidbody2Dを記録し、Jointの重複追加を防ぐ
    private readonly HashSet<Rigidbody2D> connectedBodies =
        new HashSet<Rigidbody2D>();

    // 记录正在等待连接的刚体
    // 接続待機中のRigidbody2Dを記録する
    private readonly HashSet<Rigidbody2D> pendingBodies =
        new HashSet<Rigidbody2D>();

    public bool HasLanded => hasLanded;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        CacheExistingJoints();
    }

    /// <summary>
    /// 记录Inspector中已经存在的FixedJoint2D
    /// Inspector上に既に存在するFixedJoint2Dを記録する
    /// </summary>
    private void CacheExistingJoints()
    {
        FixedJoint2D[] joints =
            GetComponents<FixedJoint2D>();

        foreach (FixedJoint2D joint in joints)
        {
            if (joint == null)
                continue;

            if (joint.connectedBody == null)
                continue;

            connectedBodies.Add(joint.connectedBody);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStick(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 如果刚接触时尚未落地，落地后持续接触时再次检查
        // 接触開始時に未着地だった場合、着地後に再確認する
        TryStick(collision);
    }

    /// <summary>
    /// 尝试黏住接触到的方块
    /// 接触したブロックとの接続を試みる
    /// </summary>
    private void TryStick(Collision2D collision)
    {
        if (collision == null)
            return;

        if (stickyOnlyAfterLanding && !hasLanded)
            return;

        Rigidbody2D otherRb =
            collision.rigidbody;

        if (otherRb == null)
        {
            otherRb =
                collision.collider.GetComponentInParent<Rigidbody2D>();
        }

        if (otherRb == null)
            return;

        // 不能连接自己
        // 自分自身とは接続しない
        if (otherRb == rb)
            return;

        // 只黏住指定Layer中的对象
        // 指定したLayerのオブジェクトのみ接続する
        if (!IsLayerIncluded(otherRb.gameObject.layer))
            return;

        // 已经连接过
        // 既に接続済み
        if (connectedBodies.Contains(otherRb))
            return;

        // 已经进入等待连接状态
        // 既に接続待機中
        if (pendingBodies.Contains(otherRb))
            return;

        StartCoroutine(
            StickCoroutine(otherRb)
        );
    }

    /// <summary>
    /// 等待适当时机后建立FixedJoint2D
    /// 適切なタイミングでFixedJoint2Dを作成する
    /// </summary>
    private IEnumerator StickCoroutine(Rigidbody2D otherRb)
    {
        if (otherRb == null)
            yield break;

        pendingBodies.Add(otherRb);

        if (stickyDelay > 0f)
        {
            yield return new WaitForSeconds(stickyDelay);
        }
        else
        {
            // 避免直接在碰撞回调中修改物理组件
            // 衝突コールバック内で物理コンポーネントを直接変更しない
            yield return new WaitForFixedUpdate();
        }

        if (otherRb == null || rb == null)
        {
            pendingBodies.Remove(otherRb);
            yield break;
        }

        if (connectedBodies.Contains(otherRb))
        {
            pendingBodies.Remove(otherRb);
            yield break;
        }

        CreateFixedJoint(otherRb);

        pendingBodies.Remove(otherRb);
    }

    /// <summary>
    /// 创建固定关节，将两个完整方块黏在一起
    /// FixedJoint2Dを作成し、2つのブロック全体を固定する
    /// </summary>
    private void CreateFixedJoint(Rigidbody2D otherRb)
    {
        FixedJoint2D joint =
            gameObject.AddComponent<FixedJoint2D>();

        joint.connectedBody = otherRb;

        // 自动根据当前接触位置计算锚点
        // 現在の接触位置からアンカーを自動設定する
        joint.autoConfigureConnectedAnchor = true;

        // 通常关闭相连物体之间的碰撞，避免持续互相挤压和抖动
        // 接続された物体同士の押し合いや振動を防ぐ
        joint.enableCollision =
            enableCollisionBetweenConnectedBlocks;

        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;

        connectedBodies.Add(otherRb);

        // 唤醒两个刚体，立即更新Joint
        // 両方のRigidbody2Dを起こし、Jointを即座に更新する
        rb.WakeUp();
        otherRb.WakeUp();

        if (showDebugLog)
        {
            Debug.Log(
                $"{gameObject.name} stuck to {otherRb.gameObject.name}.",
                this
            );
        }
    }

    /// <summary>
    /// 设置黏性方块的落地状态
    /// 粘着ブロックの着地状態を設定する
    /// </summary>
    public void SetLanded(bool landed)
    {
        hasLanded = landed;
    }

    /// <summary>
    /// 判断对象Layer是否属于黏着目标
    /// オブジェクトのLayerが粘着対象か確認する
    /// </summary>
    private bool IsLayerIncluded(int layer)
    {
        return (
            stickyTargetLayers.value &
            (1 << layer)
        ) != 0;
    }

    /// <summary>
    /// 解除与指定方块的黏着
    /// 指定したブロックとの接続を解除する
    /// </summary>
    public void DetachBlock(Rigidbody2D targetRb)
    {
        if (targetRb == null)
            return;

        FixedJoint2D[] joints =
            GetComponents<FixedJoint2D>();

        foreach (FixedJoint2D joint in joints)
        {
            if (joint == null)
                continue;

            if (joint.connectedBody != targetRb)
                continue;

            Destroy(joint);
        }

        connectedBodies.Remove(targetRb);
        pendingBodies.Remove(targetRb);
    }

    /// <summary>
    /// 解除所有黏着关系
    /// すべての接続を解除する
    /// </summary>
    public void DetachAllBlocks()
    {
        FixedJoint2D[] joints =
            GetComponents<FixedJoint2D>();

        foreach (FixedJoint2D joint in joints)
        {
            if (joint != null)
            {
                Destroy(joint);
            }
        }

        connectedBodies.Clear();
        pendingBodies.Clear();
    }
}