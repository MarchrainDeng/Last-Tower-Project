using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
令攻击方块和已充能方块接触时，进入可以攻击的状态。
上下方向与左右方向的检测范围可以分别调整，
并且检测范围会跟随方块旋转。

无法攻击时使用未充能图像。
可以攻击时切换为已充能图像，
并额外显示枪的图像。

攻撃ブロックと通電済みブロックが接触すると、
攻撃可能な状態になる。
上下方向と左右方向の判定範囲を個別に調整でき、
判定範囲はブロックの回転に追従する。

攻撃不可の場合は非通電画像を使用する。
攻撃可能の場合は通電画像へ切り替え、
さらに銃の画像を表示する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/05
---------------------------------------
*/

public class AttackBlockState : MonoBehaviour
{
    [Header("Attack State")]

    // 是否可以攻击
    // 攻撃可能かどうか
    public bool canAttack = false;

    [Header("Visual Settings")]

    // 无法攻击时使用的图像
    // 攻撃不可時に使用する画像
    public Sprite inactiveSprite;

    // 可以攻击时使用的图像
    // 攻撃可能時に使用する画像
    public Sprite activeSprite;

    // 枪的图像对象
    // 銃画像のオブジェクト
    public GameObject gunVisual;

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

    [Header("Layer Settings")]

    // 已充能方块Layer
    // 通電済みブロックのLayer
    public LayerMask poweredBlockLayer;

    // 所有子方块
    // すべての子ブロック
    private Transform[] childCells;

    // 所有子方块上的SpriteRenderer
    // すべての子ブロックのSpriteRenderer
    private SpriteRenderer[] childSpriteRenderers;

    private void Awake()
    {
        // 自动获取所有直接子物体作为子方块
        // すべての直接子オブジェクトを子ブロックとして自動取得する
        childCells = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            childCells[i] = transform.GetChild(i);
        }

        // 获取所有子物体中的SpriteRenderer
        // すべての子オブジェクト内のSpriteRendererを取得する
        childSpriteRenderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        // 初始化显示状态
        // 表示状態を初期化する
        UpdateVisualState();
    }

    private void Update()
    {
        CheckAttackState();
    }

    /// <summary>
    /// 检测是否接触已充能方块
    /// 通電済みブロックに接触しているか判定する
    /// </summary>
    private void CheckAttackState()
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
                SetAttackState(true);
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
                SetAttackState(true);
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
                SetAttackState(true);
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
                SetAttackState(true);
                return;
            }
        }

        // 没有检测到已充能方块
        // 通電済みブロックを検出しなかった
        SetAttackState(false);
    }

    /// <summary>
    /// 检测子方块指定局部方向的区域
    /// 子ブロックの指定ローカル方向のエリアを判定する
    /// </summary>
    private bool CheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        // 将子方块的局部方向转换成世界方向
        // 子ブロックのローカル方向をワールド方向へ変換する
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        // 计算检测盒中心位置
        // 判定ボックスの中心位置を計算する
        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        // 检测盒跟随子方块旋转
        // 判定ボックスを子ブロックの回転に追従させる
        float checkAngle = cell.eulerAngles.z;

        // 检测范围内所有已充能方块Collider
        // 判定範囲内のすべての通電済みブロックColliderを取得する
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            checkPosition,
            checkSize,
            checkAngle,
            poweredBlockLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            // 排除当前攻击方块自身以及自身子物体
            // 現在の攻撃ブロック自身と子オブジェクトを除外する
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 设置攻击状态
    /// 攻撃状態を設定する
    /// </summary>
    private void SetAttackState(bool state)
    {
        if (canAttack == state)
            return;

        canAttack = state;

        // 更新图像显示
        // 画像表示を更新する
        UpdateVisualState();

        if (canAttack)
        {
            Debug.Log("Attack Block Ready / 攻撃ブロック起動");
        }
        else
        {
            Debug.Log("Attack Block Disabled / 攻撃ブロック停止");
        }
    }

    /// <summary>
    /// 根据攻击状态更新图像
    /// 攻撃状態に応じて画像を更新する
    /// </summary>
    private void UpdateVisualState()
    {
        Sprite targetSprite =
            canAttack ? activeSprite : inactiveSprite;

        foreach (SpriteRenderer spriteRenderer in childSpriteRenderers)
        {
            if (spriteRenderer == null)
                continue;

            // 避免修改枪图像本身
            // 銃画像自身のSpriteを変更しない
            if (gunVisual != null &&
                spriteRenderer.transform.IsChildOf(gunVisual.transform))
            {
                continue;
            }

            spriteRenderer.sprite = targetSprite;
        }

        // 可以攻击时显示枪，否则隐藏
        // 攻撃可能時のみ銃を表示する
        if (gunVisual != null)
        {
            gunVisual.SetActive(canAttack);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

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
    /// 绘制指定方向的检测范围
    /// 指定方向の判定範囲を描画する
    /// </summary>
    private void DrawCheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        // 将局部方向转换成世界方向
        // ローカル方向をワールド方向へ変換する
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Gizmo跟随子方块的位置和旋转
        // Gizmoを子ブロックの位置と回転に追従させる
        Gizmos.matrix = Matrix4x4.TRS(
            checkPosition,
            Quaternion.Euler(0f, 0f, cell.eulerAngles.z),
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, checkSize);

        Gizmos.matrix = oldMatrix;
    }
}