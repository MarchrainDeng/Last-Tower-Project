using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager Instance;

    [Header("References")]

    // 黑色遮罩的CanvasGroup
    // 黒いフェード画面のCanvasGroup
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [Header("Fade Settings")]

    // 渐入黑色需要的时间
    // 暗転にかかる時間
    [SerializeField]
    private float fadeOutDuration = 0.5f;

    // 从黑色渐出的时间
    // フェードインにかかる時間
    [SerializeField]
    private float fadeInDuration = 0.5f;

    private bool isTransitioning = false;

    private void Awake()
    {
        // 防止重复生成
        // 重複生成を防止する
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 切换场景后不销毁
        // シーン切り替え後も破棄しない
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 游戏开始时从黑色渐出
        // ゲーム開始時に黒画面からフェードインする
        StartCoroutine(
            FadeFromBlack()
        );
    }

    /// <summary>
    /// 带渐入渐出的场景切换
    /// フェード付きでシーンを切り替える
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isTransitioning)
            return;

        StartCoroutine(
            LoadSceneRoutine(sceneName)
        );
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isTransitioning = true;

        // 先渐入黑色
        // 先に暗転する
        yield return Fade(
            fadeCanvasGroup.alpha,
            1f,
            fadeOutDuration
        );

        // 异步加载新场景
        // 新しいシーンを非同期で読み込む
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        // 等待加载完成
        // 読み込み完了まで待機する
        while (!operation.isDone)
        {
            yield return null;
        }

        // 确保黑屏状态
        // 黒画面状態を維持する
        fadeCanvasGroup.alpha = 1f;

        // 再等待一帧，让新场景完成初始化
        // 新シーンの初期化のため1フレーム待機する
        yield return null;

        Debug.Log("开始从黑色渐出");

        // 从黑色渐出
        // 黒画面からフェードインする
        yield return Fade(
            1f,
            0f,
            fadeInDuration
        );

        Debug.Log("渐出完成");

        isTransitioning = false;
    }

    /// <summary>
    /// 游戏启动时从黑色渐出
    /// </summary>
    private IEnumerator FadeFromBlack()
    {
        fadeCanvasGroup.alpha = 1f;

        yield return Fade(
            1f,
            0f,
            fadeInDuration
        );
    }

    /// <summary>
    /// 执行渐变
    /// フェード処理
    /// </summary>
    private IEnumerator Fade(
    float startAlpha,
    float targetAlpha,
    float duration)
    {
        float timer = 0f;

        fadeCanvasGroup.alpha =
            startAlpha;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            fadeCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        fadeCanvasGroup.alpha =
            targetAlpha;
    }
}