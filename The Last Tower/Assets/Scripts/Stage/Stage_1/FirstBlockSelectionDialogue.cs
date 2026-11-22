using UnityEngine;
using UnityEngine.Events;

public class FirstBlockSelectionDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueTrigger nextDialogue;

    [Header("Delay")]
    [Min(0f)]
    [SerializeField] private float delay = 1f;

    // 本脚本开启的第二段对话结束后触发
    // このスクリプトが開始した2回目の会話終了後に実行する
    [Header("Events")]
    [SerializeField]
    private UnityEvent onDialogueFinished = new UnityEvent();

    private bool hasReceivedSelection;
    private bool isWaiting;
    private bool isWatchingDialogue;
    private float remainingTime;

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
        if (isWatchingDialogue)
        {
            // 等待结束帧的输入锁定解除，避免确认键计入教学
            // 終了フレームの入力ロック解除を待ち、決定入力の誤判定を防ぐ
            if (DialogueManager.IsInputBlocked)
                return;

            isWatchingDialogue = false;
            onDialogueFinished?.Invoke();
            return;
        }

        if (!isWaiting ||
            GameStateManager.IsPaused ||
            DialogueManager.IsInputBlocked)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        if (remainingTime > 0f ||
            DialogueManager.Instance == null)
        {
            return;
        }

        isWaiting = false;

        if (nextDialogue != null)
        {
            nextDialogue.TriggerDialogue();

            isWatchingDialogue =
                DialogueManager.IsDialogueActive;

            if (!isWatchingDialogue)
            {
                Debug.LogWarning(
                    "Second dialogue could not start. Check Dialogue Data.",
                    this
                );
            }
        }
    }
}