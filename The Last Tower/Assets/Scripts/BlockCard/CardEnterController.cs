using System.Collections;
using UnityEngine;

public class CardEnterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cardPanel;
    [SerializeField] private RectTransform[] cards;

    [Header("Enter Animation")]
    [SerializeField] private float startOffsetY = 800f;
    [SerializeField] private float enterDuration = 0.5f;

    [Header("Selected Animation")]
    [SerializeField] private float pressDownDistance = 30f;
    [SerializeField] private float pressDownDuration = 0.08f;
    [SerializeField] private float moveUpDistance = 900f;
    [SerializeField] private float moveUpDuration = 0.25f;

    [Header("Unselected Animation")]
    [SerializeField] private float unselectedMoveUpDistance = 900f;
    [SerializeField] private float unselectedMoveUpDuration = 0.25f;

    private Vector2[] normalPositions;
    private Coroutine enterCoroutine;

    /// <summary>
    /// 当前是否允许玩家操作
    /// 現在プレイヤー操作を受け付けるか
    /// </summary>
    public bool CanAcceptInput { get; private set; }

    private void Awake()
    {
        // 创建位置数组
        // 位置配列を作成する
        normalPositions = new Vector2[cards.Length];

        // 保存每张卡牌原本的位置
        // 各カードの元の位置を保存する
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            normalPositions[i] = cards[i].anchoredPosition;
        }

        CanAcceptInput = false;

        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 进入卡牌选择状态
    /// カード選択状態に入る
    /// </summary>
    public void ShowCardSelection()
    {
        Debug.Log("进入选择");

        if (enterCoroutine != null)
        {
            StopCoroutine(enterCoroutine);
        }

        enterCoroutine = StartCoroutine(PlayEnterAnimation());
    }

    /// <summary>
    /// 播放三张卡牌同时落下的动画
    /// 3枚のカードが同時に落下するアニメーションを再生する
    /// </summary>
    private IEnumerator PlayEnterAnimation()
    {
        CanAcceptInput = false;

        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }

        // 把所有卡牌移动到原位置上方
        // すべてのカードを元の位置の上へ移動する
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            cards[i].anchoredPosition =
                normalPositions[i] + Vector2.up * startOffsetY;
        }

        Vector2[] startPositions = new Vector2[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            startPositions[i] = cards[i].anchoredPosition;
        }

        float elapsedTime = 0f;

        while (elapsedTime < enterDuration)
        {
            // 即使Time.timeScale为0，UI动画也能播放
            // Time.timeScaleが0でもUIアニメーションを再生できる
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / enterDuration
            );

            float easedProgress = EaseOutCubic(progress);

            // 三张卡牌同时移动
            // 3枚のカードを同時に移動する
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                    continue;

                cards[i].anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPositions[i],
                        normalPositions[i],
                        easedProgress
                    );
            }

            yield return null;
        }

        // 确保最后准确停在目标位置
        // 最後に正確な位置へ固定する
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            cards[i].anchoredPosition = normalPositions[i];
        }

        enterCoroutine = null;
        CanAcceptInput = true;
    }

    /// <summary>
    /// 快速开始，缓慢停止
    /// 素早く開始し、ゆっくり停止する
    /// </summary>
    private float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    /// <summary>
    /// 播放被选择卡牌的动画
    /// 選択されたカードのアニメーションを再生する
    /// </summary>
    /*
    public void PlaySelectedAnimation(int selectedIndex)
    {
        if (selectedIndex < 0 ||
            selectedIndex >= cards.Length)
        {
            return;
        }

        Debug.Log("选择动画");

        StartCoroutine(
            PlaySelectedAnimationCoroutine(selectedIndex)
        );
    }
    */

    public IEnumerator PlaySelectedAnimation(int selectedIndex)
    {
        CanAcceptInput = false;

        yield return StartCoroutine(
            PlaySelectedAnimationCoroutine(selectedIndex)
        );

        CanAcceptInput = true;
    }

    /// <summary>
    /// 被选择卡牌动画
    /// 選択されたカードのアニメーション
    /// </summary>
    public IEnumerator PlaySelectedAnimationCoroutine(int index)
    {
        RectTransform card = cards[index];

        Vector2 startPosition = normalPositions[index];

        Vector2 pressPosition =
            startPosition + Vector2.down * pressDownDistance;

        float elapsedTime = 0f;

        // 下沉
        // 下へ移動
        while (elapsedTime < pressDownDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / pressDownDuration);

            card.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    pressPosition,
                    progress
                );

            yield return null;
        }

        card.anchoredPosition = pressPosition;

        elapsedTime = 0f;

        Vector2 endPosition =
            startPosition + Vector2.up * moveUpDistance;

        // 飞向上方
        // 上方向へ移動
        while (elapsedTime < moveUpDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / moveUpDuration);

            card.anchoredPosition =
                Vector2.Lerp(
                    pressPosition,
                    endPosition,
                    progress
                );

            yield return null;
        }

        card.anchoredPosition = endPosition;
    }

    /// <summary>
    /// 播放未选择卡牌的上移动画
    /// 選択されていないカードの上移動アニメーションを再生する
    /// </summary>
    public IEnumerator PlayUnselectedCardsAnimation(int selectedIndex)
    {
        CanAcceptInput = false;

        yield return StartCoroutine(
            PlayUnselectedCardsAnimationCoroutine(selectedIndex)
        );

        CanAcceptInput = true;
    }

    /// <summary>
    /// 未选择卡牌同时向上飞出
    /// 選択されていないカードを同時に上へ移動させる
    /// </summary>
    private IEnumerator PlayUnselectedCardsAnimationCoroutine(int selectedIndex)
    {
        Vector2[] startPositions = new Vector2[cards.Length];
        Vector2[] targetPositions = new Vector2[cards.Length];

        for (int i = 0; i < cards.Length; i++)
        {
            if (i == selectedIndex || cards[i] == null)
                continue;

            startPositions[i] = cards[i].anchoredPosition;

            targetPositions[i] =
                startPositions[i] +
                Vector2.up * unselectedMoveUpDistance;
        }

        float elapsedTime = 0f;

        while (elapsedTime < unselectedMoveUpDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / unselectedMoveUpDuration
            );

            float easedProgress = EaseInCubic(progress);

            for (int i = 0; i < cards.Length; i++)
            {
                if (i == selectedIndex || cards[i] == null)
                    continue;

                cards[i].anchoredPosition =
                    Vector2.LerpUnclamped(
                        startPositions[i],
                        targetPositions[i],
                        easedProgress
                    );
            }

            yield return null;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (i == selectedIndex || cards[i] == null)
                continue;

            cards[i].anchoredPosition = targetPositions[i];
        }
    }

    /// <summary>
    /// 缓慢开始，快速结束
    /// ゆっくり開始し、素早く終了する
    /// </summary>
    private float EaseInCubic(float value)
    {
        return value * value * value;
    }
}