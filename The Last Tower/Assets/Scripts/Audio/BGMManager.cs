using UnityEngine;
using System.Collections;
public class BGMManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("BGM")]
    [SerializeField] private AudioClip normalBGM;
    [SerializeField] private AudioClip finalBGM;
    [SerializeField] private AudioClip bossBGM;
    [SerializeField] private AudioClip winBGM;
    [SerializeField] private AudioClip trueWinBGM;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// 切换BGM
    /// BGMを切り替える
    /// </summary>
    public void ChangeBGM(AudioClip newClip)
    {
        if (audioSource == null || newClip == null)
            return;

        // 如果已经在播放同一首BGM，就不重新播放
        // 同じBGMを再生中の場合は何もしない
        if (audioSource.clip == newClip &&
            audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    /// <summary>
    /// 淡出并停止BGM
    /// BGMをフェードアウトして停止する
    /// </summary>
    public void FadeOutBGM(float playDuration, float fadeDuration)
    {
        StartCoroutine(FadeOutCoroutine(playDuration,fadeDuration));
    }

    private IEnumerator FadeOutCoroutine(
    float playDuration,
    float fadeDuration)
    {
        if (audioSource == null ||
            !audioSource.isPlaying)
        {
            yield break;
        }

        // 保存原本音量
        // 元の音量を保存する
        float startVolume = audioSource.volume;

        // 淡出前保持正常播放一段时间
        // フェードアウト前に一定時間通常再生する
        if (playDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                playDuration
            );
        }

        // 开始淡出
        // フェードアウトを開始する
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / fadeDuration
            );

            audioSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0f,
                    t
                );

            yield return null;
        }

        // 停止BGM
        // BGMを停止する
        audioSource.Stop();

        // 恢复原本音量，方便下次播放
        // 次回再生のため元の音量に戻す
        audioSource.volume = startVolume;
    }

    /// <summary>
    /// 淡出当前BGM后切换并播放指定BGM
    /// 現在のBGMをフェードアウトした後、指定したBGMへ切り替える
    /// </summary>
    public void ChangeBGMWithFade(
        AudioClip newBGM,
        float fadeDuration)
    {
        if (audioSource == null ||
            newBGM == null)
        {
            return;
        }

        StartCoroutine(
            ChangeBGMWithFadeCoroutine(
                newBGM,
                fadeDuration
            )
        );
    }

    private IEnumerator ChangeBGMWithFadeCoroutine(
        AudioClip newBGM,
        float fadeDuration)
    {
        // 保存当前音量
        // 現在の音量を保存する
        float startVolume =
            audioSource.volume;

        // 当前有BGM正在播放时进行淡出
        // 現在BGMが再生中の場合はフェードアウトする
        if (audioSource.isPlaying)
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    timer / fadeDuration
                );

                audioSource.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        t
                    );

                yield return null;
            }
        }

        // 停止当前BGM
        // 現在のBGMを停止する
        audioSource.Stop();

        // 切换到新的BGM
        // 新しいBGMへ切り替える
        audioSource.clip = newBGM;

        // 恢复原来的音量
        // 元の音量に戻す
        audioSource.volume = startVolume;

        // 循环播放
        // ループ再生する
        audioSource.loop = true;

        // 播放新的BGM
        // 新しいBGMを再生する
        audioSource.Play();
    }

    /// <summary>
    /// 立即停止当前BGM
    /// 現在再生中のBGMを即時停止する
    /// </summary>
    public void StopBGM()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }

    public void PlayNormalBGM()
    {
        ChangeBGM(normalBGM);
    }

    public void PlayFinalBGM()
    {
        ChangeBGM(finalBGM);
    }

    public void PlayBossBGM()
    {
        ChangeBGM(bossBGM);
    }

    public void PlayWinBGM()
    {
        ChangeBGM(winBGM);
    }

    public void PlayTrueWinBGM()
    {
        ChangeBGM(trueWinBGM);
    }
}