using UnityEngine;
using System.Collections;

/*
----------------------------------------
【功能 / 機能】
按照设定顺序依次管理多条高度线。
游戏开始时只显示第一条高度线。
当前高度线被已落地方块触发后，将其隐藏，
并显示下一条高度线，直到全部高度线触发完毕。

設定された順番で複数の高さラインを管理する。
ゲーム開始時は最初の高さラインだけを表示する。
着地済みブロックが現在のラインに到達すると、
そのラインを非表示にして次のラインを表示する。
すべてのラインが発動するまで繰り返す。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class HeightLineManager : MonoBehaviour
{
    [Header("Height Lines")]
    // 按触发顺序排列的所有高度线
    // 発動順に並べたすべての高さライン
    [SerializeField] private HeightLineData[] heightLines;

    [Header("Check Settings")]
    // 高度检查间隔
    // 高さ判定の間隔
    [SerializeField] private float checkInterval = 0.2f;

    // 是否使用子方块Collider顶部作为高度
    // 子ブロックColliderの上端を高さとして使用するか
    [SerializeField] private bool useColliderTop = true;

    [Header("Camera Setting")]
    [SerializeField]
    private float cameraMoveDistance = 5f;

    [SerializeField]
    private float cameraSizeIncrease = 1f;

    [SerializeField]
    private float cameraMoveDuration = 1f;

    [Header("Height Check")]
    // 高度线判定容差
    // 高さライン判定の許容値
    [SerializeField]
    private float triggerOffset = 0.1f;

    [Header("References")]
    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    [SerializeField] private BlockSelectionFlowManager flowManager;

    [Header("Moving Objects")]
    [SerializeField] private GameObject leftEnemySpawnPoint;
    [SerializeField] private float moveLeftDistance = 1f;

    [SerializeField] private GameObject rightEnemySpawnPoint;
    [SerializeField] private float moveRightDistance = 1f;

    [SerializeField] private GameObject blockSpawner;
    [SerializeField] private float moveUpDistance = 1f;

    // 当前正在等待触发的高度线编号
    // 現在発動待ちの高さライン番号
    private int currentLineIndex;

    // 检查计时器
    // 判定タイマー
    private float checkTimer;

    // 是否已经触发完所有高度线
    // すべての高さラインが発動済みか
    public bool AllLinesTriggered =>
        heightLines == null ||
        currentLineIndex >= heightLines.Length;

    private void Start()
    {
        InitializeHeightLines();
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
            return;

        checkTimer = 0f;

        CheckAllHeightLines();
    }

    /// <summary>
    /// 初始化所有高度线
    /// すべての高さラインを初期化する
    /// </summary>
    private void InitializeHeightLines()
    {
        currentLineIndex = 0;

        if (heightLines == null || heightLines.Length == 0)
        {
            Debug.LogWarning(
                "Height Lines is empty. / " +
                "高さラインが設定されていません。"
            );

            return;
        }

        for (int i = 0; i < heightLines.Length; i++)
        {
            HeightLineData line = heightLines[i];

            if (line == null)
                continue;

            line.hasTriggered = false;

            if (line.lineObject != null)
            {
                // 游戏开始时只显示第一条线
                // ゲーム開始時は最初のラインだけを表示する
                line.lineObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 检查当前高度线是否被触发
    /// 現在の高さラインが発動したか判定する
    /// </summary>
    private void CheckCurrentHeightLine()
    {
        if (heightLines == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= heightLines.Length)
        {
            return;
        }

        HeightLineData currentLine =
            heightLines[currentLineIndex];

        if (currentLine == null ||
            currentLine.lineObject == null)
        {
            Debug.LogWarning(
                $"Height Line {currentLineIndex} is missing. / " +
                $"高さライン {currentLineIndex} が設定されていません。"
            );

            // 当前数据无效时跳到下一条
            // 現在のデータが無効なら次へ進む
            AdvanceToNextLine();
            return;
        }

        float highestY = GetHighestLandedBlockY();

        // 当前没有任何已落地方块
        // 現在、着地済みブロックが存在しない
        if (highestY == float.MinValue)
            return;

        float lineY = currentLine.lineObject.transform.position.y;

        // 提前一点触发
        // 少し早めに発動する
        if (highestY >= lineY - triggerOffset)
        {
            TriggerCurrentLine();
        }
    }

    /// <summary>
    /// 检查所有尚未触发的高度线
    /// まだ発動していないすべての高さラインを確認する
    /// </summary>
    private void CheckAllHeightLines()
    {
        if (heightLines == null || heightLines.Length == 0)
            return;

        float highestY = GetHighestLandedBlockY();

        // 当前没有任何已落地方块
        // 現在、着地済みブロックが存在しない
        if (highestY == float.MinValue)
            return;

        foreach (HeightLineData line in heightLines)
        {
            if (line == null ||
                line.lineObject == null ||
                line.hasTriggered)
            {
                continue;
            }

            float lineY = line.lineObject.transform.position.y;

            // 已落地方块到达高度线
            // 着地済みブロックが高さラインに到達した
            if (highestY >= lineY - triggerOffset)
            {
                TriggerLine(line);
            }
        }
    }

    /// <summary>
    /// 触发当前高度线
    /// 現在の高さラインを発動する
    /// </summary>
    private void TriggerCurrentLine()
    {
        if (AllLinesTriggered)
            return;

        HeightLineData currentLine =
            heightLines[currentLineIndex];

        if (currentLine == null)
            return;

        if (currentLine.hasTriggered)
            return;

        currentLine.hasTriggered = true;

        Debug.Log(
            $"Height Line Triggered: {currentLine.lineName} / " +
            $"高さライン発動：{currentLine.lineName}"
        );

        // 隐藏当前高度线
        // 現在の高さラインを非表示にする
        if (currentLine.lineObject != null)
        {
            currentLine.lineObject.SetActive(false);
        }

        // 执行当前高度线对应的事件
        // 現在の高さラインに対応するイベントを実行する
        ExecuteLineEvent(currentLine);

        // 进入下一条高度线
        // 次の高さラインへ進む
        AdvanceToNextLine();
    }

    /// <summary>
    /// 触发指定高度线
    /// 指定された高さラインを発動する
    /// </summary>
    private void TriggerLine(HeightLineData line)
    {
        if (line == null || line.hasTriggered)
            return;

        line.hasTriggered = true;

        Debug.Log(
            $"Height Line Triggered: {line.lineName} / " +
            $"高さライン発動：{line.lineName}"
        );

        // 触发后隐藏当前高度线
        // 発動後、現在の高さラインを非表示にする
        if (line.lineObject != null)
        {
            line.lineObject.SetActive(false);
        }

        // 执行对应事件
        // 対応するイベントを実行する
        ExecuteLineEvent(line);
    }

    /// <summary>
    /// 显示下一条高度线
    /// 次の高さラインを表示する
    /// </summary>
    private void AdvanceToNextLine()
    {
        currentLineIndex++;

        // 已经没有下一条高度线
        // 次の高さラインが存在しない
        if (AllLinesTriggered)
        {
            Debug.Log(
                "All Height Lines Triggered / " +
                "すべての高さラインが発動しました"
            );

            return;
        }

        HeightLineData nextLine =
            heightLines[currentLineIndex];

        if (nextLine == null ||
            nextLine.lineObject == null)
        {
            Debug.LogWarning(
                $"Next Height Line {currentLineIndex} is missing. / " +
                $"次の高さライン {currentLineIndex} が設定されていません。"
            );

            // 跳过无效高度线
            // 無効な高さラインを飛ばす
            AdvanceToNextLine();
            return;
        }

        // 显示下一条高度线
        // 次の高さラインを表示する
        nextLine.lineObject.SetActive(true);
    }
        
    /// <summary>
    /// 执行高度线对应事件
    /// 高さラインに対応するイベントを実行する
    /// </summary>
    private void ExecuteLineEvent(HeightLineData line)
    {
        if (line == null)
            return;

        switch (line.lineName)
        {
            case "SpecialSelection":
                if (flowManager != null)
                {
                    // 请求开启特殊方块选择
                    // 特殊ブロック選択を要求する
                    flowManager.RequestSpecialSelection();
                }
                else
                {
                    Debug.LogWarning(
                        "Flow Manager is missing. / " +
                        "フローマネージャーが設定されていません。"
                    );
                }

                break;

            case "Boss":
                // TODO：Boss事件
                // TODO：Bossイベント
                Debug.Log("Boss Event / Bossイベント");
                break;

            case "EnemyUpgrade":
                // TODO：敌人强化事件
                // TODO：敵強化イベント
                Debug.Log("Enemy Upgrade / 敵強化");
                break;
            case "CameraExpand":
                //TODO:カメラ拡張
                ExecuteCameraExpand();
                MoveObjects();
                break;

            default:
                Debug.LogWarning(
                    $"No event assigned: {line.lineName} / " +
                    $"イベント未設定：{line.lineName}"
                );
                break;
        }
    }

    /// <summary>
    /// 获取所有已落地方块的最高位置
    /// すべての着地済みブロックの最高位置を取得する
    /// </summary>
    private float GetHighestLandedBlockY()
    {
        float highestY = float.MinValue;

        BlockLanding[] blocks =
            FindObjectsByType<BlockLanding>(
                FindObjectsSortMode.None
            );

        foreach (BlockLanding block in blocks)
        {
            if (block == null || !block.IsLanded)
                continue;

            if (block.childBlocks == null)
                continue;

            foreach (Transform child in block.childBlocks)
            {
                if (child == null)
                    continue;

                float childTopY = child.position.y;

                if (useColliderTop)
                {
                    Collider2D childCollider =
                        child.GetComponent<Collider2D>();

                    if (childCollider != null)
                    {
                        // 使用Collider最顶部作为方块高度
                        // Colliderの最上端をブロックの高さとして使用する
                        childTopY =
                            childCollider.bounds.max.y;
                    }
                }

                highestY =
                    Mathf.Max(highestY, childTopY);
            }
        }

        return highestY;
    }

    /// <summary>
    /// 重置所有高度线
    /// すべての高さラインをリセットする
    /// </summary>
    public void ResetHeightLines()
    {
        InitializeHeightLines();
    }

    private void ExecuteCameraExpand()
    {
        StartCoroutine(CameraExpandRoutine());
    }

    /// <summary>
    /// 平滑移动相机并扩大视野
    /// カメラを滑らかに移動し、視野を拡大する
    /// </summary>
    private IEnumerator CameraExpandRoutine()
    {
        Camera cam = Camera.main;

        if (cam == null)
            yield break;

        // 起始位置
        // 開始位置
        Vector3 startPosition = cam.transform.position;

        // 目标位置（当前位置 + 向上偏移）
        // 目標位置（現在位置 + 上方向オフセット）
        Vector3 targetPosition =
            startPosition + Vector3.up * cameraMoveDistance;

        // 起始Size
        // 開始Size
        float startSize = cam.orthographicSize;

        // 目标Size（当前Size + 增量）
        // 目標Size（現在Size + 増加量）
        float targetSize =
            startSize + cameraSizeIncrease;

        float timer = 0f;

        while (timer < cameraMoveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / cameraMoveDuration);

            // 缓入缓出
            // イージング
            t = Mathf.SmoothStep(0f, 1f, t);

            // 相机位置
            // カメラ位置
            cam.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t);

            // 相机大小
            // カメラサイズ
            cam.orthographicSize =
                Mathf.Lerp(
                    startSize,
                    targetSize,
                    t);

            yield return null;
        }

        // 防止误差
        // 誤差補正
        cam.transform.position = targetPosition;
        cam.orthographicSize = targetSize;
    }

    private void MoveObjects()
    {
        if (leftEnemySpawnPoint != null)
        {
            leftEnemySpawnPoint.transform.position += Vector3.left * moveLeftDistance;
        }

        if(rightEnemySpawnPoint != null)
        {
            rightEnemySpawnPoint.transform.position += Vector3.right * moveRightDistance;
        }

        if(blockSpawner != null)
        {
            blockSpawner.transform.position += Vector3.up * moveUpDistance;
        }
    }
}