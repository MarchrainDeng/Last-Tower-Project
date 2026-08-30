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
        if (Gamepad.current != null &&
            Gamepad.current.dpad.down.wasPressedThisFrame)
        {
            StartFinalSequence();
        }
    }

    /// <summary>
    /// 执行最终阶段流程
    /// </summary>
    public void StartFinalSequence()
    {
        DestroyAllBlocks();

        OtherFunction();

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