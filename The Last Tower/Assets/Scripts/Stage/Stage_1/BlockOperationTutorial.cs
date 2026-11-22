using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class BlockOperationTutorial : MonoBehaviour
{
    public static BlockOperationTutorial Instance
    {
        get;
        private set;
    }

    [Header("References")]

    [SerializeField]
    private BlockSelectionFlowManager flowManager;

    [SerializeField]
    private DialogueTrigger successDialogue;

    // 可选：显示任务进度的文本
    // 任意：タスクの進捗を表示するテキスト
    [SerializeField]
    private TMP_Text progressText;

    [Header("Required Operation Amounts")]

    // 左右移动各自需要的实际距离
    // 左右それぞれに必要な実移動距離
    [Min(0.01f)]
    [SerializeField]
    private float requiredMoveDistance = 0.5f;

    // 平滑旋转需要的累计角度
    // スムーズ回転に必要な累計角度
    [Min(0.1f)]
    [SerializeField]
    private float requiredSmoothAngle = 15f;

    // 快速下落需要的实际下降距离
    // 高速落下に必要な実際の下降距離
    [Min(0.01f)]
    [SerializeField]
    private float requiredFastFallDistance = 0.15f;

    [Header("After Success Dialogue")]

    // 成功对话结束后，是否恢复普通选牌
    // 成功会話終了後に通常のカード選択を再開するか
    [SerializeField]
    private bool resumeSelectionAfterSuccessDialogue;

    [SerializeField]
    private UnityEvent onCompleted = new UnityEvent();

    private enum Phase
    {
        WaitingForIntro,
        Practicing,
        WaitingForRetry,
        SuccessPending,
        SuccessDialogue,
        Complete
    }

    private Phase phase = Phase.WaitingForIntro;

    private BlockMoveController currentBlock;
    private BlockLanding passedBlock;

    private bool retryRequested;

    private float leftDistance;
    private float rightDistance;
    private float smoothAngle;
    private float fastFallDistance;
    private bool snapRotationDone;

    // 保存上一物理步的信息，用于检查实际位移
    // 実移動量を確認するため前の物理ステップの情報を保存する
    private bool hasPendingMovement;
    private Vector2 previousPosition;
    private float previousMoveInput;
    private bool previousFastFall;

    private bool AllDone =>
        leftDistance >= requiredMoveDistance &&
        rightDistance >= requiredMoveDistance &&
        smoothAngle >= requiredSmoothAngle &&
        snapRotationDone &&
        fastFallDistance >= requiredFastFallDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Only one BlockOperationTutorial is allowed.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        if (flowManager == null)
        {
            flowManager =
                FindFirstObjectByType<BlockSelectionFlowManager>();
        }

        if (flowManager == null || successDialogue == null)
        {
            Debug.LogError(
                "Assign Flow Manager and Success Dialogue before playing.",
                this
            );
        }

        if (progressText != null)
        {
            progressText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 第二段对话结束后调用，只启动一次
    /// 2回目の会話終了後に呼び出す。一度だけ開始する
    /// </summary>
    public void BeginTutorial()
    {
        if (!isActiveAndEnabled ||
            phase != Phase.WaitingForIntro)
        {
            return;
        }

        ResetProgress();

        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
        }

        phase = currentBlock != null
            ? Phase.Practicing
            : Phase.WaitingForRetry;

        retryRequested = false;
    }

    /// <summary>
    /// 登记新生成的可操作方块
    /// 新しく生成された操作可能なブロックを登録する
    /// </summary>
    public void RegisterBlock(BlockMoveController block)
    {
        if (!isActiveAndEnabled ||
            block == null ||
            !block.isActiveAndEnabled)
        {
            return;
        }

        BlockLanding landing =
            block.GetComponent<BlockLanding>();

        if (landing == null || landing.IsLanded)
            return;

        if (phase != Phase.WaitingForIntro &&
            phase != Phase.WaitingForRetry)
        {
            return;
        }

        if (currentBlock != null)
            return;

        currentBlock = block;

        ResetProgress();

        if (phase == Phase.WaitingForRetry)
        {
            phase = Phase.Practicing;
        }
    }

    private bool CanRecord(BlockMoveController block)
    {
        return isActiveAndEnabled &&
            phase == Phase.Practicing &&
            block != null &&
            block == currentBlock &&
            !GameStateManager.IsPaused &&
            !DialogueManager.IsInputBlocked;
    }

    /// <summary>
    /// 结合输入检查上一物理步的真实移动
    /// 入力と合わせて前の物理ステップの実移動量を確認する
    /// </summary>
    public void RecordMovementStep(
        BlockMoveController block,
        Rigidbody2D body,
        float horizontalInput,
        bool fastFall)
    {
        if (!CanRecord(block) || body == null)
            return;

        ResolveMovement(body.position);

        previousPosition = body.position;
        previousMoveInput = horizontalInput;
        previousFastFall = fastFall;

        hasPendingMovement = true;
    }

    private void ResolveMovement(Vector2 position)
    {
        if (!hasPendingMovement)
            return;

        Vector2 delta = position - previousPosition;

        // 左移输入期间，只累计实际向左移动
        // 左入力中は実際の左移動のみ加算する
        if (previousMoveInput < 0f)
        {
            leftDistance += Mathf.Max(0f, -delta.x);
        }

        // 右移输入期间，只累计实际向右移动
        // 右入力中は実際の右移動のみ加算する
        if (previousMoveInput > 0f)
        {
            rightDistance += Mathf.Max(0f, delta.x);
        }

        // 只有快速下落生效时才累计，不计自然下落
        // 高速落下が有効な場合のみ加算し、自然落下は含めない
        if (previousFastFall)
        {
            fastFallDistance += Mathf.Max(0f, -delta.y);
        }

        hasPendingMovement = false;

        RefreshProgress();
    }

    public void RecordSmoothRotation(
        BlockMoveController block,
        float angle)
    {
        if (!CanRecord(block))
            return;

        smoothAngle += Mathf.Abs(angle);

        RefreshProgress();
    }

    public void RecordSnapRotation(
        BlockMoveController block,
        float angle)
    {
        if (!CanRecord(block) ||
            Mathf.Abs(angle) < 0.01f)
        {
            return;
        }

        snapRotationDone = true;

        RefreshProgress();
    }

    /// <summary>
    /// 返回true时，跳过该方块的普通落地处理
    /// trueの場合、そのブロックの通常着地処理をスキップする
    /// </summary>
    public bool TryHandleLanding(BlockLanding landing)
    {
        if (!isActiveAndEnabled ||
            landing == null ||
            currentBlock == null ||
            landing.gameObject != currentBlock.gameObject)
        {
            return false;
        }

        if (phase != Phase.Practicing &&
            phase != Phase.WaitingForIntro)
        {
            return false;
        }

        // 补算碰撞前最后一个物理步的位移
        // 衝突前の最後の物理ステップの移動量を反映する
        Rigidbody2D body =
            currentBlock.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            ResolveMovement(body.position);
        }

        if (phase == Phase.Practicing && AllDone)
        {
            passedBlock = landing;

            // 成功落地后立即停止玩家操作
            // 成功着地後は直ちにプレイヤー操作を停止する
            currentBlock.enabled = false;

            hasPendingMovement = false;
            phase = Phase.SuccessPending;

            // 保留普通落地的音效、统计和物理处理
            // 通常着地の効果音・統計・物理処理を維持する
            return false;
        }

        FailCurrentBlock();

        return true;
    }

    /// <summary>
    /// 成功后暂缓普通落地引发的选牌
    /// 成功後は通常着地による次のカード選択を保留する
    /// </summary>
    public bool BlocksNextSelection(BlockLanding landing)
    {
        return isActiveAndEnabled &&
            landing == passedBlock &&
            (
                phase == Phase.SuccessPending ||
                phase == Phase.SuccessDialogue ||
                phase == Phase.Complete
            );
    }

    /// <summary>
    /// 越界同样按失败处理
    /// 範囲外も失敗として処理する
    /// </summary>
    public bool TryHandleLostBlock(BlockMoveController block)
    {
        if (!isActiveAndEnabled ||
            block == null ||
            block != currentBlock)
        {
            return false;
        }

        if (phase != Phase.Practicing &&
            phase != Phase.WaitingForIntro)
        {
            return false;
        }

        FailCurrentBlock();

        return true;
    }

    private void FailCurrentBlock()
    {
        GameObject failedObject = currentBlock.gameObject;

        currentBlock = null;

        ResetProgress();

        // 第二段对话前已经落地时，等对话结束再重试
        // 2回目の会話前に着地した場合は会話終了後に再試行する
        if (phase != Phase.WaitingForIntro)
        {
            phase = Phase.WaitingForRetry;
        }

        retryRequested = false;

        // 临时销毁：立即停用整个方块，帧末删除
        // 仮の削除：ブロック全体を直ちに無効化し、フレーム末に削除する
        failedObject.SetActive(false);
        Destroy(failedObject);
    }

    private void Update()
    {
        // 当前方块被其他逻辑删除时，也清零并重试
        // 別の処理で現在のブロックが削除された場合もリセットして再試行する
        if (phase == Phase.Practicing &&
            currentBlock == null)
        {
            ResetProgress();

            retryRequested = false;
            phase = Phase.WaitingForRetry;
        }

        if (phase == Phase.WaitingForRetry &&
            !retryRequested &&
            !DialogueManager.IsInputBlocked &&
            !GameStateManager.IsPaused)
        {
            if (flowManager == null)
                return;

            retryRequested = true;

            // 统一请求一次，避免落地和销毁重复通知
            // 着地と削除の二重通知を避け、一度だけ要求する
            flowManager.OnCurrentBlockDestroyed();
        }

        if (phase == Phase.SuccessPending &&
            !GameStateManager.IsPaused &&
            !DialogueManager.IsInputBlocked &&
            DialogueManager.Instance != null)
        {
            if (successDialogue == null)
                return;

            successDialogue.TriggerDialogue();

            if (DialogueManager.IsDialogueActive)
            {
                phase = Phase.SuccessDialogue;
            }
            else
            {
                Debug.LogError(
                    "Success dialogue could not start. Check Dialogue Data.",
                    this
                );

                enabled = false;
            }
        }
        else if (phase == Phase.SuccessDialogue &&
                 !DialogueManager.IsInputBlocked)
        {
            phase = Phase.Complete;

            if (progressText != null)
            {
                progressText.gameObject.SetActive(false);
            }

            if (resumeSelectionAfterSuccessDialogue &&
                flowManager != null)
            {
                flowManager.OnCurrentBlockLanded();
            }

            onCompleted?.Invoke();
        }
    }

    private void ResetProgress()
    {
        leftDistance = 0f;
        rightDistance = 0f;
        smoothAngle = 0f;
        fastFallDistance = 0f;

        snapRotationDone = false;
        hasPendingMovement = false;

        RefreshProgress();
    }

    private void RefreshProgress()
    {
        if (progressText == null)
            return;

        // 此处文本可根据实际UI语言修改
        // 実際のUI言語に合わせて表示テキストを変更できる
        progressText.text =
            $"[{Mark(leftDistance >= requiredMoveDistance)}] 左移 / 左移動\n" +
            $"[{Mark(rightDistance >= requiredMoveDistance)}] 右移 / 右移動\n" +
            $"[{Mark(smoothAngle >= requiredSmoothAngle)}] 平滑旋转 / スムーズ回転\n" +
            $"[{Mark(snapRotationDone)}] 90°旋转 / 90度回転\n" +
            $"[{Mark(fastFallDistance >= requiredFastFallDistance)}] 快速下落 / 高速落下";
    }

    private string Mark(bool done)
    {
        return done ? "OK" : "  ";
    }
}