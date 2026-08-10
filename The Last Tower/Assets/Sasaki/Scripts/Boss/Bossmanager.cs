using UnityEngine;
using System.Collections;

/// <summary>
/// ボス全体の管理
/// - 一定時間後にボスを出現させる
/// - 両手のHP監視→両手撃破で勝利
///
/// 【Inspectorでアサインするもの】
/// - leftHand        : 左手のGameObject（BossHandアタッチ済み）
/// - rightHand       : 右手のGameObject（BossHandアタッチ済み）
/// - bossRoot        : ボス全体のルートGameObject（出現前は非表示）
/// - towerHP         : タワーのHPコンポーネント
/// - spawnDelay      : ボス出現までの秒数
/// </summary>
public class BossManager : MonoBehaviour
{
    [Header("── 参照 ────────────────────────")]
    public BossHand leftHand;
    public BossHand rightHand;
    public GameObject bossRoot;
    public TowerHP towerHP;

    [Header("── リザルトUI ──────────────────")]
    public GameObject victoryResultUI;  // 勝利時に表示するUI
    public GameObject defeatResultUI;   // 敗北時に表示するUI

    [Header("── リザルトBGM ──────────────────")]
    public AudioSource bgmSource;   // BGM再生用AudioSource
    public AudioClip victoryBGM;  // 勝利時のBGM
    public AudioClip defeatBGM;   // 敗北時のBGM

    [Header("── 出現タイミング ──────────────")]
    public float spawnDelay = 70f;

    // ボスが出現済みかどうか（EnemySpawnerが参照する）
    public bool HasBossSpawned { get; private set; } = false;

    [SerializeField] private GameObject heightLine_1;
    [SerializeField] private GameObject heightLine_2;
    [SerializeField] private GameObject blockSpawner;

    [SerializeField]
    private RandomTriggerSpawner randomTriggerSpawner;

    // ─── 起動 ─────────────────────────────────────────────────────
    void Start()
    {
        // ボスは最初非表示
        if (bossRoot != null)
            bossRoot.SetActive(false);

        // タワーHP0の監視はボス出現前から開始する
        // （ボスが出る前に力尽きた場合もデス判定を取るため）
        if (towerHP != null)
            towerHP.OnDead += OnDefeat;

        StartCoroutine(SpawnBoss());
    }

    // ─── 出現 ─────────────────────────────────────────────────────
    IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(spawnDelay);

        Debug.Log("[BossManager] ボス出現！");

        HasBossSpawned = true;

        //ボスが現れたときにカメラが最終位置にあるようにする
        Camera.main.GetComponent<CameraController>().MoveToTarget();

        heightLine_1.SetActive(false);
        heightLine_2.SetActive(false);

        //移动方块生成点
        if (blockSpawner != null)
        {
            blockSpawner.transform.position = new Vector3(0, 6.9f, 0);
        }

        randomTriggerSpawner.StartSpawning();

        if (bossRoot != null)
            bossRoot.SetActive(true);

        // 両手の行動を開始
        if (leftHand != null) leftHand.StartBehavior();
        if (rightHand != null) rightHand.StartBehavior();

        // 両手の撃破イベントを購読
        if (leftHand != null) leftHand.OnDefeated += CheckVictory;
        if (rightHand != null) rightHand.OnDefeated += CheckVictory;
    }

    // ─── 勝利判定（どちらかの手が倒された時に呼ばれる） ──────────
    void CheckVictory()
    {
        bool leftDead = leftHand == null || leftHand.IsDead;
        bool rightDead = rightHand == null || rightHand.IsDead;

        if (leftDead && rightDead)
        {
            Debug.Log("[BossManager] 両手撃破！勝利！");
            OnVictory();
        }
    }

    void OnVictory()
    {
        Debug.Log("[BossManager] ゲームクリア！");

        if (victoryResultUI != null)
            victoryResultUI.SetActive(true);

        PlayBGM(victoryBGM);

        Time.timeScale = 0f;
        GameStateManager.SetPaused(true);

        // bgmSourceがこのGameObjectの子だと再生中に消えてしまうため、
        // Destroyする前に切り離しておく
        if (bgmSource != null)
            bgmSource.transform.SetParent(null);

        Destroy(gameObject);
    }

    void OnDefeat()
    {
        Debug.Log("[BossManager] タワーHP0 ゲームオーバー！");

        if (defeatResultUI != null)
            defeatResultUI.SetActive(true);

        PlayBGM(defeatBGM);

        GameStateManager.SetPaused(true);
    }

    // ─── リザルトBGM再生 ─────────────────────────────────────────
    void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ─── 破棄時にイベント購読を必ず解除する ───────────────────────
    // （敵やタワーが別シーンでも生き残ってイベントを発火し続けるのを防ぐ）
    void OnDestroy()
    {
        if (towerHP != null)
            towerHP.OnDead -= OnDefeat;

        if (leftHand != null)
            leftHand.OnDefeated -= CheckVictory;

        if (rightHand != null)
            rightHand.OnDefeated -= CheckVictory;
    }
}