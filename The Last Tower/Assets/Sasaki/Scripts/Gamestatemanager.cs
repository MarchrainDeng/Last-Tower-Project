using UnityEngine;

/// <summary>
/// ゲーム全体の一時停止状態を管理するシングルトン
///
/// 設定画面・勝利画面・敗北画面など、
/// ゲームプレイ操作を止めたい画面が開いたら
/// GameStateManager.IsPaused = true にする
///
/// ゲームプレイ側（BlockMoveController・BossHandなど）は
/// 入力処理の先頭で GameStateManager.IsPaused を確認する
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public static bool IsPaused { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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