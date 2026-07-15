using UnityEngine;
using UnityEngine.InputSystem;

/*
----------------------------------------
【功能 / 機能】
通过左摇杆或键盘控制方块卡牌选择。
根据普通选择或特殊选择刷新对应卡牌，
确认后生成当前选择的方块。

左スティックまたはキーボードでブロックカードを選択する。
通常選択または特殊選択に応じてカードを更新し、
決定後に現在選択中のブロックを生成する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/09

【更新 / 更新】
2026/07/15：
対応普通选择与特殊选择。
通常選択と特殊選択に対応。
----------------------------------------
*/

public class CardSelector : MonoBehaviour
{
    [Header("Cards")]
    // 场上显示的卡牌UI
    // 表示されるカードUI
    [SerializeField] private BlockCardUI[] cards;

    [Header("Spawn")]
    // 方块生成位置
    // ブロック生成位置
    [SerializeField] private Transform spawnPoint;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    [SerializeField] private BlockSelectionFlowManager flowManager;

    [Header("Input")]
    // 摇杆触发阈值
    // スティック入力の判定値
    [SerializeField] private float stickThreshold = 0.5f;

    // 摇杆回中阈值
    // スティック中央復帰の判定値
    [SerializeField] private float returnThreshold = 0.2f;

    // 当前选择的卡牌编号
    // 現在選択中のカード番号
    private int currentIndex;

    // 摇杆是否已经回中
    // スティックが中央に戻ったか
    private bool stickReturnedToCenter = true;

    // 是否允许处理卡牌输入
    // カード入力を処理可能か
    private bool inputEnabled;

    // 防止一帧内重复确认生成
    // 同一フレームでの重複生成を防止する
    private bool isConfirming;

    private void Awake()
    {
        // 自动查找流程管理器
        // フローマネージャーを自動検索する
        if (flowManager == null)
        {
            flowManager = FindFirstObjectByType<BlockSelectionFlowManager>();
        }
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        HandleSelection();
        HandleConfirm();
    }

    /// <summary>
    /// 设置是否允许卡牌输入
    /// カード入力を許可するか設定する
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        // 每次重新开启选择时允许摇杆重新输入
        // 選択を再開するたびにスティック入力を再許可する
        stickReturnedToCenter = true;

        // 重新开启选择时解除确认锁
        // 選択再開時に決定ロックを解除する
        if (enabled)
        {
            isConfirming = false;
        }
    }

    /// <summary>
    /// 根据选择类型刷新全部卡牌
    /// 選択タイプに応じてすべてのカードを更新する
    /// </summary>
    public void RefreshCards(BlockSelectionType selectionType)
    {
        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning(
                "Cards is empty. / カードが設定されていません。"
            );
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                Debug.LogWarning(
                    $"Card {i} is missing. / Card {i} が設定されていません。"
                );
                continue;
            }

            // 根据普通或特殊选择刷新对应卡池
            // 通常または特殊選択に応じてカードを更新する
            cards[i].RefreshCard(selectionType);
        }

        // 每次开启选择时默认选择第一张有效卡牌
        // 選択開始時は最初の有効なカードを選択する
        currentIndex = FindFirstValidCardIndex();

        UpdateCardVisuals();
    }

    /// <summary>
    /// 查找第一张有效卡牌
    /// 最初の有効なカードを探す
    /// </summary>
    private int FindFirstValidCardIndex()
    {
        if (cards == null)
            return 0;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
                return i;
        }

        return 0;
    }

    /// <summary>
    /// 处理左右选择输入
    /// 左右選択入力を処理する
    /// </summary>
    private void HandleSelection()
    {
        float stickInput = 0f;

        // 手柄左摇杆输入
        // ゲームパッド左スティック入力
        if (Gamepad.current != null)
        {
            stickInput = Gamepad.current.leftStick.x.ReadValue();
        }

        // 摇杆回到中心后，允许下一次选择
        // スティックが中央に戻った後、次の選択を許可する
        if (Mathf.Abs(stickInput) < returnThreshold)
        {
            stickReturnedToCenter = true;
        }
        else if (stickReturnedToCenter)
        {
            if (stickInput > stickThreshold)
            {
                SelectNextCard();
                stickReturnedToCenter = false;
            }
            else if (stickInput < -stickThreshold)
            {
                SelectPreviousCard();
                stickReturnedToCenter = false;
            }
        }

        // 键盘输入
        // キーボード入力
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            SelectPreviousCard();
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            SelectNextCard();
        }
    }

    /// <summary>
    /// 处理确认输入
    /// 決定入力を処理する
    /// </summary>
    private void HandleConfirm()
    {
        if (isConfirming)
            return;

        bool confirmPressed = false;

        // 手柄A键
        // ゲームパッドAボタン
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            confirmPressed = true;
        }

        // 键盘Enter或小键盘Enter
        // EnterキーまたはテンキーEnter
        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            confirmPressed = true;
        }

        if (!confirmPressed)
            return;

        isConfirming = true;
        SpawnSelectedBlock();
    }

    /// <summary>
    /// 生成当前选择的方块
    /// 現在選択中のブロックを生成する
    /// </summary>
    private void SpawnSelectedBlock()
    {
        if (cards == null || cards.Length == 0)
        {
            isConfirming = false;
            return;
        }

        if (currentIndex < 0 || currentIndex >= cards.Length)
        {
            Debug.LogWarning(
                "Current card index is invalid. / " +
                "現在のカード番号が無効です。"
            );

            isConfirming = false;
            return;
        }

        BlockCardUI selectedCard = cards[currentIndex];

        if (selectedCard == null)
        {
            Debug.LogWarning(
                "Selected card is missing. / " +
                "選択中のカードが設定されていません。"
            );

            isConfirming = false;
            return;
        }

        BlockOption option = selectedCard.GetOption();

        if (option == null || option.blockPrefab == null)
        {
            Debug.LogWarning(
                "Selected card data is missing. / " +
                "選択中カードのデータが不足しています。"
            );

            isConfirming = false;
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                "Spawn Point is missing. / " +
                "Spawn Pointが設定されていません。"
            );

            isConfirming = false;
            return;
        }

        // 生成当前卡牌对应的方块
        // 現在のカードに対応するブロックを生成する
        GameObject spawnedBlock = Instantiate(
            option.blockPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        // 为落地脚本设置流程管理器
        // 着地スクリプトにフローマネージャーを設定する
        BlockLanding landing =
            spawnedBlock.GetComponent<BlockLanding>();

        if (landing != null)
        {
            landing.SetFlowManager(flowManager);
        }
        else
        {
            Debug.LogWarning(
                "Spawned block has no BlockLanding. / " +
                "生成したブロックにBlockLandingがありません。"
            );
        }

        // 为越界脚本设置流程管理器
        // 範囲外判定スクリプトにフローマネージャーを設定する
        /*
        ActiveBlockOutOfBounds outOfBounds =
            spawnedBlock.GetComponent<ActiveBlockOutOfBounds>();
        

        if (outOfBounds != null)
        {
            outOfBounds.SetFlowManager(flowManager);
        }*/

        // 本次选择完成
        // 今回の選択を完了する
        if (flowManager != null)
        {
            flowManager.CompleteSelection();
        }
        else
        {
            Debug.LogWarning(
                "Flow Manager is missing. / " +
                "フローマネージャーが設定されていません。"
            );

            isConfirming = false;
        }
    }

    /// <summary>
    /// 选择下一张有效卡牌
    /// 次の有効なカードを選択する
    /// </summary>
    private void SelectNextCard()
    {
        if (cards == null || cards.Length == 0)
            return;

        int startIndex = currentIndex;

        do
        {
            currentIndex++;

            if (currentIndex >= cards.Length)
            {
                currentIndex = 0;
            }

            if (cards[currentIndex] != null)
                break;

        } while (currentIndex != startIndex);

        UpdateCardVisuals();
    }

    /// <summary>
    /// 选择上一张有效卡牌
    /// 前の有効なカードを選択する
    /// </summary>
    private void SelectPreviousCard()
    {
        if (cards == null || cards.Length == 0)
            return;

        int startIndex = currentIndex;

        do
        {
            currentIndex--;

            if (currentIndex < 0)
            {
                currentIndex = cards.Length - 1;
            }

            if (cards[currentIndex] != null)
                break;

        } while (currentIndex != startIndex);

        UpdateCardVisuals();
    }

    /// <summary>
    /// 更新所有卡牌的选中显示
    /// すべてのカードの選択表示を更新する
    /// </summary>
    private void UpdateCardVisuals()
    {
        if (cards == null)
            return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            cards[i].SetSelected(i == currentIndex);
        }
    }
}