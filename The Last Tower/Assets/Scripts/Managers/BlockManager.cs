using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/*
----------------------------------------
【功能 / 機能】
管理场景中的所有方块。

シーン内のすべてのブロックを管理する。

----------------------------------------
*/

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance;

    [Header("Block")]

    // 所有方块的Tag
    // ブロックのTag
    [SerializeField]
    private string blockTag = "TowerBlock";

    // 弾丸のTag（Bullet.prefab / Cannon.prefab が使用）
    [SerializeField]
    private string bulletTag = "Bullet";

    [Header("References")]
    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    [SerializeField] private BlockSelectionFlowManager flowManager;

    [Header("Moving Objects")]
    [SerializeField] private GameObject blockSpawner;
    [SerializeField] private float moveDistance = 3f;

    [SerializeField] private GameObject finalCannon;

    [SerializeField]
    private ObjectSpawner objectSpawner;

    [SerializeField] private GameObject heightLine_1;
    [SerializeField] private GameObject heightLine_2;

    [SerializeField]
    private TMP_Text countdownText;

    [SerializeField] float countDown = 30f;

    [SerializeField]
    private RectTransform targetUI;

    [Header("Final Result (仕様8: home/replay選択)")]
    [Tooltip("仕様4〜7(キャノン発射/インク爆発/インク退場演出)が未実装のため、暫定的にここから直接呼び出してテストする。実装後はインク演出完了後に呼ぶよう差し替えること")]
    [SerializeField] private FinalResultChooser finalResultChooser;

    [SerializeField] private GameOverSequence gameOverSequence;
    [SerializeField] private GameObject defeatCanvas;

    [SerializeField] GameObject UI_1;
    [SerializeField] private GameObject UI_2;

    // 是否已经开始最终攻击演出
    // 最終攻撃演出が開始されているか
    private bool isFinalAttackStarted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 测试：按下方向键摧毁全部方块
        // テスト：下方向キーですべてのブロックを破壊
        /*
        if (Gamepad.current != null &&
            Gamepad.current.dpad.down.wasPressedThisFrame)
        {
            StartFinalSequence();
        }*/
    }

    /// <summary>
    /// 通知BlockManager最终攻击演出已经开始
    /// BlockManagerへ最終攻撃演出の開始を通知する
    /// </summary>
    public void SetFinalAttackStarted()
    {
        isFinalAttackStarted = true;
    }

    /// <summary>
    /// 执行最终阶段流程
    /// </summary>
    public void StartFinalSequence()
    {
        Debug.Log("[BlockManager] StartFinalSequence 開始");

        // カード選択等で timeScale が 0 のまま残っていると、カウントダウンやカメラ演出が
        // 進まず「何も起きていないように見える」状態になるため、念のためここでリセットする
        Time.timeScale = 1f;

        // BossManager.OnVictory() でボス撃破時に GameStateManager.SetPaused(true) が呼ばれるが、
        // このプロジェクトの BlockSelectionFlowManager は pauseGameDuringSelection = false のため
        // カード選択完了時に SetPaused(false) へ戻す処理が実行されない。
        // その結果 IsPaused が true のまま残り、CardSelector/BlockMoveController の入力判定
        // (if (GameStateManager.IsPaused) return;) でずっと入力がブロックされてしまうため、
        // トウscene開始時にここで明示的に解除する
        GameStateManager.SetPaused(false);

        DestroyAllBlocks();
        Debug.Log("[BlockManager] DestroyAllBlocks 完了");

        // 発射済みの弾丸が画面に残り続けないように、ここで一括削除する
        DestroyAllBullets();

        OtherFunction();
        Debug.Log("[BlockManager] OtherFunction 完了");

        StartCoroutine(
            CountdownCoroutine(countDown)
        );
    }

    /// <summary>
    /// 摧毁场景中的所有方块
    /// シーン内のすべてのブロックを破壊する
    /// </summary>
    public void DestroyAllBlocks()
    {
        GameObject[] blocks =
            GameObject.FindGameObjectsWithTag(blockTag);

        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
    }

    /// <summary>
    /// 摧毁场景中所有已发射的子弹
    /// シーン内の発射済みの弾丸をすべて破壊する
    ///
    /// トウフェーズへ移行すると敵とボスが居なくなるため、
    /// 追尾先を失った弾丸が画面内に残り続けてしまうのを防ぐ
    /// </summary>
    public void DestroyAllBullets()
    {
        GameObject[] bullets =
            GameObject.FindGameObjectsWithTag(bulletTag);

        foreach (GameObject bullet in bullets)
        {
            Destroy(bullet);
        }

        Debug.Log($"[BlockManager] DestroyAllBullets 完了 ({bullets.Length}個)");
    }

    /// <summary>
    /// 执行其他功能
    /// </summary>
    public void OtherFunction()
    {
        //将方块选择变为动力方块
        flowManager.SetNextSelectionType(BlockSelectionType.Final);
        flowManager.RequestFinalSelection();

        //移动相机
        Camera.main.GetComponent<CameraController>().MoveToTarget();

        //移动方块生成点
        if (blockSpawner != null)
        {
            blockSpawner.transform.position += Vector3.right * moveDistance;
            blockSpawner.transform.position = new Vector3(0, 6.9f, 0);
        }

        objectSpawner.SpawnAndMove(finalCannon, new Vector3(0, 8.5f, 0), new Vector3(0, 3.5f, 0));

        heightLine_1.SetActive(false);
        heightLine_2.SetActive(false);

        StartCoroutine(MoveUIDown(targetUI,300f,800f));
    }

    private IEnumerator CountdownCoroutine(float seconds)
    {
        countdownText.gameObject.SetActive(true);

        float timer = seconds;

        while (timer > 0f)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int secs = Mathf.FloorToInt(timer % 60f);
            int centiseconds = Mathf.FloorToInt((timer * 100f) % 100f);

            countdownText.text =
                $"{minutes:00}:{secs:00}:{centiseconds:00}";

            timer -= Time.deltaTime;

            yield return null;
        }

        countdownText.text = "00:00:00";

        // =========================
        // 在这里处理倒计时结束事件
        // =========================
        OnCountdownFinished();

        countdownText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 倒计时结束时执行
    /// カウントダウン終了時に実行
    /// </summary>
    private void OnCountdownFinished()
    {
        Debug.Log("30秒倒计时结束");

        // 在这里写你需要执行的事件
        // TODO: 本来は仕様4〜7(キャノン発射→ボス爆発→インクが画面を覆って引いていく演出)の後に
        //       home/replay選択(仕様8)を出す。仕様4〜7が未実装のため、暫定的にここから直接呼び出す
        /*
        if (finalResultChooser != null)
        {
            finalResultChooser.Show();
        }
        */

        if (isFinalAttackStarted)
        {
            Debug.Log("最终攻击演出进行中，跳过GameOver处理");
            return;
        }

        UI_1.SetActive(false);
        UI_2.SetActive(false);

        defeatCanvas.SetActive(true);

        if (gameOverSequence != null)
        {
            gameOverSequence.PlaySequence();
        }
    }

    /// <summary>
    /// UI向下平滑移动固定距离
    /// UIを一定距離だけ下へスムーズに移動する
    /// </summary>
    private IEnumerator MoveUIDown(
        RectTransform rect,
        float distance,
        float speed)
    {
        Vector2 startPosition = rect.anchoredPosition;
        Vector2 targetPosition =
            startPosition + Vector2.down * distance;

        while (Vector2.Distance(
            rect.anchoredPosition,
            targetPosition) > 1f)
        {
            rect.anchoredPosition =
                Vector2.MoveTowards(
                    rect.anchoredPosition,
                    targetPosition,
                    speed * Time.deltaTime);

            yield return null;
        }

        rect.anchoredPosition = targetPosition;
    }
}