using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BlockGroup))]
public class StickyBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BlockGroup blockGroup;

    [Header("Sticky Settings")]
    [SerializeField] private LayerMask blockLayerMask;
    [SerializeField] private bool stickyOnlyAfterLanding = true;
    [SerializeField] private float mergeDelay = 0f;

    private bool hasLanded;
    private bool isProcessingCollision;

    private void Awake()
    {
        if (blockGroup == null)
        {
            blockGroup = GetComponent<BlockGroup>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryMerge(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryMerge(collision);
    }

    /// <summary>
    /// 尝试合并接触到的完整方块
    /// 接触したブロック全体との結合を試みる
    /// </summary>
    private void TryMerge(Collision2D collision)
    {
        if (isProcessingCollision)
            return;

        if (stickyOnlyAfterLanding && !hasLanded)
            return;

        if (blockGroup == null)
            return;

        GameObject otherObject = collision.gameObject;

        if (!IsInBlockLayer(otherObject.layer))
            return;

        BlockGroup otherGroup =
            otherObject.GetComponentInParent<BlockGroup>();

        if (otherGroup == null)
            return;

        BlockGroup currentRoot =
            blockGroup.GetRootGroup();

        BlockGroup otherRoot =
            otherGroup.GetRootGroup();

        if (currentRoot == otherRoot)
            return;

        if (currentRoot.IsMerging ||
            otherRoot.IsMerging)
        {
            return;
        }

        StartCoroutine(
            MergeAfterPhysicsFrame(
                currentRoot,
                otherRoot
            )
        );
    }

    /// <summary>
    /// 等待物理帧结束后合并
    /// 物理フレーム終了後に結合する
    /// </summary>
    private IEnumerator MergeAfterPhysicsFrame(
        BlockGroup currentRoot,
        BlockGroup otherRoot)
    {
        isProcessingCollision = true;

        if (mergeDelay > 0f)
        {
            yield return new WaitForSeconds(
                mergeDelay
            );
        }
        else
        {
            yield return new WaitForFixedUpdate();
        }

        if (currentRoot != null &&
            otherRoot != null)
        {
            currentRoot.MergeWholeBlock(
                otherRoot
            );
        }

        yield return null;

        isProcessingCollision = false;
    }

    /// <summary>
    /// 设置落地状态
    /// 着地状態を設定する
    /// </summary>
    public void SetLanded(bool landed)
    {
        hasLanded = landed;
    }

    /// <summary>
    /// 判断图层是否可以被黏住
    /// Layerが粘着対象か確認する
    /// </summary>
    private bool IsInBlockLayer(int layer)
    {
        return (
            blockLayerMask.value &
            (1 << layer)
        ) != 0;
    }
}