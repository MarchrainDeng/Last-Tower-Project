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

        // 防止重复输入
        // 重複入力を防止する
        Debug.Log($"Scene transition start: {sceneName}");

        // 淡出到黑色
        // 黒へフェードアウト
        yield return Fade(
            fadeCanvasGroup.alpha,
            1f,
            fadeOutDuration
        );

        // 确保完全黑
        // 完全に黒くする
        fadeCanvasGroup.alpha = 1f;

        // 加载场景
        // シーンをロードする
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        // 等待一帧，确保新场景初始化
        // 新しいシーンの初期化を待つ
        yield return null;

        // 从黑色淡入
        // 黒からフェードイン
        /*
        yield return Fade(
            1f,
            0f,
            fadeInDuration
        );*/

        fadeCanvasGroup.alpha = 0f;

        isTransitioning = false;

        Debug.Log("Scene transition finished.");
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