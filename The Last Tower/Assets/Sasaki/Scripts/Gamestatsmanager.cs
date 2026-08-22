using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム内の各種統計（倒した敵・配置/落下/連結ブロック数）を
/// 一元管理するシングルトン
///
/// 【カウント元】
/// - 倒した敵     : EnemyHealth.Die() / BossHand.Die()
/// - 配置ブロック : BlockLanding.Land()
/// - 落下ブロック : DestroyZone.OnTriggerEnter2D()（"Block"タグ削除時）
/// - 連結ブロック : PowerBlock.SetPowered(true)（充電状態になった瞬間）
///
/// シーン再読み込みで自然にリセットされる想定（DontDestroyOnLoadしない）
/// </summary>
public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("── 統計（読み取り専用表示） ──────")]
    [SerializeField] private int enemiesDefeated = 0;
    [SerializeField] private int blocksPlaced = 0;
    [SerializeField] private int blocksDropped = 0;
    [SerializeField] private int blocksConnected = 0;

    public int EnemiesDefeated => enemiesDefeated;
    public int BlocksPlaced => blocksPlaced;
    public int BlocksDropped => blocksDropped;
    public int BlocksConnected => blocksConnected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // シーン再読み込みでリセットしたいので DontDestroyOnLoad はしない
    }

    // ─── カウント用メソッド（各スクリプトから呼ぶ） ──────────────
    public void OnEnemyDefeated()
    {
        enemiesDefeated++;
    }

    public void OnBlockPlaced()
    {
        blocksPlaced++;
    }

    public void OnBlockDropped()
    {
        blocksDropped++;
    }

    public void OnBlockConnected()
    {
        blocksConnected++;
    }

    // ─── リザルト表示用にリセットしたい場合 ────────────────────
    public void ResetStats()
    {
        enemiesDefeated = 0;
        blocksPlaced = 0;
        blocksDropped = 0;
        blocksConnected = 0;
    }
}