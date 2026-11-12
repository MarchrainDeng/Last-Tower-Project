using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "NewDialogue",
    menuName = "Final Tower/Dialogue Data"
)]
public class DialogueData : ScriptableObject
{
    [Header("Localized Dialogue Lines")]

    // 多语言对话文本
    // 多言語対応のセリフ
    public LocalizedString[] localizedLines;


    [Header("Legacy Dialogue Lines")]

    // 旧版单语言文本，保留以兼容已有数据
    // 旧バージョンのテキストとの互換性を維持する
    [TextArea(2, 5)]
    public string[] lines;
}