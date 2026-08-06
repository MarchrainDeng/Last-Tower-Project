using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
统一管理普通方块、特殊方块与最终阶段方块的选择流程。

特殊选择只执行一次，完成后恢复为普通选择。
进入最终阶段后，所有后续选择都固定为最终选择，
普通选择与特殊选择请求将被忽略。

通常ブロック、特殊ブロック、最終段階ブロックの
選択フローを統一管理する。

特殊選択は一度だけ実行され、その後通常選択へ戻る。
最終段階に入った後は、すべての選択を最終選択に固定し、
通常選択と特殊選択の要求を無視する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/11
----------------------------------------
*/

public class BlockSelectionFlowManager : MonoBehaviour
{
    [Header("References")]

    // 方块选择UI的根对象
    // ブロック選択UIのルートオブジェクト
    [SerializeField]
    private GameObject cardSelectionUI;

    // 卡牌选择控制器
    // カード選択コントローラー
    [SerializeField]
    private CardSelector cardSelector;

    // 卡牌入场动画控制器
    // カード登場アニメーションコントローラー
    [SerializeField]
    private CardEnterController cardEnterController;

    [Header("Pause Settings")]

    // 进入方块选择时是否暂停游戏时间
    // ブロック選択中にゲーム時間を停止するか
    [SerializeField]
    private bool pauseGameDuringSelection = true;

    [Header("Next Selection")]

    // 下一次方块落地后开启的选择类型
    // 次回の着地後に開始する選択タイプ
    [SerializeField]
    private BlockSelectionType nextSelectionType =
        BlockSelectionType.Normal;

    [Header("Final Phase")]

    // 是否已经进入最终阶段
    // 最終段階に入っているか
    [SerializeField]
    private bool isFinalPhase;

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

    // 对外提供最终阶段状态
    // 最終段階状態を外部へ提供する
    public bool IsFinalPhase => isFinalPhase;

    private void Start()
    {
        /*
         * 如果场景开始时已经设置为最终阶段，
         * 则直接开启最终选择。
         *
         * シーン開始時に既に最終段階なら、
         * 最終選択を開始する。
         */
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
    /// 请求开启普通方块选择
    /// 通常ブロック選択の開始を要求する
    /// </summary>
    public void RequestNormalSelection()
    {
        // 最终阶段中禁止普通选择
        // 最終段階では通常選択を禁止する
        if (isFinalPhase)
            return;

        RequestSelection(
            BlockSelectionType.Normal
        );
    }

    /// <summary>
    /// 请求开启特殊方块选择
    /// 特殊ブロック選択の開始を要求する
    /// </summary>
    public void RequestSpecialSelection()
    {
        // 最终阶段中禁止特殊选择
        // 最終段階では特殊選択を禁止する
        if (isFinalPhase)
            return;

        RequestSelection(
            BlockSelectionType.Special
        );
    }

    /// <summary>
    /// 请求开启最终阶段方块选择
    /// 最終段階ブロック選択の開始を要求する
    /// </summary>
    public void RequestFinalSelection()
    {
        // 请求最终选择时，同时锁定最终阶段
        // 最終選択要求時に最終段階を固定する
        isFinalPhase = true;
        nextSelectionType =
            BlockSelectionType.Final;

        RequestSelection(
            BlockSelectionType.Final
        );
    }

    /// <summary>
    /// 请求指定类型的方块选择
    /// 指定タイプのブロック選択を要求する
    /// </summary>
    private void RequestSelection(
        BlockSelectionType requestedType)
    {
        if (requestedType ==
            BlockSelectionType.None)
        {
            return;
        }

        /*
         * 最终阶段中，无论其他脚本请求什么，
         * 都只能执行最终选择。
         *
         * 最終段階では他のスクリプトが何を要求しても、
         * 最終選択のみ実行する。
         */
        if (isFinalPhase &&
            requestedType != BlockSelectionType.Final)
        {
            return;
        }

        /*
         * 当前已经是相同类型的选择时，
         * 不重复刷新和开启。
         *
         * 同じタイプの選択中なら、
         * 重複して開かない。
         */
        if (CurrentSelectionType == requestedType)
            return;

        /*
         * 特殊选择进行中时，不接受普通选择。
         *
         * 特殊選択中は通常選択を受け付けない。
         */
        if (CurrentSelectionType ==
                BlockSelectionType.Special &&
            requestedType ==
                BlockSelectionType.Normal)
        {
            return;
        }

        /*
         * 最终选择进行中时，不接受其他任何选择。
         *
         * 最終選択中は他の選択を受け付けない。
         */
        if (CurrentSelectionType ==
                BlockSelectionType.Final &&
            requestedType !=
                BlockSelectionType.Final)
        {
            return;
        }

        OpenSelection(requestedType);
    }

    /// <summary>
    /// 开启指定类型的选择界面
    /// 指定タイプの選択画面を開く
    /// </summary>
    private void OpenSelection(
        BlockSelectionType selectionType)
    {
        CurrentSelectionType = selectionType;

        if (cardSelectionUI != null)
        {
            cardSelectionUI.SetActive(true);
        }

        if (cardEnterController != null)
        {
            cardEnterController.ShowCardSelection();
        }

        if (cardSelector != null)
        {
            // 根据选择类型刷新相应卡池
            // 選択タイプに応じてカードプールを更新する
            cardSelector.RefreshCards(selectionType);

            // 开启卡牌输入
            // カード入力を有効にする
            cardSelector.SetInputEnabled(true);
        }

        if (pauseGameDuringSelection)
        {
            Time.timeScale = 0f;
            GameStateManager.SetPaused(true);
        }
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

        /*
         * 如果你的卡牌飞出动画完成后会自行隐藏UI，
         * 这里可以保持不关闭。
         *
         * カード退場アニメーション後にUIを非表示にする場合、
         * ここでは無効化しない。
         */
        // if (cardSelectionUI != null)
        // {
        //     cardSelectionUI.SetActive(false);
        // }

        CurrentSelectionType =
            BlockSelectionType.None;

        if (pauseGameDuringSelection)
        {
            Time.timeScale = 1f;
            GameStateManager.SetPaused(false);
        }
    }

    /// <summary>
    /// 当前操作方块落地时调用
    /// 現在操作中のブロックが着地した時に呼び出す
    /// </summary>
    public void OnCurrentBlockLanded()
    {
        RequestNextSelection();
    }

    /// <summary>
    /// 当前操作方块被摧毁时调用
    /// 現在操作中のブロックが削除された時に呼び出す
    /// </summary>
    public void OnCurrentBlockDestroyed()
    {
        RequestNextSelection();
    }

    /// <summary>
    /// 根据当前状态开启下一次选择
    /// 現在の状態に応じて次の選択を開始する
    /// </summary>
    private void RequestNextSelection()
    {
        /*
         * 最终阶段拥有最高优先级。
         * 即使nextSelectionType被其他脚本错误修改，
         * 也始终只开启最终选择。
         *
         * 最終段階を最優先する。
         * 他のスクリプトがnextSelectionTypeを変更しても、
         * 最終選択のみ開始する。
         */
        if (isFinalPhase)
        {
            RequestFinalSelection();
            return;
        }

        BlockSelectionType requestedType =
            nextSelectionType;

        switch (requestedType)
        {
            case BlockSelectionType.Normal:
                RequestNormalSelection();
                break;

            case BlockSelectionType.Special:
                /*
                 * 特殊选择只使用一次。
                 * 在开启前先恢复默认值，
                 * 防止特殊选择连续出现。
                 *
                 * 特殊選択は一度だけ使用する。
                 * 開始前に通常選択へ戻し、
                 * 特殊選択の連続発生を防止する。
                 */
                nextSelectionType =
                    BlockSelectionType.Normal;

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
                    $"Unsupported selection type: " +
                    $"{requestedType}",
                    this
                );
                break;
        }
    }

    /// <summary>
    /// 设置下一次落地后开启的卡牌选择类型
    /// 次回の着地後に開始するカード選択タイプを設定する
    /// </summary>
    public void SetNextSelectionType(
        BlockSelectionType selectionType)
    {
        /*
         * 已经进入最终阶段后，
         * 不允许再改回普通或特殊选择。
         *
         * 最終段階に入った後は、
         * 通常選択や特殊選択へ戻せない。
         */
        if (isFinalPhase &&
            selectionType != BlockSelectionType.Final)
        {
            return;
        }

        if (selectionType ==
            BlockSelectionType.Final)
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
        nextSelectionType =
            BlockSelectionType.Final;
    }

    /// <summary>
    /// 立即进入最终阶段并开启最终选择
    /// 即座に最終段階へ移行し、最終選択を開始する
    /// </summary>
    public void EnterFinalPhaseAndOpenSelection()
    {
        EnterFinalPhase();

        /*
         * 如果当前正在显示普通或特殊选择，
         * 这里会直接刷新成最终选择。
         *
         * 通常または特殊選択中でも、
         * 最終選択へ切り替える。
         */
        RequestFinalSelection();
    }

    private void OnDisable()
    {
        if (pauseGameDuringSelection)
        {
            Time.timeScale = 1f;
            GameStateManager.SetPaused(false);
        }
    }
}