using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDialogue",
    menuName = "Final Tower/Dialogue Data"
)]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Lines")]

    // 按顺序显示的对话文本
    // 順番に表示するセリフ
    [TextArea(2, 5)]
    public string[] lines;
}