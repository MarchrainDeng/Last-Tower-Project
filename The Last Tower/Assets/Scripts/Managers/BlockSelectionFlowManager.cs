using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
控制卡牌选择UI界面的流程

カード選択のUIインターフェースを制御するプロセス

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/11

---------------------------------------
*/

public class BlockSelectionFlowManager : MonoBehaviour
{
    [Header("References")]
    // 方块选择界面的根对象
    // ブロック選択UIのルートオブジェクト
    [SerializeField] private GameObject cardSelectionUI;

    // 卡牌选择控制器
    // カード選択コントローラー
    [SerializeField] private CardSelector cardSelector;

    [Header("Pause Settings")]
    // 进入方块选择时是否暂停游戏时间
    // ブロック選択中にゲーム時間を停止するか
    [SerializeField] private bool pauseGameDuringSelection = true;

    // 当前是否正在选择方块
    // 現在ブロックを選択中か
    public bool IsSelectingBlock { get; private set; }

    private void Start()
    {
        // 进入场景后立即开始第一次方块选择
        // シーン開始後、最初のブロック選択を開始する
        OpenBlockSelection();
    }

    /// <summary>
    /// 打开方块选择界面
    /// ブロック選択画面を開く
    /// </summary>
    public void OpenBlockSelection()
    {
        // 防止重复开启
        // 重複して開かないようにする
        if (IsSelectingBlock)
            return;

        IsSelectingBlock = true;

        if (cardSelectionUI != null)
        {
            cardSelectionUI.SetActive(true);
        }

        if (cardSelector != null)
        {
            // 刷新全部卡牌
            // すべてのカードを更新する
            cardSelector.RefreshCards();

            // 重新启用卡牌输入
            // カード入力を再度有効にする
            cardSelector.SetInputEnabled(true);
        }

        if (pauseGameDuringSelection)
        {
            // 暂停游戏时间
            // ゲーム時間を停止する
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// 玩家选择方块后关闭选择界面
    /// プレイヤーがブロックを選択した後、選択画面を閉じる
    /// </summary>
    public void CloseBlockSelection()
    {
        if (!IsSelectingBlock)
            return;

        IsSelectingBlock = false;

        if (cardSelector != null)
        {
            // 禁用卡牌输入
            // カード入力を無効にする
            cardSelector.SetInputEnabled(false);
        }

        if (cardSelectionUI != null)
        {
            cardSelectionUI.SetActive(false);
        }

        if (pauseGameDuringSelection)
        {
            // 恢复游戏时间
            // ゲーム時間を再開する
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// 当前方块落地时调用
    /// 現在のブロックが着地した時に呼び出す
    /// </summary>
    public void OnCurrentBlockLanded()
    {
        OpenBlockSelection();
    }

    private void OnDisable()
    {
        // 防止切换场景或禁用对象后，游戏仍保持暂停
        // シーン切り替えや無効化後も停止状態が残らないようにする
        if (pauseGameDuringSelection)
        {
            Time.timeScale = 1f;
        }
    }
}
