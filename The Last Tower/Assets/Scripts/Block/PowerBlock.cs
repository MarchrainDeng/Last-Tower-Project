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

    [Header("Vertical Check Settings")]
    // 上下方向检测盒大小
    // 上下方向の判定ボックスサイズ
    public Vector2 verticalCheckSize = new Vector2(0.4f, 0.08f);

    // 上下方向检测盒距离子方块中心的偏移
    // 上下方向の判定ボックスの中心からの距離
    public float verticalCheckOffset = 0.29f;

    [Header("Horizontal Check Settings")]
    // 左右方向检测盒大小
    // 左右方向の判定ボックスサイズ
    public Vector2 horizontalCheckSize = new Vector2(0.08f, 0.4f);

    // 左右方向检测盒距离子方块中心的偏移
    // 左右方向の判定ボックスの中心からの距離
    public float horizontalCheckOffset = 0.29f;

    [Header("Power Check")]
    // 地面Layer
    // 地面Layer
    public LayerMask groundLayer;

    // 动力方块Layer
    // 動力ブロックLayer
    public LayerMask powerBlockLayer;

    // 所有子方块
    // すべての子ブロック
    private Transform[] childCells;

    [Header("Sprite")]

    // 方块图片
    // ブロック画像
    public SpriteRenderer spriteRenderer;

    // 未通电图片
    // 非通電時の画像
    public Sprite unpoweredSprite;

    // 已通电图片
    // 通電時の画像
    public Sprite poweredSprite;

    [Header("Special Settings")]

    // 是否始终保持通电
    // 常に通電状態を維持するか
    public bool alwaysPowered = false;

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

    private void Start()
    {
        UpdateSprite();

        // 永远通电的方块，游戏开始立即通电
        // 常時通電ブロックは開始時に通電する
        if (alwaysPowered)
        {
            SetPowered(true);
        }
    }

    private void Update()
    {
        if (alwaysPowered)
        {
            return;
        }

        CheckPower();
    }

    private void CheckPower()
    {
        foreach (Transform cell in childCells)
        {
            if (cell == null)
                continue;

            // 检测上方
            // 上方向を判定する
            if (CheckArea(
                cell,
                Vector2.up,
                verticalCheckOffset,
                verticalCheckSize))
            {
                SetPowered(true);
                return;
            }

            // 检测下方
            // 下方向を判定する
            if (CheckArea(
                cell,
                Vector2.down,
                verticalCheckOffset,
                verticalCheckSize))
            {
                SetPowered(true);
                return;
            }

            // 检测左侧
            // 左方向を判定する
            if (CheckArea(
                cell,
                Vector2.left,
                horizontalCheckOffset,
                horizontalCheckSize))
            {
                SetPowered(true);
                return;
            }

            // 检测右侧
            // 右方向を判定する
            if (CheckArea(
                cell,
                Vector2.right,
                horizontalCheckOffset,
                horizontalCheckSize))
            {
                SetPowered(true);
                return;
            }
        }

        // 没检测到任何有效电源时断电
        // 有効な電源を検出しなかった場合は停電する
        SetPowered(false);
    }

    /// <summary>
    /// 检测指定方向的区域
    /// 指定方向のエリアを判定する
    /// </summary>
    private bool CheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        // 把局部方向转换为世界方向
        // ローカル方向をワールド方向へ変換する
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        // 计算检测盒中心位置
        // 判定ボックスの中心位置を計算する
        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        // 检测盒跟随子方块旋转
        // 判定ボックスを子ブロックの回転に合わせる
        float angle = cell.eulerAngles.z;

        // 先检测地面
        // まず地面を判定する
        Collider2D[] groundHits = Physics2D.OverlapBoxAll(
            checkPosition,
            checkSize,
            angle,
            groundLayer
        );

        foreach (Collider2D hit in groundHits)
        {
            if (IsValidOtherCollider(hit))
            {
                return true;
            }
        }

        // 再检测其他动力方块
        // 次に他の動力ブロックを判定する
        Collider2D[] powerHits = Physics2D.OverlapBoxAll(
            checkPosition,
            checkSize,
            angle,
            powerBlockLayer
        );

        foreach (Collider2D hit in powerHits)
        {
            if (!IsValidOtherCollider(hit))
                continue;

            PowerBlock otherPowerBlock =
                hit.GetComponentInParent<PowerBlock>();

            if (otherPowerBlock == null ||
                otherPowerBlock == this)
            {
                continue;
            }

            // 只有对方已经通电时才可以传导
            // 相手が通電済みの場合のみ伝導できる
            if (otherPowerBlock.isPowered)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否为有效的外部碰撞体
    /// 有効な外部Colliderか判定する
    /// </summary>
    private bool IsValidOtherCollider(Collider2D hit)
    {
        if (hit == null)
            return false;

        // 排除自身和自身子物体
        // 自身と自身の子オブジェクトを除外する
        if (hit.transform == transform ||
            hit.transform.IsChildOf(transform))
        {
            return false;
        }

        return true;
    }

    //No longer used
    /*
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
    */

    /// <summary>
    /// 设置通电状态
    /// 通電状態を設定する
    /// </summary>
    private void SetPowered(bool powered)
    {
        // 状态没有变化时不重复处理
        // 状態が変化していない場合は処理しない
        if (isPowered == powered)
            return;

        isPowered = powered;

        if (isPowered)
        {
            // 修改整个方块的Layer
            // ブロック全体のLayerを変更する
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("PowerBlock"));
        }
        else
        {
            // 修改整个方块的Layer
            // ブロック全体のLayerを変更する
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("NoPowerBlock"));
        }

        // 根据状态更新图片
        // 状態に応じて画像を更新する
        UpdateSprite();

        Debug.Log(
            isPowered
                ? "Power On / 通電"
                : "Power Off / 停電"
        );
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

    /// <summary>
    /// 根据通电状态刷新图片
    /// 通電状態に応じて画像を更新する
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite =
            isPowered ? poweredSprite : unpoweredSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        foreach (Transform cell in transform)
        {
            if (cell == null)
                continue;

            DrawCheckArea(
                cell,
                Vector2.up,
                verticalCheckOffset,
                verticalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.down,
                verticalCheckOffset,
                verticalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.left,
                horizontalCheckOffset,
                horizontalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.right,
                horizontalCheckOffset,
                horizontalCheckSize
            );
        }
    }

    /// <summary>
    /// 绘制检测范围
    /// 判定範囲を描画する
    /// </summary>
    private void DrawCheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            checkPosition,
            Quaternion.Euler(0f, 0f, cell.eulerAngles.z),
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, checkSize);

        Gizmos.matrix = oldMatrix;
    }
}
