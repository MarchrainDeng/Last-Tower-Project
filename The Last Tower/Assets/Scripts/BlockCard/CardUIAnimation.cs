using System.Collections;
using UnityEngine;

public class CardUIAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;

    [Header("Enter Animation")]
    [SerializeField] private float enterOffsetY = 800f;
    [SerializeField] private float enterDuration = 0.45f;

    [Header("Selected Animation")]
    [SerializeField] private float pressDownDistance = 30f;
    [SerializeField] private float pressDownDuration = 0.1f;
    [SerializeField] private float selectedExitOffsetY = 900f;
    [SerializeField] private float selectedExitDuration = 0.3f;

    [Header("Normal Exit Animation")]
    [SerializeField] private float normalExitOffsetY = 900f;
    [SerializeField] private float normalExitDuration = 0.35f;

    private Vector2 normalPosition;
    private bool hasSavedNormalPosition;

    public float EnterDuration => enterDuration;
    public float NormalExitDuration => normalExitDuration;

    private void Awake()
    {
        // 获取RectTransform
        // RectTransformを取得する
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// 保存卡牌在界面中的正常位置
    /// カードの通常位置を保存する
    /// </summary>
    public void SaveNormalPosition()
    {
        if (rectTransform == null)
            return;

        normalPosition = rectTransform.anchoredPosition;
        hasSavedNormalPosition = true;
    }

    /// <summary>
    /// 将卡牌直接放到屏幕上方
    /// カードを画面上部へ即座に移動する
    /// </summary>
    public void SetEnterStartPosition()
    {
        EnsureNormalPositionSaved();

        rectTransform.anchoredPosition =
            normalPosition + Vector2.up * enterOffsetY;
    }

    /// <summary>
    /// 将卡牌恢复到正常位置
    /// カードを通常位置へ戻す
    /// </summary>
    public void ResetToNormalPosition()
    {
        EnsureNormalPositionSaved();

        rectTransform.anchoredPosition = normalPosition;
    }

    /// <summary>
    /// 播放从上方落下的进入动画
    /// 上から落下する登場アニメーションを再生する
    /// </summary>
    public IEnumerator PlayEnterAnimation()
    {
        EnsureNormalPositionSaved();

        yield return MoveTo(
            normalPosition,
            enterDuration,
            EaseOutCubic
        );
    }

    /// <summary>
    /// 播放选中卡牌的动画
    /// 選択されたカードのアニメーションを再生する
    /// </summary>
    public IEnumerator PlaySelectedExitAnimation()
    {
        EnsureNormalPositionSaved();

        // 先向下移动一点
        // 最初に少し下へ移動する
        Vector2 pressedPosition =
            normalPosition + Vector2.down * pressDownDistance;

        yield return MoveTo(
            pressedPosition,
            pressDownDuration,
            EaseOutCubic
        );

        // 再从当前位置向上离开屏幕
        // その後、現在位置から上方向へ退場する
        Vector2 exitPosition =
            normalPosition + Vector2.up * selectedExitOffsetY;

        yield return MoveTo(
            exitPosition,
            selectedExitDuration,
            EaseInCubic
        );
    }

    /// <summary>
    /// 播放未选择卡牌的退出动画
    /// 選択されていないカードの退場アニメーションを再生する
    /// </summary>
    public IEnumerator PlayNormalExitAnimation()
    {
        EnsureNormalPositionSaved();

        Vector2 exitPosition =
            normalPosition + Vector2.up * normalExitOffsetY;

        yield return MoveTo(
            exitPosition,
            normalExitDuration,
            EaseInCubic
        );
    }

    /// <summary>
    /// 移动到目标位置
    /// 目標位置まで移動する
    /// </summary>
    private IEnumerator MoveTo(
        Vector2 targetPosition,
        float duration,
        System.Func<float, float> easingFunction
    )
    {
        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = targetPosition;
            yield break;
        }

        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float easedProgress = easingFunction(progress);

            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    easedProgress
                );

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// 确保正常位置已经保存
    /// 通常位置が保存されていることを確認する
    /// </summary>
    private void EnsureNormalPositionSaved()
    {
        if (hasSavedNormalPosition)
            return;

        SaveNormalPosition();
    }

    /// <summary>
    /// 快速开始，缓慢结束
    /// 素早く開始し、ゆっくり終了する
    /// </summary>
    private float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
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