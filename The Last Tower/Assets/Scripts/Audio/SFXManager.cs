using System.Collections.Generic;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
统一管理游戏中的音效播放。
限制同类音效的播放频率和同时播放数量，
避免大量攻击音效同时播放时过于杂乱。

ゲーム内の効果音再生を一括管理する。
同じ種類の効果音の再生頻度と同時再生数を制限し、
大量の攻撃音が重なって騒がしくなることを防ぐ。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("References")]

    // 普通音效播放器
    // 通常効果音再生用AudioSource
    [SerializeField]
    private AudioSource sfxSource;

    [Header("Attack Sound Settings")]

    // 同一种攻击音效的最小播放间隔
    // 同じ攻撃音の最小再生間隔
    [SerializeField]
    private float attackSoundInterval = 0.08f;

    // 攻击音效的最低随机音高
    // 攻撃音の最小ランダムピッチ
    [SerializeField]
    private float minimumAttackPitch = 0.95f;

    // 攻击音效的最高随机音高
    // 攻撃音の最大ランダムピッチ
    [SerializeField]
    private float maximumAttackPitch = 1.05f;

    // 攻击音效的最低随机音量
    // 攻撃音の最小ランダム音量
    [SerializeField]
    private float minimumAttackVolume = 0.75f;

    // 攻击音效的最高随机音量
    // 攻撃音の最大ランダム音量
    [SerializeField]
    private float maximumAttackVolume = 0.9f;

    // 记录每个音效上次播放时间
    // 各効果音の前回再生時間を記録する
    private readonly Dictionary<AudioClip, float>
        lastPlayTimes = new Dictionary<AudioClip, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

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
    /// 播放普通音效
    /// 通常効果音を再生する
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 使用指定音量播放普通音效
    /// 指定音量で通常効果音を再生する
    /// </summary>
    public void PlaySFX(
        AudioClip clip,
        float volumeScale)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeScale)
        );
    }

    /// <summary>
    /// 播放攻击音效，并限制同类音效的播放频率
    /// 攻撃音を再生し、同じ音の再生頻度を制限する
    /// </summary>
    public void PlayAttackSFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        float currentTime = Time.unscaledTime;

        // 如果同一种音效刚刚播放过，则跳过本次播放
        // 同じ音が直前に再生されていた場合、
        // 今回の再生をスキップする
        if (lastPlayTimes.TryGetValue(
            clip,
            out float lastPlayTime))
        {
            if (currentTime - lastPlayTime <
                attackSoundInterval)
            {
                return;
            }
        }

        lastPlayTimes[clip] = currentTime;

        float randomPitch = Random.Range(
            minimumAttackPitch,
            maximumAttackPitch
        );

        float randomVolume = Random.Range(
            minimumAttackVolume,
            maximumAttackVolume
        );

        float originalPitch = sfxSource.pitch;

        // 为本次攻击音效设置轻微随机音高
        // 今回の攻撃音にわずかなランダムピッチを設定する
        sfxSource.pitch = randomPitch;

        sfxSource.PlayOneShot(
            clip,
            randomVolume
        );

        // 恢复原来的音高
        // 元のピッチへ戻す
        sfxSource.pitch = originalPitch;
    }

    public void PlayLaserSFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        float currentTime = Time.unscaledTime;

        // 如果同一种音效刚刚播放过，则跳过本次播放
        // 同じ音が直前に再生されていた場合、
        // 今回の再生をスキップする
        if (lastPlayTimes.TryGetValue(
            clip,
            out float lastPlayTime))
        {
            if (currentTime - lastPlayTime <
                attackSoundInterval)
            {
                return;
            }
        }

        lastPlayTimes[clip] = currentTime;

        float randomPitch = Random.Range(
            minimumAttackPitch,
            maximumAttackPitch
        );

        float randomVolume = Random.Range(
            minimumAttackVolume,
            maximumAttackVolume
        );

        float originalPitch = sfxSource.pitch;

        // 为本次攻击音效设置轻微随机音高
        // 今回の攻撃音にわずかなランダムピッチを設定する
        sfxSource.pitch = randomPitch;

        sfxSource.PlayOneShot(
            clip,
            randomVolume
        );

        // 恢复原来的音高
        // 元のピッチへ戻す
        sfxSource.pitch = originalPitch;
    }

    public void PlayCannonSFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        float currentTime = Time.unscaledTime;

        // 如果同一种音效刚刚播放过，则跳过本次播放
        // 同じ音が直前に再生されていた場合、
        // 今回の再生をスキップする
        if (lastPlayTimes.TryGetValue(
            clip,
            out float lastPlayTime))
        {
            if (currentTime - lastPlayTime <
                attackSoundInterval)
            {
                return;
            }
        }

        lastPlayTimes[clip] = currentTime;

        float randomPitch = Random.Range(
            minimumAttackPitch,
            maximumAttackPitch
        );

        float randomVolume = Random.Range(
            minimumAttackVolume,
            maximumAttackVolume
        );

        float originalPitch = sfxSource.pitch;

        // 为本次攻击音效设置轻微随机音高
        // 今回の攻撃音にわずかなランダムピッチを設定する
        sfxSource.pitch = randomPitch;

        sfxSource.PlayOneShot(
            clip,
            randomVolume
        );

        // 恢复原来的音高
        // 元のピッチへ戻す
        sfxSource.pitch = originalPitch;
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
    /// 停止全部音效
    /// すべての効果音を停止する
    /// </summary>
    public void StopAllSFX()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
    }
}