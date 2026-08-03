using UnityEngine;
using UnityEngine.InputSystem;

/*
----------------------------------------
【功能 / 機能】
管理场景中的所有方块。

シーン内のすべてのブロックを管理する。

----------------------------------------
*/

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance;

    [Header("Block")]

    // 所有方块的Tag
    // ブロックのTag
    [SerializeField]
    private string blockTag = "TowerBlock";

    [Header("References")]
    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    [SerializeField] private BlockSelectionFlowManager flowManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 测试：按下方向键摧毁全部方块
        // テスト：下方向キーですべてのブロックを破壊
        if (Gamepad.current != null &&
            Gamepad.current.dpad.down.wasPressedThisFrame)
        {
            DestroyAllBlocks();
            flowManager.SetNextSelectionType(BlockSelectionType.Final);

            flowManager.RequestFinalSelection();
        }
    }

    /// <summary>
    /// 摧毁场景中的所有方块
    /// シーン内のすべてのブロックを破壊する
    /// </summary>
    public void DestroyAllBlocks()
    {
        GameObject[] blocks =
            GameObject.FindGameObjectsWithTag(blockTag);

        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
    }
}