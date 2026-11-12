using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Display Position")]

    // 是否显示在目标对象附近
    // ターゲット付近に表示するか
    [SerializeField] private bool useWorldPosition = false;

    // 指定屏幕位置
    // 画面上の表示位置
    [SerializeField]
    private Vector2 screenPosition = new Vector2(0.5f, 0.18f);

    // 世界位置目标
    // ワールド位置のターゲット
    [SerializeField] private Transform worldTarget;


    /// <summary>
    /// 显示对话，可由其他脚本或UnityEvent调用
    /// 会話を表示する。他のスクリプトやUnityEventから呼び出し可能
    /// </summary>
    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
            return;

        if (useWorldPosition)
        {
            Transform target =
                worldTarget != null ? worldTarget : transform;

            DialogueManager.Instance.ShowDialogue(
                dialogueData,
                target
            );
        }
        else
        {
            DialogueManager.Instance.ShowDialogue(
                dialogueData,
                screenPosition
            );
        }
    }
}