using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
统一管理普通方块选择与特殊方块选择的流程。
特殊选择的优先级高于普通选择，避免两种选择发生冲突。

通常ブロック選択と特殊ブロック選択の流れを統一管理する。
特殊選択は通常選択より優先され、
2種類の選択が競合しないようにする。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/11

【更新 / 更新】
2026/07/15：
普通选择与特殊选择统一使用SelectionType管理。
通常選択と特殊選択をSelectionTypeで統一管理する。
---------------------------------------
*/

public class BlockSelectionFlowManager : MonoBehaviour
{
    [Header("References")]
    // 方块选择UI的根对象
    // ブロック選択UIのルートオブジェクト
    [SerializeField] private GameObject cardSelectionUI;

    // 卡牌选择控制器
    // カード選択コントローラー
    [SerializeField] private CardSelector cardSelector;

    [Header("Pause Settings")]
    // 进入方块选择时是否暂停游戏时间
    // ブロック選択中にゲーム時間を停止するか
    [SerializeField] private bool pauseGameDuringSelection = true;

    // 当前正在进行的选择类型
    // 現在実行中の選択タイプ
    public BlockSelectionType CurrentSelectionType
    {
        get;
        private set;
    } = BlockSelectionType.None;

    // 当前是否正在选择方块
    // 現在ブロックを選択中か
    public bool IsSelectingBlock =>
        CurrentSelectionType != BlockSelectionType.None;

    private void Start()
    {
        // 进入场景后开启第一次普通选择
        // シーン開始後、最初の通常選択を開始する
        RequestNormalSelection();
    }

    /// <summary>
    /// 请求开启普通方块选择
    /// 通常ブロック選択の開始を要求する
    /// </summary>
    public void RequestNormalSelection()
    {
        RequestSelection(BlockSelectionType.Normal);
    }

    /// <summary>
    /// 请求开启特殊方块选择
    /// 特殊ブロック選択の開始を要求する
    /// </summary>
    public void RequestSpecialSelection()
    {
        RequestSelection(BlockSelectionType.Special);
    }

    /// <summary>
    /// 请求指定类型的方块选择
    /// 指定タイプのブロック選択を要求する
    /// </summary>
    private void RequestSelection(BlockSelectionType requestedType)
    {
        if (requestedType == BlockSelectionType.None)
            return;

        /*
         * 当前正在特殊选择时，不接受普通选择请求。
         * 特殊选择优先级高于普通选择。
         *
         * 特殊選択中は通常選択要求を受け付けない。
         * 特殊選択は通常選択より優先される。
         */
        if (CurrentSelectionType == BlockSelectionType.Special &&
            requestedType == BlockSelectionType.Normal)
        {
            return;
        }

        /*
         * 当前已经是相同类型的选择时，不重复刷新和开启。
         * 同じタイプの選択中なら、重複して開かない。
         */
        if (CurrentSelectionType == requestedType)
            return;

        /*
         * 如果普通选择已经打开，此时触发特殊选择，
         * 直接将当前界面切换成特殊选择。
         *
         * 通常選択中に特殊選択が発生した場合、
         * 現在の画面を特殊選択へ切り替える。
         */
        OpenSelection(requestedType);
    }

    /// <summary>
    /// 开启指定类型的选择界面
    /// 指定タイプの選択画面を開く
    /// </summary>
    private void OpenSelection(BlockSelectionType selectionType)
    {
        CurrentSelectionType = selectionType;

        if (cardSelectionUI != null)
        {
            cardSelectionUI.SetActive(true);
        }

        if (cardSelector != null)
        {
            // 根据选择类型刷新相应卡池
            // 選択タイプに応じたカードプールを更新する
            cardSelector.RefreshCards(selectionType);

            // 开启卡牌输入
            // カード入力を有効にする
            cardSelector.SetInputEnabled(true);
        }

        if (pauseGameDuringSelection)
        {
            // 暂停游戏时间
            // ゲーム時間を停止する
            Time.timeScale = 0f;
        }

        Debug.Log(
            $"Open Selection: {selectionType} / " +
            $"選択開始：{selectionType}"
        );
    }

    /// <summary>
    /// 玩家完成选择后调用
    /// プレイヤーが選択を完了した時に呼び出す
    /// </summary>
    public void CompleteSelection()
    {
        if (!IsSelectingBlock)
            return;

        CloseSelection();
    }

    /// <summary>
    /// 关闭当前选择界面
    /// 現在の選択画面を閉じる
    /// </summary>
    private void CloseSelection()
    {
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

        CurrentSelectionType = BlockSelectionType.None;

        if (pauseGameDuringSelection)
        {
            // 恢复游戏时间
            // ゲーム時間を再開する
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// 当前操作方块落地时调用
    /// 現在操作中のブロックが着地した時に呼び出す
    /// </summary>
    public void OnCurrentBlockLanded()
    {
        RequestNormalSelection();
    }

    /// <summary>
    /// 当前操作方块被摧毁时调用
    /// 現在操作中のブロックが削除された時に呼び出す
    /// </summary>
    public void OnCurrentBlockDestroyed()
    {
        RequestNormalSelection();
    }

    private void OnDisable()
    {
        // 防止对象被禁用或切换场景后仍保持暂停
        // 無効化やシーン切り替え後も停止状態が残らないようにする
        if (pauseGameDuringSelection)
        {
            Time.timeScale = 1f;
        }
    }
}