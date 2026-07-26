using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BlockGroup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Merge Settings")]
    [SerializeField] private bool disableOtherMoveController = true;
    [SerializeField] private bool preserveWorldPosition = true;

    private bool isMerging;
    private BlockGroup rootGroup;

    public Rigidbody2D Rigidbody => GetRootGroup().rb;
    public bool IsMerging => GetRootGroup().isMerging;
    public BlockGroup RootGroup => GetRootGroup();

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        rootGroup = this;
    }

    /// <summary>
    /// 获取当前物理整体的根方块组
    /// 現在の物理グループのルートを取得する
    /// </summary>
    public BlockGroup GetRootGroup()
    {
        if (rootGroup == null)
        {
            rootGroup = this;
        }

        if (rootGroup != this)
        {
            rootGroup = rootGroup.GetRootGroup();
        }

        return rootGroup;
    }

    /// <summary>
    /// 将另一个完整方块挂到当前方块组下面
    /// 別のブロック全体を現在のグループの子にする
    /// </summary>
    public void MergeWholeBlock(BlockGroup otherGroup)
    {
        BlockGroup currentRoot = GetRootGroup();

        if (currentRoot != this)
        {
            currentRoot.MergeWholeBlock(otherGroup);
            return;
        }

        if (otherGroup == null)
            return;

        BlockGroup otherRoot = otherGroup.GetRootGroup();

        if (otherRoot == null)
            return;

        if (otherRoot == currentRoot)
            return;

        if (currentRoot.isMerging || otherRoot.isMerging)
            return;

        StartCoroutine(
            MergeWholeBlockCoroutine(otherRoot)
        );
    }

    /// <summary>
    /// 执行完整方块合并
    /// ブロック全体の結合を実行する
    /// </summary>
    private IEnumerator MergeWholeBlockCoroutine(
        BlockGroup otherRoot)
    {
        isMerging = true;
        otherRoot.isMerging = true;

        Rigidbody2D otherRb =
            otherRoot.GetComponent<Rigidbody2D>();

        BlockMoveController otherMoveController =
            otherRoot.GetComponent<BlockMoveController>();

        Collider2D[] otherColliders =
            otherRoot.GetComponentsInChildren<Collider2D>(true);

        // 临时关闭对方碰撞体，避免修改层级时发生物理冲突
        // 階層変更中の物理衝突を防ぐため、一時的にColliderを無効にする
        foreach (Collider2D col in otherColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        // 停止被合并方块的独立运动
        // 結合されるブロックの独立した移動を停止する
        if (otherRb != null)
        {
            otherRb.linearVelocity = Vector2.zero;
            otherRb.angularVelocity = 0f;
            otherRb.simulated = false;
        }

        if (disableOtherMoveController &&
            otherMoveController != null)
        {
            otherMoveController.enabled = false;
        }

        // 保持整个方块的世界坐标，将其挂到黏着方块下
        // ワールド座標を維持したままブロック全体を子にする
        otherRoot.transform.SetParent(
            transform,
            preserveWorldPosition
        );

        // 删除对方自己的刚体
        // 相手側のRigidbody2Dを削除する
        if (otherRb != null)
        {
            Destroy(otherRb);
        }

        // 等待Destroy真正完成
        // Destroy処理の完了を待つ
        yield return null;

        // 现在对方所有Collider都会归属于当前根对象的Rigidbody2D
        // 相手側のColliderは現在のルートRigidbody2Dに所属する
        foreach (Collider2D col in otherColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }

        // 记录新的根方块组
        // 新しいルートグループを記録する
        otherRoot.rootGroup = this;

        // 子方块不再单独负责合并
        // 子ブロック側では個別の結合処理を行わない
        otherRoot.enabled = false;

        if (rb != null)
        {
            rb.WakeUp();
        }

        otherRoot.isMerging = false;
        isMerging = false;
    }
}