using UnityEngine;

[RequireComponent(typeof(DialogueTrigger))]
public class SceneStartDialogue : MonoBehaviour
{
    [Header("Dialogue Dimmer / 对话黑幕 / 会話用暗幕")]
    [SerializeField] private GameObject dialogueDimmer;

    private bool isWatchingDialogue;

    private void Awake()
    {
        // 初始隐藏黑幕
        // 初期状態では暗幕を非表示にする
        if (dialogueDimmer != null)
        {
            dialogueDimmer.SetActive(false);
        }
    }

    private void Start()
    {
        // 检查对话管理器
        // 会話マネージャーを確認する
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning(
                "SceneStartDialogue: DialogueManager is missing.",
                this
            );
            return;
        }

        // 已有其他对话时，不接管该对话的黑幕
        // 別の会話が表示中の場合、その会話の暗幕は制御しない
        if (DialogueManager.IsDialogueActive)
        {
            return;
        }

        // 播放开场对话
        // 開始時の会話を再生する
        GetComponent<DialogueTrigger>().TriggerDialogue();

        // 只有成功开启对话后，才显示黑幕
        // 会話が開始された場合のみ暗幕を表示する
        isWatchingDialogue = DialogueManager.IsDialogueActive;

        if (dialogueDimmer != null)
        {
            dialogueDimmer.SetActive(isWatchingDialogue);
        }
    }

    private void Update()
    {
        if (!isWatchingDialogue)
        {
            return;
        }

        if (DialogueManager.IsDialogueActive)
        {
            return;
        }

        // 本次对话结束后隐藏黑幕，不再监听后续对话
        // 今回の会話終了後に暗幕を隠し、以降の会話は監視しない
        isWatchingDialogue = false;

        if (dialogueDimmer != null)
        {
            dialogueDimmer.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 触发物体被禁用时，也清理黑幕
        // トリガーが無効になった場合も暗幕を非表示にする
        isWatchingDialogue = false;

        if (dialogueDimmer != null)
        {
            dialogueDimmer.SetActive(false);
        }
    }
}