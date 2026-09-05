using System.Collections;
using UnityEngine;

public class BossFlyAway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bossTransform;

    // Boss最终飞向的位置
    // Bossの最終移動先
    [SerializeField] private Transform targetPoint;

    [Header("Scale Settings")]

    // 缩小所需时间
    // 縮小にかかる時間
    [SerializeField] private float scaleDuration = 1f;

    // 最终大小倍率
    // 最終スケール倍率
    [SerializeField] private float targetScaleMultiplier = 0.6f;

    [Header("Move Settings")]

    // 向上飞行所需时间
    // 上方向への移動時間
    [SerializeField] private float moveDuration = 2f;

    private bool isPlaying = false;

    /// <summary>
    /// 开始Boss离场演出
    /// Boss退場演出を開始する
    /// </summary>
    public void PlayFlyAway()
    {
        if (isPlaying)
            return;

        StartCoroutine(
            FlyAwayCoroutine()
        );
    }

    private IEnumerator FlyAwayCoroutine()
    {
        if (bossTransform == null ||
            targetPoint == null)
        {
            yield break;
        }

        isPlaying = true;

        // ==============================
        // 第一阶段：原地缩小
        // 第1段階：その場で縮小する
        // ==============================

        Vector3 startScale =
            bossTransform.localScale;

        Vector3 targetScale =
            startScale *
            targetScaleMultiplier;

        float timer = 0f;

        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / scaleDuration
            );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            bossTransform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    smoothT
                );

            yield return null;
        }

        // 确保缩小后的大小准确
        // 縮小後のスケールを確定する
        bossTransform.localScale =
            targetScale;


        // ==============================
        // 第二阶段：向目标位置飞行
        // 第2段階：目標位置へ飛行する
        // ==============================

        Vector3 startPosition =
            bossTransform.position;

        Vector3 targetPosition =
            targetPoint.position;

        timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / moveDuration
            );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            bossTransform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            yield return null;
        }

        // 确保最终位置准确
        // 最終位置を確定する
        bossTransform.position =
            targetPosition;

        isPlaying = false;
    }
}