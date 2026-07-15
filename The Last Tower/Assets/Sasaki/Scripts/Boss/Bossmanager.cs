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

    [Header("── 出現タイミング ──────────────")]
    public float spawnDelay = 70f;

    // ─── 起動 ─────────────────────────────────────────────────────
    void Start()
    {
        // ボスは最初非表示
        if (bossRoot != null)
            bossRoot.SetActive(false);

        StartCoroutine(SpawnBoss());
    }

    // ─── 出現 ─────────────────────────────────────────────────────
    IEnumerator SpawnBoss()
    {
        yield return new WaitForSeconds(spawnDelay);

        Debug.Log("[BossManager] ボス出現！");

        if (bossRoot != null)
            bossRoot.SetActive(true);

        // 両手の行動を開始
        if (leftHand != null) leftHand.StartBehavior();
        if (rightHand != null) rightHand.StartBehavior();

        // 両手の撃破イベントを購読
        if (leftHand != null) leftHand.OnDefeated += CheckVictory;
        if (rightHand != null) rightHand.OnDefeated += CheckVictory;

        // タワーHP0で敗北監視
        if (towerHP != null)
            towerHP.OnDead += OnDefeat;
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
        // TODO: 勝利演出・シーン遷移
        Debug.Log("[BossManager] ゲームクリア！");
        Destroy(gameObject);
    }

    void OnDefeat()
    {
        // TODO: 敗北演出・シーン遷移
        Debug.Log("[BossManager] タワーHP0 ゲームオーバー！");
    }
}