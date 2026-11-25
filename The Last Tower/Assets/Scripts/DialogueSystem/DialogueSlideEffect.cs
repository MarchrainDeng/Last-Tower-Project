using System.Collections;
using UnityEngine;

public class DialogueSlideEffect : MonoBehaviour
{
    [Header("References")]

    // 整个对话框的RectTransform，包括背景和文字
    // 背景とテキストを含む会話枠全体のRectTransform
    [SerializeField]
    private RectTransform dialoguePanel;

    [Header("Animation Settings")]

    // 入场和出场各自的持续时间
    // 登場・退場それぞれの所要時間
    [Min(0f)]
    [SerializeField]
    private float duration = 0.2f;

    // 移出屏幕后额外留出的距离
    // 画面外へ移動した後の余白
    [Min(0f)]
    [SerializeField]
    private float offscreenPadding = 30f;

    private Vector2 displayPosition;
    private bool hasDisplayPosition;

    // 即使对话Canvas尚未启用，也允许检查其结构
    // 会話Canvasが無効な状態でも構造を確認できるようにする
    public bool CanAnimate =>
        isActiveAndEnabled &&
        dialoguePanel != null &&
        dialoguePanel.parent is RectTransform &&
        dialoguePanel.GetComponentInParent<Canvas>(true) != null;

    /// <summary>
    /// 从屏幕上方进入当前位置
    /// 画面上方から現在の表示位置へ移動する
    /// </summary>
    public IEnumerator PlayEnter()
    {
        Debug.Log(
            $"[对话动画] PlayEnter执行，CanAnimate：{CanAnimate}",
            this
        );

        if (!CanAnimate)
            yield break;

        Canvas.ForceUpdateCanvases();

        displayPosition = dialoguePanel.anchoredPosition;
        hasDisplayPosition = true;

        Vector2 hiddenPosition = CalculateHiddenPosition();

        Debug.Log(
            $"[对话动画] 移动物体：{dialoguePanel.name}，" +
            $"起点：{hiddenPosition}，终点：{displayPosition}",
            dialoguePanel
        );

        dialoguePanel.anchoredPosition = hiddenPosition;

        yield return MovePanel(
            hiddenPosition,
            displayPosition
        );

        Debug.Log("[对话动画] 入场完成", this);
    }

    /// <summary>
    /// 从当前位置向上移出屏幕
    /// 現在位置から画面上方へ退場する
    /// </summary>
    public IEnumerator PlayExit()
    {
        if (!CanAnimate)
            yield break;

        Canvas.ForceUpdateCanvases();

        displayPosition = dialoguePanel.anchoredPosition;
        hasDisplayPosition = true;

        Vector2 hiddenPosition = CalculateHiddenPosition();

        yield return MovePanel(
            displayPosition,
            hiddenPosition
        );
    }

    /// <summary>
    /// 对话UI隐藏后恢复显示位置，供下一次使用
    /// 会話UIを非表示にした後、次回用に表示位置を復元する
    /// </summary>
    public void RestorePosition()
    {
        if (dialoguePanel == null || !hasDisplayPosition)
            return;

        dialoguePanel.anchoredPosition = displayPosition;
        hasDisplayPosition = false;
    }

    private Vector2 CalculateHiddenPosition()
    {
        RectTransform parentRect =
            dialoguePanel.parent as RectTransform;

        // 搜索时包含停用的父物体
        // 無効な親オブジェクトも検索対象に含める
        Canvas parentCanvas =
            dialoguePanel.GetComponentInParent<Canvas>(true);

        RectTransform canvasRect =
            parentCanvas.rootCanvas.transform as RectTransform;

        Vector3[] canvasCorners = new Vector3[4];
        Vector3[] panelCorners = new Vector3[4];

        canvasRect.GetWorldCorners(canvasCorners);
        dialoguePanel.GetWorldCorners(panelCorners);

        float canvasTop = float.NegativeInfinity;
        float panelBottom = float.PositiveInfinity;

        // 将两者边界转换到同一个父物体坐标系
        // 両方の境界を同じ親オブジェクトの座標系へ変換する
        for (int i = 0; i < 4; i++)
        {
            float canvasY =
                parentRect.InverseTransformPoint(
                    canvasCorners[i]
                ).y;

            float panelY =
                parentRect.InverseTransformPoint(
                    panelCorners[i]
                ).y;

            canvasTop = Mathf.Max(canvasTop, canvasY);
            panelBottom = Mathf.Min(panelBottom, panelY);
        }

        float moveDistance = Mathf.Max(
            0f,
            canvasTop - panelBottom + offscreenPadding
        );

        return dialoguePanel.anchoredPosition +
            Vector2.up * moveDistance;
    }

    private IEnumerator MovePanel(Vector2 from, Vector2 to)
    {
        if (dialoguePanel == null)
            yield break;

        if (duration <= 0f)
        {
            dialoguePanel.anchoredPosition = to;
            yield break;
        }

        float elapsed = 0f;

        dialoguePanel.anchoredPosition = from;

        while (elapsed < duration)
        {
            yield return null;

            if (dialoguePanel == null)
                yield break;

            // 使用不受timeScale影响的时间，避免动画卡住
            // timeScaleに依存しない時間を使用して停止を防ぐ
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            dialoguePanel.anchoredPosition =
                Vector2.LerpUnclamped(from, to, t);
        }

        dialoguePanel.anchoredPosition = to;
    }
}