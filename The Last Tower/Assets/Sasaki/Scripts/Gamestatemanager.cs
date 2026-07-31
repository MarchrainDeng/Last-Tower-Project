using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// ゲーム全体の一時停止状態を管理するシングルトン
///
/// 設定画面・勝利画面・敗北画面など、
/// ゲームプレイ操作を止めたい画面が開いたら
/// GameStateManager.IsPaused = true にする
///
/// ゲームプレイ側（BlockMoveController・BossHandなど）は
/// 入力処理の先頭で GameStateManager.IsPaused を確認する
///
/// 【強制リセット】
/// Ctrl + Escape + Enter の同時押しで、全シーン共通で
/// タイトルシーンへ強制的に戻る（デバッグ・詰み対策用）
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public static bool IsPaused { get; private set; } = false;

    [Header("── 強制リセット ────────────────")]
    public string titleSceneName = "MainMenu"; // SettingsMenu.homeSceneNameと合わせる

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        CheckForceReset();
    }

    // ─── 強制リセット：Ctrl + Escape + Enter ────────────────────────
    void CheckForceReset()
    {
        if (Keyboard.current == null) return;

        bool ctrl = Keyboard.current.ctrlKey.isPressed;
        bool escape = Keyboard.current.escapeKey.isPressed;
        bool enter = Keyboard.current.enterKey.wasPressedThisFrame
                   || Keyboard.current.numpadEnterKey.wasPressedThisFrame;

        if (ctrl && escape && enter)
        {
            Debug.Log("[GameStateManager] 強制リセット実行 → タイトルシーンへ");

            IsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(titleSceneName);
        }
    }

    // シーンが切り替わったら必ずリセットする
    // （前のシーンの一時停止状態を持ち越さないため）
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 一時停止状態をセットする
    /// 設定画面・リザルト画面などが呼ぶ
    /// </summary>
    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
    }
}