using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Master/BGM/SFXの音量をAudio Mixer経由で一元管理するシングルトン
///
/// 各AudioSourceは今まで通り個別に再生してOK。
/// このスクリプトが触るのはMixerの音量パラメータのみで、
/// 既存のAudioSourceやその再生コードには一切手を加えない。
///
/// 【必要な準備（Unity側）】
/// 1. Audio Mixerアセット（MainMixerなど）を作成
/// 2. Master配下にBGM/SFXの子グループを作成
/// 3. Master/BGM/SFXそれぞれのVolumeパラメータをExpose
///    → パラメータ名は MasterVolume / BGMVolume / SFXVolume にする
/// 4. 音を鳴らしている各AudioSourceの「出力」を
///    対応するグループ（BGM or SFX）に設定する
///    （既存スクリプトの変更は不要）
///
/// 【Inspectorでアサインするもの】
/// - mixer : 上記で作成したAudio Mixerアセット
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("── Mixer ──────────────────────")]
    public AudioMixer mixer;

    [Header("── PlayerPrefsキー ────────────")]
    const string MasterKey = "Volume_Master";
    const string BGMKey = "Volume_BGM";
    const string SFXKey = "Volume_SFX";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        // 保存済み音量を復元（未保存なら1.0）
        SetMasterVolume(PlayerPrefs.GetFloat(MasterKey, 1f));
        SetBGMVolume(PlayerPrefs.GetFloat(BGMKey, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SFXKey, 1f));
    }

    // ─── 音量設定（0〜1 → dBに変換） ────────────────────────────
    public void SetMasterVolume(float value)
    {
        SetMixerVolume("MasterVolume", value);
        PlayerPrefs.SetFloat(MasterKey, value);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float value)
    {
        SetMixerVolume("BGMVolume", value);
        PlayerPrefs.SetFloat(BGMKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SetMixerVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
    }

    void SetMixerVolume(string paramName, float value)
    {
        if (mixer == null) return;
        // 0の時は-80dB（実質無音）、それ以外はlogスケールに変換
        float dB = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        mixer.SetFloat(paramName, dB);
    }

    // ─── 保存済み音量の取得（Slider初期値に使う） ─────────────────
    public float GetMasterVolume() => PlayerPrefs.GetFloat(MasterKey, 1f);
    public float GetBGMVolume() => PlayerPrefs.GetFloat(BGMKey, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SFXKey, 1f);
}