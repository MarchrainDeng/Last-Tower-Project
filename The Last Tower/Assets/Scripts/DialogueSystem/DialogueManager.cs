using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // 包含入场、正文和出场阶段
    // 登場・会話本文・退場の各段階を含む
    public static bool IsDialogueActive =>
        Instance != null && Instance.isDialogueActive;

    // 对话期间及结束帧禁止游戏输入
    // 会話中と終了フレームはゲーム入力を禁止する
    public static bool IsInputBlocked =>
        Instance != null &&
        (
            Instance.isDialogueActive ||
            Time.frameCount == Instance.closedFrame
        );

    // 动画期间及动画结束帧禁止所有输入，包括暂停输入
    // アニメーション中と終了フレームはポーズを含む入力を禁止する
    public static bool IsTransitionInputBlocked =>
        Instance != null &&
        (
            Instance.isTransitioning ||
            Time.frameCount == Instance.transitionFinishedFrame
        );

    [Header("UI")]

    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private RectTransform dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Canvas canvas;

    [Header("Position")]

    // 默认屏幕位置
    // デフォルト画面位置
    [SerializeField]
    private Vector2 defaultPosition =
        new Vector2(0.5f, 0.18f);

    // 对话框与世界目标之间的偏移
    // 会話枠とワールドターゲットのオフセット
    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 1.5f, 0f);

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive;

    private int openedFrame = -1;
    private int closedFrame = -1;

    private Transform worldTarget;
    private Action onDialogueFinished;
    private LocalizedString activeLocalizedLine;

    // 只在本次对话指定动画时使用
    // 今回の会話にアニメーションが指定された場合のみ使用する
    private DialogueSlideEffect activeSlideEffect;

    private bool isTransitioning;
    private bool isClosing;
    private int transitionFinishedFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate DialogueManager detected.");
            enabled = false;
            return;
        }

        Instance = this;

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeCurrentLine();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

        // 入场与出场期间不接受推进对话输入
        // 登場・退場中は会話送り入力を受け付けない
        if (isClosing || IsTransitionInputBlocked)
            return;

        if (GameStateManager.IsPaused)
            return;

        // 避免开始对话或动画结束的同一帧跳过文字
        // 会話開始・アニメーション終了フレームの文字送りを防ぐ
        if (Time.frameCount == openedFrame)
            return;

        bool confirmPressed = false;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            confirmPressed = true;
        }

        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            confirmPressed = true;
        }

        if (confirmPressed)
        {
            NextLine();
        }
    }

    private void LateUpdate()
    {
        // 动画期间不更新世界位置，避免覆盖动画坐标
        // アニメーション中はワールド位置更新による座標上書きを防ぐ
        if (isDialogueActive &&
            !isTransitioning &&
            !isClosing &&
            worldTarget != null)
        {
            UpdateWorldPosition();
        }
    }

    public void ShowDialogue(DialogueData data)
    {
        ShowDialogue(data, defaultPosition);
    }

    // 原接口：不使用动画
    // 既存の呼び出し：アニメーションなし
    public void ShowDialogue(
        DialogueData data,
        Vector2 normalizedPosition)
    {
        ShowDialogue(data, normalizedPosition, null);
    }

    // 新增接口：可指定独立的滑动效果
    // 追加の呼び出し：個別のスライド演出を指定可能
    public void ShowDialogue(
        DialogueData data,
        Vector2 normalizedPosition,
        DialogueSlideEffect slideEffect)
    {
        if (!CanStartDialogue(data))
            return;

        worldTarget = null;
        SetSlideEffect(slideEffect);

        StartDialogue(data);
        SetScreenPosition(normalizedPosition);
        BeginEntryTransition();
    }

    // 原接口：不使用动画
    // 既存の呼び出し：アニメーションなし
    public void ShowDialogue(
        DialogueData data,
        Transform target)
    {
        ShowDialogue(data, target, null);
    }

    public void ShowDialogue(
        DialogueData data,
        Transform target,
        DialogueSlideEffect slideEffect)
    {
        if (!CanStartDialogue(data))
            return;

        worldTarget = target;
        SetSlideEffect(slideEffect);

        StartDialogue(data);

        if (worldTarget != null)
        {
            UpdateWorldPosition();
        }
        else
        {
            SetScreenPosition(defaultPosition);
        }

        BeginEntryTransition();
    }

    private void SetSlideEffect(DialogueSlideEffect slideEffect)
    {
        activeSlideEffect = null;

        if (slideEffect == null)
            return;

        if (!slideEffect.CanAnimate)
        {
            Debug.LogWarning(
                "DialogueSlideEffect is not ready. " +
                "Check its component and Dialogue Panel reference.",
                slideEffect
            );
            return;
        }

        activeSlideEffect = slideEffect;
    }

    private bool CanStartDialogue(DialogueData data)
    {
        if (isDialogueActive)
            return false;

        if (GameStateManager.IsPaused)
            return false;

        if (data == null)
            return false;

        bool hasLocalizedLines =
            data.localizedLines != null &&
            data.localizedLines.Length > 0;

        bool hasLegacyLines =
            data.lines != null &&
            data.lines.Length > 0;

        if (!hasLocalizedLines && !hasLegacyLines)
        {
            Debug.LogWarning("DialogueData is empty.");
            return false;
        }

        return true;
    }

    private void StartDialogue(DialogueData data)
    {
        currentDialogue = data;
        currentLineIndex = 0;

        isDialogueActive = true;
        isClosing = false;
        isTransitioning = false;
        openedFrame = Time.frameCount;

        dialogueCanvas.SetActive(true);

        DisplayCurrentLine();
    }

    private void BeginEntryTransition()
    {
        Debug.Log(
            $"[对话动画] 开始检查，管理物体：{name}，" +
            $"动画组件：{(activeSlideEffect != null ? activeSlideEffect.name : "未指定")}",
            this
        );

        if (activeSlideEffect == null)
            return;

        isTransitioning = true;
        StartCoroutine(PlayEntryTransition());
    }

    private IEnumerator PlayEntryTransition()
    {
        yield return activeSlideEffect.PlayEnter();

        isTransitioning = false;
        transitionFinishedFrame = Time.frameCount;
        openedFrame = Time.frameCount;
    }

    private void DisplayCurrentLine()
    {
        UnsubscribeCurrentLine();

        if (currentDialogue.localizedLines != null &&
            currentDialogue.localizedLines.Length > 0)
        {
            activeLocalizedLine =
                currentDialogue.localizedLines[currentLineIndex];

            if (activeLocalizedLine == null ||
                activeLocalizedLine.IsEmpty)
            {
                dialogueText.text = "";
                return;
            }

            dialogueText.text = "";

            activeLocalizedLine.StringChanged +=
                OnLocalizedLineChanged;

            return;
        }

        dialogueText.text =
            currentDialogue.lines[currentLineIndex];
    }

    private void OnLocalizedLineChanged(string value)
    {
        if (!isDialogueActive)
            return;

        dialogueText.text = value;
    }

    private void UnsubscribeCurrentLine()
    {
        if (activeLocalizedLine == null)
            return;

        activeLocalizedLine.StringChanged -=
            OnLocalizedLineChanged;

        activeLocalizedLine = null;
    }

    private void NextLine()
    {
        if (currentDialogue == null)
            return;

        currentLineIndex++;

        int totalLines =
            currentDialogue.localizedLines != null &&
            currentDialogue.localizedLines.Length > 0
                ? currentDialogue.localizedLines.Length
                : currentDialogue.lines.Length;

        if (currentLineIndex >= totalLines)
        {
            EndDialogue();
            return;
        }

        DisplayCurrentLine();
    }

    public void EndDialogue()
    {
        // 防止动画期间或连续调用导致重复结束
        // 演出中や連続呼び出しによる重複終了を防ぐ
        if (!isDialogueActive ||
            isClosing ||
            IsTransitionInputBlocked)
        {
            return;
        }

        isClosing = true;

        if (activeSlideEffect != null &&
            activeSlideEffect.CanAnimate)
        {
            isTransitioning = true;
            StartCoroutine(PlayExitTransition());
        }
        else
        {
            FinishDialogue();
        }
    }

    private IEnumerator PlayExitTransition()
    {
        yield return activeSlideEffect.PlayExit();

        isTransitioning = false;
        transitionFinishedFrame = Time.frameCount;

        FinishDialogue();
    }

    private void FinishDialogue()
    {
        isDialogueActive = false;
        isClosing = false;
        isTransitioning = false;
        closedFrame = Time.frameCount;

        UnsubscribeCurrentLine();

        worldTarget = null;
        currentDialogue = null;

        // 先隐藏，再恢复位置，避免画面中出现位置跳变
        // 先に非表示にしてから位置を戻し、画面上の瞬間移動を防ぐ
        dialogueCanvas.SetActive(false);

        if (activeSlideEffect != null)
        {
            activeSlideEffect.RestorePosition();
        }

        activeSlideEffect = null;

        // 出场完成后才执行结束回调
        // 退場完了後に終了コールバックを実行する
        Action callback = onDialogueFinished;
        onDialogueFinished = null;

        callback?.Invoke();
    }

    public void SetOnDialogueFinished(Action callback)
    {
        onDialogueFinished = callback;
    }

    private void SetScreenPosition(Vector2 position)
    {
        // 保留原有方式，使用Inspector设置的显示位置
        // 既存の方式を維持し、Inspectorの表示位置を使用する
    }

    private void UpdateWorldPosition()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null || worldTarget == null)
            return;

        Vector3 worldPosition =
            worldTarget.position + worldOffset;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                mainCamera,
                worldPosition
            );

        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out Vector2 localPosition))
        {
            dialoguePanel.anchoredPosition = localPosition;
        }
    }
}