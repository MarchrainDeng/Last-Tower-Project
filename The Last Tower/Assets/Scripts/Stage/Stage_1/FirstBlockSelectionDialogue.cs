using UnityEngine;

public class FirstBlockSelectionDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueTrigger nextDialogue;

    [Header("Delay")]
    [Min(0f)]
    [SerializeField] private float delay = 1f;

    private bool hasReceivedSelection;
    private bool isWaiting;
    private float remainingTime;

    /// <summary>
    /// 接收选择完成通知，仅响应第一次
    /// 選択完了通知を受け取る。初回のみ反応する
    /// </summary>
    public void OnBlockSelected()
    {
        if (hasReceivedSelection)
            return;

        if (nextDialogue == null)
        {
            Debug.LogWarning(
                "FirstBlockSelectionDialogue: Next Dialogue is missing.",
                this
            );
            return;
        }

        hasReceivedSelection = true;
        isWaiting = true;
        remainingTime = delay;
    }

    private void Update()
    {
        if (!isWaiting)
            return;

        // 暂停或其他对话显示期间，暂缓计时与播放
        // ポーズ中や別の会話表示中は、計時と再生を待機する
        if (GameStateManager.IsPaused ||
            DialogueManager.IsDialogueActive)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        if (remainingTime > 0f)
            return;

        // 管理器尚未就绪时继续等待
        // マネージャーの準備ができるまで待機する
        if (DialogueManager.Instance == null)
            return;

        // 本次教学只播放一次
        // 今回のチュートリアルでは一度だけ再生する
        isWaiting = false;

        if (nextDialogue != null)
        {
            nextDialogue.TriggerDialogue();
        }
    }
}
