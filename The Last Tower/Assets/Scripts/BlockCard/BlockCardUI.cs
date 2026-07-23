using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockCardUI : MonoBehaviour
{
    [Header("UI References")]
    // 方块图片
    // ブロック画像
    [SerializeField] private Image blockImage;

    // 方块类型文字
    // ブロックタイプテキスト
    [SerializeField] private TMP_Text typeText;

    // 选中高亮边框
    // 選択中のハイライト枠
    [SerializeField] private GameObject highlightFrame;

    [Header("Normal Card Pool")]
    // 这张卡牌的普通方块池
    // このカードの通常ブロックプール
    [SerializeField] private BlockOption[] normalCardPool;

    [Header("Special Card Pool")]
    // 这张卡牌的特殊方块池
    // このカードの特殊ブロックプール
    [SerializeField] private BlockOption[] specialCardPool;

    [Header("Select Effect")]
    // 在Inspector中设置的原始缩放
    // Inspectorで設定された元のスケール
    private Vector3 originalScale;


    // 选中大小
    // 選択時のサイズ
    [Header("Select Effect")]
    // 选中时的放大倍率
    // 選択時の拡大倍率
    [SerializeField] private float selectedScaleMultiplier = 1.15f;

    // 当前卡牌实际保存的数据
    // 現在カードが保持しているデータ
    private BlockOption currentOption;

    private void Awake()
    {
        // 记录RectTransform原本的Scale
        // RectTransformの元のScaleを記録する
        originalScale = transform.localScale;
    }

    /// <summary>
    /// 根据选择类型刷新卡牌
    /// 選択タイプに応じてカードを更新する
    /// </summary>
    public void RefreshCard(BlockSelectionType selectionType)
    {
        BlockOption[] targetPool = GetPool(selectionType);

        if (targetPool == null || targetPool.Length == 0)
        {
            currentOption = null;

            Debug.LogWarning(
                $"Card pool is empty: {selectionType} / " +
                $"カードプールが空です：{selectionType}"
            );

            return;
        }

        int randomIndex = Random.Range(0, targetPool.Length);
        currentOption = targetPool[randomIndex];

        if (currentOption == null)
            return;

        if (blockImage != null)
        {
            blockImage.sprite = currentOption.cardSprite;

            // 保持图片原始比例
            // 画像のアスペクト比を維持する
            //blockImage.preserveAspect = true;

            // 使用图片原始尺寸
            // 画像の元サイズを使用する
            blockImage.SetNativeSize();
        }

        if (typeText != null)
        {
            typeText.text = currentOption.typeName;
        }
    }

    /// <summary>
    /// 获取指定选择类型对应的卡池
    /// 指定選択タイプに対応するカードプールを取得する
    /// </summary>
    private BlockOption[] GetPool(BlockSelectionType selectionType)
    {
        switch (selectionType)
        {
            case BlockSelectionType.Normal:
                return normalCardPool;

            case BlockSelectionType.Special:
                return specialCardPool;

            default:
                return null;
        }
    }

    /// <summary>
    /// 获取当前卡牌的数据
    /// 現在のカードデータを取得する
    /// </summary>
    public BlockOption GetOption()
    {
        return currentOption;
    }

    /// <summary>
    /// 设置选中状态
    /// 選択状態を設定する
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (highlightFrame != null)
        {
            highlightFrame.SetActive(selected);
        }

        // 以原始Scale为基础进行放大
        // 元のScaleを基準に拡大する
        transform.localScale = selected
            ? originalScale * selectedScaleMultiplier
            : originalScale;
    }
}