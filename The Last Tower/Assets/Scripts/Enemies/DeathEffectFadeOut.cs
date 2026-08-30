using System.Collections;
using UnityEngine;

/// <summary>
/// 死亡エフェクト(インク等)を、一定時間表示した後に
/// 徐々に透明度を上げてフェードアウトさせてから破棄するスクリプト
/// </summary>
public class DeathEffectFadeOut : MonoBehaviour
{
    [Tooltip("フェード開始までそのまま表示しておく時間（秒）")]
    public float holdDuration = 1f;

    [Tooltip("フェードアウトにかける時間（秒）")]
    public float fadeDuration = 1f;

    private SpriteRenderer[] renderers;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(1f - timer / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        Destroy(gameObject);
    }

    void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }
}
