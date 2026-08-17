using System.Collections;
using TMPro;
using UnityEngine;

public class SpecialAttackArea : MonoBehaviour
{
    [Header("Power Check")]

    // 上下方向检测盒大小
    [SerializeField]
    private Vector2 verticalCheckSize =
        new Vector2(0.4f, 0.08f);

    // 上下方向检测偏移
    [SerializeField]
    private float verticalCheckOffset = 0.29f;

    // 左右方向检测盒大小
    [SerializeField]
    private Vector2 horizontalCheckSize =
        new Vector2(0.08f, 0.4f);

    // 左右方向检测偏移
    [SerializeField]
    private float horizontalCheckOffset = 0.29f;

    // 动力方块Layer
    [SerializeField]
    private LayerMask powerBlockLayer;

    [Header("References")]

    // 构成该区域的所有格子
    [SerializeField]
    private Transform[] childCells;

    // 区域中央的倒计时文字
    [SerializeField]
    private TMP_Text countdownText;

    [Header("Countdown")]

    // 启动后的倒计时时间
    [SerializeField]
    private float countdownDuration = 3f;

    // 当前是否已经启动
    [SerializeField]
    private bool isActivated;

    // 是否已经完成
    [SerializeField]
    private bool hasFinished;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (hasFinished)
            return;

        // 已经启动后不再重复检测
        if (isActivated)
            return;

        CheckPowerConnection();
    }

    /// <summary>
    /// 检查是否连接到了已通电的动力方块
    /// </summary>
    private void CheckPowerConnection()
    {
        if (childCells == null)
            return;

        foreach (Transform cell in childCells)
        {
            if (cell == null)
                continue;

            // 上
            if (CheckArea(
                cell,
                Vector2.up,
                verticalCheckOffset,
                verticalCheckSize))
            {
                Activate();
                return;
            }

            // 下
            if (CheckArea(
                cell,
                Vector2.down,
                verticalCheckOffset,
                verticalCheckSize))
            {
                Activate();
                return;
            }

            // 左
            if (CheckArea(
                cell,
                Vector2.left,
                horizontalCheckOffset,
                horizontalCheckSize))
            {
                Activate();
                return;
            }

            // 右
            if (CheckArea(
                cell,
                Vector2.right,
                horizontalCheckOffset,
                horizontalCheckSize))
            {
                Activate();
                return;
            }
        }
    }

    /// <summary>
    /// 检查指定方向是否存在已通电的动力方块
    /// </summary>
    private bool CheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        float angle =
            cell.eulerAngles.z;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                checkPosition,
                checkSize,
                angle,
                powerBlockLayer
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            // 排除自身
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            PowerBlock powerBlock =
                hit.GetComponentInParent<PowerBlock>();

            if (powerBlock == null)
                continue;

            // 只有已经通电的动力方块才能启动区域
            if (powerBlock.isPowered)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 启动特殊区域
    /// </summary>
    private void Activate()
    {
        if (isActivated || hasFinished)
            return;

        isActivated = true;

        countdownCoroutine =
            StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// 启动后的倒计时
    /// </summary>
    private IEnumerator CountdownRoutine()
    {
        float timer =
            countdownDuration;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        while (timer > 0f)
        {
            if (countdownText != null)
            {
                countdownText.text =
                    Mathf.CeilToInt(timer).ToString();
            }

            timer -= Time.deltaTime;

            yield return null;
        }

        if (countdownText != null)
        {
            countdownText.text = "0";
        }

        hasFinished = true;

        // 倒计时完成后执行事件
        OnCountdownFinished();

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        countdownCoroutine = null;
    }

    /// <summary>
    /// 倒计时完成后的事件接口
    /// </summary>
    private void OnCountdownFinished()
    {
        Debug.Log("Special Attack Area Countdown Finished");

        // ==========================================
        // 在这里添加倒计时结束后的事件
        // ==========================================

        // 示例：
        // AttackBoss();
        // SpawnSomething();
        // StartBossAnimation();
        // flowManager.EnterFinalPhase();
    }

    private void OnDrawGizmosSelected()
    {
        if (childCells == null)
            return;

        Gizmos.color = Color.cyan;

        foreach (Transform cell in childCells)
        {
            if (cell == null)
                continue;

            DrawCheckArea(
                cell,
                Vector2.up,
                verticalCheckOffset,
                verticalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.down,
                verticalCheckOffset,
                verticalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.left,
                horizontalCheckOffset,
                horizontalCheckSize
            );

            DrawCheckArea(
                cell,
                Vector2.right,
                horizontalCheckOffset,
                horizontalCheckSize
            );
        }
    }

    private void DrawCheckArea(
        Transform cell,
        Vector2 localDirection,
        float checkOffset,
        Vector2 checkSize)
    {
        Vector2 worldDirection =
            cell.TransformDirection(localDirection).normalized;

        Vector2 checkPosition =
            (Vector2)cell.position +
            worldDirection * checkOffset;

        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                checkPosition,
                Quaternion.Euler(
                    0f,
                    0f,
                    cell.eulerAngles.z
                ),
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            checkSize
        );

        Gizmos.matrix = oldMatrix;
    }
}