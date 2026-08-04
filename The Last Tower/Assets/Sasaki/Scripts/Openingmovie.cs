using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// タイトルシーン起動時に再生するオープニング動画
///
/// 【Inspectorでアサインするもの】
/// - videoPlayer : 動画再生用のVideoPlayer
/// - movieRoot   : 動画表示用のRootオブジェクト（RawImageなど、終了後にSetActive(false)）
/// - titleMenu   : 動画再生中は操作を無効化したいTitleMenu（任意）
///
/// 【操作】
/// - 何かボタンを押すとスキップ
/// </summary>
public class OpeningMovie : MonoBehaviour
{
    [Header("── 参照 ────────────────────────")]
    public VideoPlayer videoPlayer;
    public GameObject movieRoot;
    public TitleMenu titleMenu; // 動画再生中は操作を止める（任意）

    [Header("── スキップ ────────────────────")]
    public GamepadButton skipGamepadButton = GamepadButton.South;
    public Key skipKey = Key.Space;

    // アプリ起動中だけ有効なフラグ（再起動すると消える）
    static bool hasPlayedThisSession = false;

    bool isPlaying = false;

    void Start()
    {
        // 同じ起動中の2周目以降は再生しない
        if (hasPlayedThisSession)
        {
            EndMovie();
            return;
        }

        if (videoPlayer == null || movieRoot == null)
        {
            EndMovie();
            return;
        }

        isPlaying = true;
        movieRoot.SetActive(true);

        if (titleMenu != null)
            titleMenu.enabled = false;

        // 動画（Direct出力）以外の音を止める
        AudioListener.pause = true;

        videoPlayer.loopPointReached += OnMovieFinished;
        videoPlayer.Play();

        hasPlayedThisSession = true;
    }

    void Update()
    {
        if (!isPlaying) return;

        bool skipPressed =
            (Keyboard.current != null && Keyboard.current[skipKey].wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current[skipGamepadButton].wasPressedThisFrame);

        if (skipPressed)
            EndMovie();
    }

    void OnMovieFinished(VideoPlayer vp)
    {
        EndMovie();
    }

    void EndMovie()
    {
        if (!isPlaying && movieRoot != null && !movieRoot.activeSelf)
            return; // 既に終了処理済み

        isPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnMovieFinished;
            videoPlayer.Stop();
        }

        if (movieRoot != null)
            movieRoot.SetActive(false);

        if (titleMenu != null)
            titleMenu.enabled = true;

        // 音を元に戻す
        AudioListener.pause = false;
    }
}