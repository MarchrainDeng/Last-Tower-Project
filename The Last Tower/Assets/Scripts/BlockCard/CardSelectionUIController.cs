using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CardSelectionUIController : MonoBehaviour
{
    [System.Serializable]
    public class CardSelectedEvent : UnityEvent<int>
    {
    }

    [Header("References")]
    [SerializeField] private GameObject cardPanel;
    [SerializeField] private CardUIAnimation[] cardAnimations;

    [Header("Selection Settings")]
    [SerializeField] private bool pauseGameWhileSelecting = true;

    [Header("Events")]
    [SerializeField] private CardSelectedEvent onCardSelected;

    private bool isUIOpen;
    private bool isAnimating;
    private int selectedCardIndex = -1;

    public bool IsUIOpen => isUIOpen;
    public bool IsAnimating => isAnimating;
    public int SelectedCardIndex => selectedCardIndex;

    private void Awake()
    {
        // 游戏开始时隐藏卡牌面板
        // ゲーム開始時にカードパネルを非表示にする
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示卡牌选择UI
    /// カード選択UIを表示する
    /// </summary>
    public void ShowCardSelectionUI()
    {
        if (isUIOpen || isAnimating)
            return;

        StartCoroutine(ShowCardSelectionCoroutine());
    }

    /// <summary>
    /// 确认指定卡牌，并立即执行选择结果
    /// 指定カードを確定し、選択結果を即座に実行する
    /// </summary>
    public void ConfirmCardSelection(int cardIndex)
    {
        if (!isUIOpen)
            return;

        if (isAnimating)
            return;

        if (cardAnimations == null ||
            cardIndex < 0 ||
            cardIndex >= cardAnimations.Length)
        {
            Debug.LogWarning(
                $"Invalid card index: {cardIndex}"
            );

            return;
        }

        selectedCardIndex = cardIndex;
        isUIOpen = false;
        isAnimating = true;

        // 选择完成后立刻通知生成系统
        // 選択完了後、即座に生成システムへ通知する
        //onCardSelected?.Invoke(cardIndex);

        // 生成完成后继续播放卡牌退出动画
        // 生成後もカードの退場アニメーションを再生する
        StartCoroutine(
            ConfirmCardSelectionCoroutine(cardIndex)
        );
    }

    /// <summary>
    /// 三张卡牌同时从上方落下
    /// 3枚のカードを同時に上から落下させる
    /// </summary>
    private IEnumerator ShowCardSelectionCoroutine()
    {
        isAnimating = true;
        selectedCardIndex = -1;

        if (cardPanel == null)
        {
            Debug.LogError(
                "CardPanel is not assigned."
            );

            isAnimating = false;
            yield break;
        }

        if (cardAnimations == null ||
            cardAnimations.Length == 0)
        {
            Debug.LogError(
                "Card animations are not assigned."
            );

            isAnimating = false;
            yield break;
        }

        cardPanel.SetActive(true);

        // 暂停游戏
        // ゲームを一時停止する
        if (pauseGameWhileSelecting)
        {
            Time.timeScale = 0f;
        }

        // 保存卡牌正常位置
        // カードの通常位置を保存する
        foreach (CardUIAnimation card in cardAnimations)
        {
            if (card == null)
                continue;

            card.SaveNormalPosition();
        }

        // 将卡牌移动到屏幕上方
        // カードを画面上部へ移動する
        foreach (CardUIAnimation card in cardAnimations)
        {
            if (card == null)
                continue;

            card.SetEnterStartPosition();
        }

        // 三张卡牌同时开始下落
        // 3枚のカードを同時に落下させる
        float longestEnterDuration = 0f;

        foreach (CardUIAnimation card in cardAnimations)
        {
            if (card == null)
                continue;

            StartCoroutine(
                card.PlayEnterAnimation()
            );

            longestEnterDuration = Mathf.Max(
                longestEnterDuration,
                card.EnterDuration
            );
        }

        // 等待所有卡牌完成进入动画
        // すべてのカードの登場完了を待つ
        yield return new WaitForSecondsRealtime(
            longestEnterDuration
        );

        isAnimating = false;
        isUIOpen = true;
    }

    /// <summary>
    /// 播放确认选择后的退出动画
    /// 選択確定後の退場アニメーションを再生する
    /// </summary>
    private IEnumerator ConfirmCardSelectionCoroutine(
        int selectedIndex
    )
    {
        CardUIAnimation selectedCard =
            cardAnimations[selectedIndex];

        if (selectedCard == null)
        {
            Debug.LogError(
                $"Card animation at index {selectedIndex} is null."
            );

            FinishCardSelection();
            yield break;
        }

        // 选中的卡牌先向下移动，再向上离开
        // 選択カードを下へ動かしてから上へ退場させる
        yield return selectedCard.PlaySelectedExitAnimation();

        // 选中的卡牌离开后，其他卡牌同时向上离开
        // 選択カードの退場後、他のカードを同時に退場させる
        float longestNormalExitDuration = 0f;

        for (int i = 0; i < cardAnimations.Length; i++)
        {
            if (i == selectedIndex)
                continue;

            CardUIAnimation card = cardAnimations[i];

            if (card == null)
                continue;

            StartCoroutine(
                card.PlayNormalExitAnimation()
            );

            longestNormalExitDuration = Mathf.Max(
                longestNormalExitDuration,
                card.NormalExitDuration
            );
        }

        // 等待其他卡牌退出
        // 他のカードの退場完了を待つ
        yield return new WaitForSecondsRealtime(
            longestNormalExitDuration
        );

        FinishCardSelection();
    }

    /// <summary>
    /// 完成卡牌选择流程
    /// カード選択処理を終了する
    /// </summary>
    private void FinishCardSelection()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }

        // 恢复游戏时间
        // ゲーム時間を再開する
        if (pauseGameWhileSelecting)
        {
            Time.timeScale = 1f;
        }

        isAnimating = false;
    }

    /// <summary>
    /// 强制关闭卡牌UI
    /// カードUIを強制的に閉じる
    /// </summary>
    public void ForceCloseCardSelectionUI()
    {
        StopAllCoroutines();

        isUIOpen = false;
        isAnimating = false;
        selectedCardIndex = -1;

        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }

        if (pauseGameWhileSelecting)
        {
            Time.timeScale = 1f;
        }
    }
}