using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
管理普通、特殊及最终阶段的方块选择流程。
防止重复选择请求以及卡牌动画之间的冲突。

通常・特殊・最終段階のブロック選択を管理する。
重複した選択要求とカードアニメーションの競合を防ぐ。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class BlockSelectionFlowManager : MonoBehaviour
{
    [Header("References")]

    // 方块选择UI
    // ブロック選択UI
    [SerializeField]
    private GameObject cardSelectionUI;

    // 卡牌选择控制器
    // カード選択コントローラー
    [SerializeField]
    private CardSelector cardSelector;

    // 卡牌动画控制器
    // カードアニメーションコントローラー
    [SerializeField]
    private CardEnterController cardEnterController;


    [Header("Pause Settings")]

    // 选择期间是否暂停游戏
    // 選択中にゲームを停止するか
    [SerializeField]
    private bool pauseGameDuringSelection = true;


    [Header("Next Selection")]

    // 下一次选择类型
    // 次回の選択タイプ
    [SerializeField]
    private BlockSelectionType nextSelectionType =
        BlockSelectionType.Normal;


    [Header("Final Phase")]

    // 是否进入最终阶段
    // 最終段階に入っているか
    [SerializeField]
    private bool isFinalPhase;


    // 当前选择类型
    // 現在の選択タイプ
    public BlockSelectionType CurrentSelectionType
    {
        get;
        private set;
    } = BlockSelectionType.None;


    // 是否正在选择方块
    // ブロック選択中か
    public bool IsSelectingBlock =>
        CurrentSelectionType != BlockSelectionType.None;


    // 最终阶段状态
    // 最終段階の状態
    public bool IsFinalPhase => isFinalPhase;


    // 是否正在执行卡牌退场动画
    // カード退場アニメーション中か
    private bool isClosingSelection = false;

    // 等待执行的选择请求
    // 実行待ちの選択要求
    private BlockSelectionType pendingSelectionType =
        BlockSelectionType.None;

    // 是否由本脚本暂停游戏
    // このスクリプトがゲームを停止したか
    private bool selectionOwnsPause = false;


    private void Start()
    {
        // 最终阶段优先
        // 最終段階を優先する
        if (isFinalPhase ||
            nextSelectionType == BlockSelectionType.Final)
        {
            EnterFinalPhase();
            RequestFinalSelection();
        }
        else
        {
            RequestNormalSelection();
        }
    }


    /// <summary>
    /// 请求普通选择
    /// 通常選択を要求する
    /// </summary>
    public void RequestNormalSelection()
    {
        if (isFinalPhase)
            return;

        RequestSelection(BlockSelectionType.Normal);
    }


    /// <summary>
    /// 请求特殊选择
    /// 特殊選択を要求する
    /// </summary>
    public void RequestSpecialSelection()
    {
        if (isFinalPhase)
            return;

        RequestSelection(BlockSelectionType.Special);
    }


    /// <summary>
    /// 请求最终选择
    /// 最終選択を要求する
    /// </summary>
    public void RequestFinalSelection()
    {
        isFinalPhase = true;
        nextSelectionType = BlockSelectionType.Final;

        RequestSelection(BlockSelectionType.Final);
    }


    /// <summary>
    /// 统一处理选择请求
    /// 選択要求を統一して処理する
    /// </summary>
    private void RequestSelection(
        BlockSelectionType requestedType)
    {
        if (requestedType == BlockSelectionType.None)
            return;

        // 最终阶段只允许最终选择
        // 最終段階では最終選択のみ許可
        if (isFinalPhase &&
            requestedType != BlockSelectionType.Final)
        {
            return;
        }

        // 相同选择不重复执行
        // 同じ選択を重複実行しない
        if (CurrentSelectionType == requestedType)
            return;

        // 特殊选择期间不接受普通选择
        // 特殊選択中は通常選択を受け付けない
        if (CurrentSelectionType == BlockSelectionType.Special &&
            requestedType == BlockSelectionType.Normal)
        {
            return;
        }

        // 最终选择期间不接受其他选择
        // 最終選択中は他の選択を受け付けない
        if (CurrentSelectionType == BlockSelectionType.Final &&
            requestedType != BlockSelectionType.Final)
        {
            return;
        }

        // 卡牌正在显示或退场时，不立即刷新UI
        // カード表示中・退場中はすぐに更新しない
        if (IsSelectingBlock || isClosingSelection)
        {
            QueueSelection(requestedType);
            return;
        }

        OpenSelection(requestedType);
    }


    /// <summary>
    /// 保存等待中的选择请求
    /// 選択要求を保留する
    /// </summary>
    private void QueueSelection(
        BlockSelectionType requestedType)
    {
        // 最终选择拥有最高优先级
        // 最終選択が最優先
        if (isFinalPhase)
        {
            pendingSelectionType = BlockSelectionType.Final;
            return;
        }

        // 特殊选择优先于普通选择
        // 特殊選択は通常選択より優先
        if (pendingSelectionType == BlockSelectionType.Special &&
            requestedType == BlockSelectionType.Normal)
        {
            return;
        }

        pendingSelectionType = requestedType;
    }


    /// <summary>
    /// 开启选择界面
    /// 選択画面を開く
    /// </summary>
    private void OpenSelection(
        BlockSelectionType selectionType)
    {
        if (selectionType == BlockSelectionType.None)
            return;

        // 退场期间禁止开启新选择
        // 退場中は新しい選択を開始しない
        if (isClosingSelection)
        {
            QueueSelection(selectionType);
            return;
        }

        CurrentSelectionType = selectionType;

        if (cardSelectionUI != null)
        {
            cardSelectionUI.SetActive(true);
        }

        // 开始卡牌入场动画
        // カード登場アニメーション開始
        if (cardEnterController != null)
        {
            cardEnterController.ShowCardSelection();
        }

        if (cardSelector != null)
        {
            // 刷新对应卡池
            // 対応するカードプールを更新
            cardSelector.RefreshCards(selectionType);

            // 开启卡牌输入
            // カード入力を有効化
            cardSelector.SetInputEnabled(true);
        }

        if (pauseGameDuringSelection)
        {
            Time.timeScale = 0f;
            GameStateManager.SetPaused(true);

            selectionOwnsPause = true;
        }
    }


    /// <summary>
    /// 完成当前选择
    /// 現在の選択を完了する
    /// </summary>
    public void CompleteSelection()
    {
        if (!IsSelectingBlock || isClosingSelection)
            return;

        CloseSelection();
    }


    /// <summary>
    /// 关闭选择并进入退场阶段
    /// 選択を終了して退場段階へ移行
    /// </summary>
    private void CloseSelection()
    {
        // 立即进入退场状态，防止重复处理
        // 直ちに退場状態へ移行して重複処理を防ぐ
        isClosingSelection = true;

        if (cardSelector != null)
        {
            cardSelector.SetInputEnabled(false);
        }

        // 不立即关闭UI，等待退场动画完成
        // 退場アニメーション完了までUIを維持する

        CurrentSelectionType = BlockSelectionType.None;

        ResumeSelectionPause();
    }


    /// <summary>
    /// 卡牌退场动画结束时调用
    /// カード退場アニメーション完了時に呼び出す
    /// </summary>
    public void NotifySelectionExitCompleted()
    {
        if (!isClosingSelection)
            return;

        isClosingSelection = false;

        // 如果存在等待中的请求，开始下一轮选择
        // 保留された要求があれば次の選択を開始
        if (pendingSelectionType != BlockSelectionType.None)
        {
            BlockSelectionType requestedType =
                pendingSelectionType;

            pendingSelectionType = BlockSelectionType.None;

            // 最终阶段始终优先
            // 最終段階を常に優先する
            if (isFinalPhase)
            {
                requestedType = BlockSelectionType.Final;
            }

            RequestSelection(requestedType);
        }
    }


    /// <summary>
    /// 当前方块落地
    /// 現在のブロックが着地
    /// </summary>
    public void OnCurrentBlockLanded()
    {
        // 正在选择其他方块时忽略重复通知
        // 他のブロックを選択中は重複通知を無視
        if (IsSelectingBlock)
            return;

        RequestNextSelection();
    }


    /// <summary>
    /// 当前方块被摧毁
    /// 現在のブロックが削除された
    /// </summary>
    public void OnCurrentBlockDestroyed()
    {
        if (IsSelectingBlock)
            return;

        RequestNextSelection();
    }


    /// <summary>
    /// 请求下一次选择
    /// 次回の選択を要求する
    /// </summary>
    private void RequestNextSelection()
    {
        if (isFinalPhase)
        {
            RequestFinalSelection();
            return;
        }

        switch (nextSelectionType)
        {
            case BlockSelectionType.Normal:
                RequestNormalSelection();
                break;

            case BlockSelectionType.Special:

                // 特殊选择只执行一次
                // 特殊選択は一度のみ実行する
                nextSelectionType = BlockSelectionType.Normal;

                RequestSpecialSelection();
                break;

            case BlockSelectionType.Final:
                EnterFinalPhase();
                RequestFinalSelection();
                break;

            case BlockSelectionType.None:
                break;

            default:
                Debug.LogWarning(
                    $"Unsupported selection type: {nextSelectionType}",
                    this
                );
                break;
        }
    }


    /// <summary>
    /// 设置下一次选择类型
    /// 次回の選択タイプを設定する
    /// </summary>
    public void SetNextSelectionType(
        BlockSelectionType selectionType)
    {
        if (isFinalPhase &&
            selectionType != BlockSelectionType.Final)
        {
            return;
        }

        if (selectionType == BlockSelectionType.Final)
        {
            EnterFinalPhase();
            return;
        }

        nextSelectionType = selectionType;
    }


    /// <summary>
    /// 进入最终阶段
    /// 最終段階へ移行する
    /// </summary>
    public void EnterFinalPhase()
    {
        isFinalPhase = true;
        nextSelectionType = BlockSelectionType.Final;

        // 已排队的选择也改为最终选择
        // 保留中の選択も最終選択に変更
        if (pendingSelectionType != BlockSelectionType.None)
        {
            pendingSelectionType = BlockSelectionType.Final;
        }
    }


    /// <summary>
    /// 立即进入最终阶段并请求最终选择
    /// 最終段階へ移行して最終選択を要求
    /// </summary>
    public void EnterFinalPhaseAndOpenSelection()
    {
        EnterFinalPhase();
        RequestFinalSelection();
    }


    /// <summary>
    /// 恢复由选择流程设置的暂停
    /// 選択処理によるポーズを解除する
    /// </summary>
    private void ResumeSelectionPause()
    {
        if (!selectionOwnsPause)
            return;

        selectionOwnsPause = false;

        Time.timeScale = 1f;
        GameStateManager.SetPaused(false);
    }


    private void OnDisable()
    {
        ResumeSelectionPause();

        isClosingSelection = false;
        pendingSelectionType = BlockSelectionType.None;
        CurrentSelectionType = BlockSelectionType.None;
    }
}