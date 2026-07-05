using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
令方块依照设定获得充能。
充能条件：与地面接触 或者 与已充能的方块接触

ブロックが設定された条件に従って充電される。
充電条件：地面に接触するか、または既に充電されたブロックに接触する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/05

---------------------------------------
*/

public class PowerBlock : MonoBehaviour
{
    [Header("Power Settings")]
    // 是否已经通电
    // すでに通電しているか
    public bool isPowered = false;

    // 子方块尺寸
    // 子ブロックのサイズ
    public Vector2 cellSize = new Vector2(0.5f, 0.5f);

    // 检测距离
    // 判定距離
    public float checkDistance = 0.05f;

    // 检测目标Layer：Ground + PowerBlock
    // 判定対象Layer：Ground + PowerBlock
    public LayerMask powerSourceLayer;

    [Header("Layer Names")]
    // 通电后的Layer名
    // 通電後のLayer名
    public string poweredLayerName = "PowerBlock";

    // 所有子方块
    // すべての子ブロック
    private Transform[] childCells;

    private void Awake()
    {
        // 自动获取所有子物体作为子方块
        // すべての子オブジェクトを子ブロックとして自動取得する
        childCells = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            childCells[i] = transform.GetChild(i);
        }
    }

    private void Update()
    {
        // 已通电后不重复检测
        // 通電済みなら再判定しない
        if (isPowered)
            return;

        CheckPower();
    }

    private void CheckPower()
    {
        // 世界坐标四方向
        // ワールド座標の四方向
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Transform cell in childCells)
        {
            foreach (Vector2 dir in directions)
            {
                // 从每个子方块向四周检测
                // 各子ブロックから四方向を判定する
                Vector2 checkPos = (Vector2)cell.position + dir * (cellSize.x / 2f + checkDistance);

                // 检测盒大小
                // 判定ボックスサイズ
                Vector2 checkSize;

                if (dir == Vector2.left || dir == Vector2.right)
                {
                    checkSize = new Vector2(checkDistance * 2f, cellSize.y * 0.8f);
                }
                else
                {
                    checkSize = new Vector2(cellSize.x * 0.8f, checkDistance * 2f);
                }

                Collider2D hit = Physics2D.OverlapBox(
                    checkPos,
                    checkSize,
                    0f,
                    powerSourceLayer
                );

                if (hit == null)
                    continue;

                // 避免检测到自己的子方块
                // 自分自身の子ブロックを検出しないようにする
                if (hit.transform.IsChildOf(transform))
                    continue;

                PowerOn();
                return;
            }
        }
    }

    private void PowerOn()
    {
        // 已经通电则不重复执行
        // すでに通電している場合は処理しない
        if (isPowered)
            return;

        isPowered = true;

        // 修改整个方块的Layer
        // ブロック全体のLayerを変更する
        SetLayerRecursively(gameObject, LayerMask.NameToLayer(poweredLayerName));

        Debug.Log("Power On / 通電");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        // 修改当前物体Layer
        // 現在のオブジェクトのLayerを変更する
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            // 修改子物体Layer
            // 子オブジェクトのLayerを変更する
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Transform cell in transform)
        {
            foreach (Vector2 dir in directions)
            {
                Vector2 checkPos = (Vector2)cell.position + dir * (cellSize.x / 2f + checkDistance);

                Vector2 checkSize;

                if (dir == Vector2.left || dir == Vector2.right)
                {
                    checkSize = new Vector2(checkDistance * 2f, cellSize.y * 0.8f);
                }
                else
                {
                    checkSize = new Vector2(cellSize.x * 0.8f, checkDistance * 2f);
                }

                Gizmos.DrawWireCube(checkPos, checkSize);
            }
        }
    }
}
