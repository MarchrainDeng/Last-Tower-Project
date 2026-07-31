using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Video;

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

    bool isPlaying = false;

    void Start()
    {
        if (videoPlayer == null || movieRoot == null)
        {
            EndMovie();
            return;
        }

        isPlaying = true;
        movieRoot.SetActive(true);

        if (titleMenu != null)
            titleMenu.enabled = false;

        videoPlayer.loopPointReached += OnMovieFinished;
        videoPlayer.Play();
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
    }
}