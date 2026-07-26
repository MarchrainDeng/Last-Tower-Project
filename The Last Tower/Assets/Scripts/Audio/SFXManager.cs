using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
统一管理游戏中的音效播放。

ゲーム内の効果音再生を一括管理する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/26
----------------------------------------
*/

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    // 全局访问实例
    // グローバルアクセス用インスタンス
    public static SFXManager Instance { get; private set; }

    [Header("References")]

    // 音效播放用AudioSource
    // 効果音再生用AudioSource
    [SerializeField]
    private AudioSource sfxSource;

    [Header("Settings")]

    // 场景切换时是否保留
    // シーン切り替え時に保持するか
    [SerializeField]
    private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        // 防止场景中同时存在多个管理器
        // シーン内に複数のマネージャーが存在することを防ぐ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    /// <summary>
    /// 播放一次音效
    /// 効果音を一度再生する
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 使用指定音量播放一次音效
    /// 指定音量で効果音を一度再生する
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null)
            return;

        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeScale)
        );
    }

    /// <summary>
    /// 设置全部音效音量
    /// すべての効果音音量を設定する
    /// </summary>
    public void SetVolume(float volume)
    {
        if (sfxSource == null)
            return;

        sfxSource.volume =
            Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 暂停所有正在播放的音效
    /// 再生中のすべての効果音を一時停止する
    /// </summary>
    public void PauseAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Pause();
    }

    /// <summary>
    /// 继续播放被暂停的音效
    /// 一時停止中の効果音を再開する
    /// </summary>
    public void ResumeAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.UnPause();
    }

    /// <summary>
    /// 停止所有正在播放的音效
    /// 再生中のすべての効果音を停止する
    /// </summary>
    public void StopAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
    }
}