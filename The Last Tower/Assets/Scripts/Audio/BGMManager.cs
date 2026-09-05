using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("BGM")]
    [SerializeField] private AudioClip normalBGM;
    [SerializeField] private AudioClip finalBGM;
    [SerializeField] private AudioClip bossBGM;

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
}