using UnityEngine;

public class RandomTriggerArea : MonoBehaviour
{
    [Header("Trigger Settings")]

    // 可以触发区域的方块Layer
    [SerializeField]
    private LayerMask triggerLayer;

    // 检测区域大小
    [SerializeField]
    private Vector2 checkSize =
        new Vector2(0.5f, 0.5f);

    private RandomTriggerSpawner spawner;

    private bool hasTriggered;

    public void SetSpawner(
        RandomTriggerSpawner owner)
    {
        spawner = owner;
    }

    private void FixedUpdate()
    {
        if (hasTriggered)
            return;

        CheckLandedBlocks();
    }

    /// <summary>
    /// 检查范围内是否存在已落地方块
    /// 範囲内に着地済みブロックが存在するか確認する
    /// </summary>
    private void CheckLandedBlocks()
    {
        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                transform.position,
                checkSize,
                transform.eulerAngles.z,
                triggerLayer
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BlockLanding blockLanding =
                hit.GetComponentInParent<BlockLanding>();

            if (blockLanding == null)
                continue;

            // 只接受已经落地的方块
            if (!blockLanding.IsLanded)
                continue;

            TriggerArea();
            return;
        }
    }

    /// <summary>
    /// 触发区域
    /// エリアを発動する
    /// </summary>
    private void TriggerArea()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        if (spawner != null)
        {
            spawner.OnTriggerActivated(this);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            checkSize
        );

        Gizmos.matrix = oldMatrix;
    }
}