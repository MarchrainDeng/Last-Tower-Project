using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // 对话是否正在显示
    // 会話が表示中かどうか
    public static bool IsDialogueActive =>
        Instance != null && Instance.isDialogueActive;

    // 对话结束的同一帧也阻止游戏输入
    // 会話終了フレームもゲーム入力を禁止する
    public static bool IsInputBlocked =>
        Instance != null &&
        (Instance.isDialogueActive ||
         Time.frameCount == Instance.closedFrame);

    [Header("UI")]

    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private RectTransform dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Canvas canvas;

    [Header("Position")]

    // 默认屏幕位置，左下角为(0,0)，右上角为(1,1)
    // デフォルト画面位置
    [SerializeField]
    private Vector2 defaultPosition = new Vector2(0.5f, 0.18f);

    // 对话框与世界目标之间的偏移
    // 会話枠とワールドターゲットのオフセット
    [SerializeField]
    private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive;

    private int openedFrame = -1;
    private int closedFrame = -1;

    private Transform worldTarget;
    private Action onDialogueFinished;

    // 当前正在显示的多语言文本
    // 現在表示中のローカライズされたテキスト
    private LocalizedString activeLocalizedLine;

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
        if (Instance == this)
        {
            Instance = null;
        }
    }


    private void Update()
    {
        if (!isDialogueActive)
            return;

        // 暂停菜单打开时，不处理对话输入
        // ポーズメニュー表示中は会話入力を処理しない
        if (GameStateManager.IsPaused)
            return;

        // 防止触发对话的同一次点击立即跳过第一句
        // 会話開始時の入力で最初のセリフを飛ばさない
        if (Time.frameCount == openedFrame)
            return;

        bool confirmPressed = false;

        // 鼠标左键
        // マウス左ボタン
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            confirmPressed = true;
        }

        // 手柄A键
        // ゲームパッドAボタン
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
        // 有指定世界目标时，持续跟随目标
        // ワールドターゲットがある場合は追従する
        if (isDialogueActive && worldTarget != null)
        {
            UpdateWorldPosition();
        }
    }


    /// <summary>
    /// 在默认位置显示对话
    /// デフォルト位置で会話を表示
    /// </summary>
    public void ShowDialogue(DialogueData data)
    {
        ShowDialogue(data, defaultPosition);
    }


    /// <summary>
    /// 在指定的屏幕比例位置显示对话
    /// 指定した画面位置で会話を表示
    /// </summary>
    public void ShowDialogue(
        DialogueData data,
        Vector2 normalizedPosition)
    {
        if (!CanStartDialogue(data))
            return;

        worldTarget = null;

        StartDialogue(data);

        SetScreenPosition(normalizedPosition);
    }


    /// <summary>
    /// 在世界目标附近显示对话
    /// ワールドターゲット付近に会話を表示
    /// </summary>
    public void ShowDialogue(
        DialogueData data,
        Transform target)
    {
        if (!CanStartDialogue(data))
            return;

        worldTarget = target;

        StartDialogue(data);

        if (worldTarget != null)
        {
            UpdateWorldPosition();
        }
        else
        {
            SetScreenPosition(defaultPosition);
        }
    }


    private bool CanStartDialogue(DialogueData data)
    {
        if (isDialogueActive)
            return false;

        if (GameStateManager.IsPaused)
            return false;

        if (data == null)
            return false;

        // 是否存在多语言文本
        // 多言語テキストが存在するか
        bool hasLocalizedLines =
            data.localizedLines != null &&
            data.localizedLines.Length > 0;

        // 是否存在旧版文本
        // 旧バージョンのテキストが存在するか
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
        openedFrame = Time.frameCount;

        dialogueCanvas.SetActive(true);

        DisplayCurrentLine();
    }


    /// <summary>
    /// 显示当前句
    /// 現在のセリフを表示
    /// </summary>
    private void DisplayCurrentLine()
    {
        // 取消上一句文本的监听
        // 前のセリフのイベント登録を解除する
        UnsubscribeCurrentLine();

        // 优先使用多语言文本
        // 多言語テキストを優先して使用する
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

            // 等待文本加载，并在切换语言时自动更新
            // 読み込み完了時と言語変更時に自動更新する
            dialogueText.text = "";

            activeLocalizedLine.StringChanged +=
                OnLocalizedLineChanged;

            return;
        }

        // 兼容旧版文本
        // 旧バージョンのテキストに対応する
        dialogueText.text =
            currentDialogue.lines[currentLineIndex];
    }

    /// <summary>
    /// 更新多语言文本
    /// ローカライズされたテキストを更新する
    /// </summary>
    private void OnLocalizedLineChanged(string value)
    {
        if (!isDialogueActive)
            return;

        dialogueText.text = value;
    }


    /// <summary>
    /// 取消当前文本监听
    /// 現在のテキストイベント登録を解除する
    /// </summary>
    private void UnsubscribeCurrentLine()
    {
        if (activeLocalizedLine == null)
            return;

        activeLocalizedLine.StringChanged -=
            OnLocalizedLineChanged;

        activeLocalizedLine = null;
    }


    /// <summary>
    /// 切换下一句
    /// 次のセリフに切り替える
    /// </summary>
    private void NextLine()
    {
        if (currentDialogue == null)
            return;

        currentLineIndex++;

        // 优先获取多语言文本数量
        // 多言語テキストの数を優先して取得する
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


    /// <summary>
    /// 结束对话
    /// 会話を終了する
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive)
            return;

        isDialogueActive = false;
        closedFrame = Time.frameCount;

        UnsubscribeCurrentLine();

        worldTarget = null;
        currentDialogue = null;

        dialogueCanvas.SetActive(false);

        Action callback = onDialogueFinished;
        onDialogueFinished = null;

        callback?.Invoke();
    }


    /// <summary>
    /// 设置对话结束回调
    /// 会話終了時のコールバックを設定する
    /// </summary>
    public void SetOnDialogueFinished(Action callback)
    {
        onDialogueFinished = callback;
    }


    /// <summary>
    /// 设置屏幕相对位置
    /// 画面の相対位置を設定する
    /// </summary>
    private void SetScreenPosition(Vector2 position)
    {
        // 使用Inspector中预先设置的位置
        // Inspectorで設定された位置を使用する
    }


    /// <summary>
    /// 将世界坐标转换为UI位置
    /// ワールド座標をUI座標へ変換する
    /// </summary>
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

        // Overlay Canvas的UI相机参数为null
        // Overlay Canvasの場合はカメラにnullを指定する
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