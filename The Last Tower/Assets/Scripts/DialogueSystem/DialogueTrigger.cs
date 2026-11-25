using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]

    [SerializeField]
    private DialogueData dialogueData;

    [Header("Display Position")]

    // 是否显示在世界目标附近
    // ワールドターゲット付近に表示するか
    [SerializeField]
    private bool useWorldPosition = false;

    [SerializeField]
    private Vector2 screenPosition =
        new Vector2(0.5f, 0.18f);

    [SerializeField]
    private Transform worldTarget;

    [Header("Optional Slide Effect")]

    // 留空时不使用入场和出场动画
    // 未設定の場合は登場・退場アニメーションを使用しない
    [SerializeField]
    private DialogueSlideEffect slideEffect;

    public void TriggerDialogue()
    {
        DialogueManager manager = DialogueManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("没有找到DialogueManager。", this);
            return;
        }

        // 临时检查实际触发物体及动画组件的状态
        // 実際のトリガーとアニメーションの状態を一時確認する
        if (slideEffect == null)
        {
            Debug.LogWarning(
                $"[触发检查] 触发物体：{name}，Slide Effect为空。",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[触发检查] 触发物体：{name}，" +
                $"动画物体：{slideEffect.name}，" +
                $"动画所在场景：{slideEffect.gameObject.scene.name}，" +
                $"CanAnimate：{slideEffect.CanAnimate}",
                this
            );
        }

        if (useWorldPosition)
        {
            Transform target =
                worldTarget != null ? worldTarget : transform;

            // 必须传入第三个参数slideEffect
            // 第3引数のslideEffectを渡す
            manager.ShowDialogue(
                dialogueData,
                target,
                slideEffect
            );
        }
        else
        {
            // 必须传入第三个参数slideEffect
            // 第3引数のslideEffectを渡す
            manager.ShowDialogue(
                dialogueData,
                screenPosition,
                slideEffect
            );
        }
    }
}