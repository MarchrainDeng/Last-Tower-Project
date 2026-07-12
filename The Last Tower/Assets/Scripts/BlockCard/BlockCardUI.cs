using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
----------------------------------------
【功能 / 機能】
处理方块选择卡牌的内容显示

ブロック処理のカード選択内容の表示

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/09

---------------------------------------
*/

public class BlockCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image blockImage;
    public TMP_Text typeText;
    public GameObject highlightFrame;

    [Header("Card Pool")]
    // 这张卡牌自己的随机池
    // このカード専用のランダムプール
    public BlockOption[] cardPool;

    private BlockOption currentOption;

    public void RefreshCard()
    {
        if (cardPool == null || cardPool.Length == 0)
            return;

        int randomIndex = Random.Range(0, cardPool.Length);
        currentOption = cardPool[randomIndex];

        blockImage.sprite = currentOption.cardSprite;
        typeText.text = currentOption.typeName;

        // 保持图片宽高比
        // 画像のアスペクト比を維持する
        //blockImage.preserveAspect = true;
        blockImage.SetNativeSize();
    }

    public BlockOption GetOption()
    {
        return currentOption;
    }

    public void SetSelected(bool selected)
    {
        if (highlightFrame != null)
            highlightFrame.SetActive(selected);

        transform.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
    }
}
