using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 起動専用シーンで再生するオープニング動画
/// 再生終了、またはスキップで指定したシーン（タイトル画面）へ遷移する
/// </summary>
public class OpeningMovie : MonoBehaviour
{
    [Header("── 参照 ────────────────────────")]
    public VideoPlayer videoPlayer;
    [Tooltip("非表示制御用のCanvasGroup（movieRootにアタッチ）")]
    public CanvasGroup movieCanvasGroup;

    [Header("── 遷移設定 ────────────────────")]
    [Tooltip("動画終了後にロードするシーン名")]
    public string titleSceneName = "TitleScene";

    [Header("── スキップ ────────────────────")]
    public GamepadButton skipGamepadButton = GamepadButton.South;
    public Key skipKey = Key.Space;

    bool isPlaying = false;
    bool isTransitioning = false; // シーン遷移の多重呼び出し防止用

    void Awake()
    {
        if (videoPlayer == null)
        {
            GoToNextScene();
            return;
        }

        // 映像の準備が終わるまでは透明にして非表示（GameObject自体はアクティブを維持）
        if (movieCanvasGroup != null)
        {
            movieCanvasGroup.alpha = 0f;
        }

        // InspectorのPlayOnAwakeがONだと競合するためOFFに設定
        videoPlayer.playOnAwake = false;
        videoPlayer.loopPointReached += OnMovieFinished;
    }

    IEnumerator Start()
    {
        isPlaying = true;

        // 1. 動画の読み込み準備を開始（アクティブ状態のため正常に動作）
        videoPlayer.Prepare();

        // 2. 準備が完了するまで待機
        while (!videoPlayer.isPrepared && isPlaying)
        {
            yield return null;
        }

        if (!isPlaying) yield break;

        // 3. 再生開始
        videoPlayer.Play();

        // 4. 映像のテクスチャが生成されるまで待機（黒画面防止）
        while (videoPlayer.texture == null && isPlaying)
        {
            yield return null;
        }

        if (!isPlaying) yield break;

        // 5. 完全に映像が描画できる状態になってから表示
        if (movieCanvasGroup != null)
        {
            movieCanvasGroup.alpha = 1f;
        }
    }

    void Update()
    {
        if (!isPlaying || isTransitioning) return;

        bool skipPressed =
            (Keyboard.current != null && Keyboard.current[skipKey].wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current[skipGamepadButton].wasPressedThisFrame);

        if (skipPressed)
        {
            GoToNextScene();
        }
    }

    void OnMovieFinished(VideoPlayer vp)
    {
        GoToNextScene();
    }

    void GoToNextScene()
    {
        if (isTransitioning) return;

        isPlaying = false;
        isTransitioning = true;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnMovieFinished;
            videoPlayer.Stop();
        }

        // 次のシーン（タイトル画面）へ遷移
        SceneManager.LoadScene(titleSceneName);
    }
}