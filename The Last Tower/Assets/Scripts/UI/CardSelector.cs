using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

/*
----------------------------------------
【功能 / 機能】
通过左摇杆控制方块卡牌选择

左スティルでブロックカードの選択を操作します

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/09

---------------------------------------
*/
public class CardSelector : MonoBehaviour
{
    [Header("Cards")]
    // 场上显示的卡牌UI
    // 表示されるカードUI
    public BlockCardUI[] cards;

    [Header("Spawn")]
    // 方块生成位置
    // ブロック生成位置
    public Transform spawnPoint;

    // 方块选择流程管理器
    // ブロック選択フローマネージャー
    [SerializeField] private BlockSelectionFlowManager flowManager;


    [Header("Input")]
    // 摇杆触发阈值
    // スティック入力の判定値
    public float stickThreshold = 0.5f;

    // 摇杆回中阈值
    // スティック中央戻り判定値
    public float returnThreshold = 0.2f;

    private int currentIndex = 0;
    private bool stickReturnedToCenter = true;

    // 是否允许处理卡牌输入
    // カード入力を処理可能か
    private bool inputEnabled = false;

    private void Start()
    {
        RefreshCards();
        UpdateCardVisuals();
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        HandleSelection();
        HandleConfirm();
    }

    /// <summary>
    /// 设置是否允许输入
    /// 入力を許可するか設定する
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        // 每次重新开启时让摇杆可以重新接受输入
        // 再度有効化した時にスティック入力を受け付けられるようにする
        stickReturnedToCenter = true;
    }

    public void RefreshCards()
    {
        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning("Cards is empty / Cardsが設定されていません");
            return;
        }

        foreach (BlockCardUI card in cards)
        {
            if (card == null)
                continue;

            card.RefreshCard();
        }

        // 每次重新选择时默认选择第一张
        // 選択開始時は最初のカードを選択する
        currentIndex = 0;

        UpdateCardVisuals();
    }

    /// <summary>
    /// 处理左右选择
    /// 左右選択を処理する
    /// </summary>
    private void HandleSelection()
    {
        float x = 0f;

        if (Gamepad.current != null)
            x = Gamepad.current.leftStick.x.ReadValue();

        if (Mathf.Abs(x) < returnThreshold)
            stickReturnedToCenter = true;

        if (stickReturnedToCenter)
        {
            if (x > stickThreshold)
            {
                SelectNextCard();
                stickReturnedToCenter = false;
            }
            else if (x < -stickThreshold)
            {
                SelectPreviousCard();
                stickReturnedToCenter = false;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
                SelectPreviousCard();

            if (Keyboard.current.dKey.wasPressedThisFrame)
                SelectNextCard();
        }
    }

    private void HandleConfirm()
    {
        bool confirmPressed = false;

        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
            confirmPressed = true;

        if (Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame)
            confirmPressed = true;

        if (confirmPressed)
            SpawnSelectedBlock();
    }

    /// <summary>
    /// 生成当前选择的方块
    /// 現在選択中のブロックを生成する
    /// </summary>
    private void SpawnSelectedBlock()
    {
        if (cards == null ||
            cards.Length == 0 ||
            currentIndex < 0 ||
            currentIndex >= cards.Length)
        {
            return;
        }

        BlockOption option = cards[currentIndex].GetOption();

        if (option == null)
        {
            Debug.LogWarning(
                "Selected card has no data. / 選択中カードにデータがありません。"
            );
            return;
        }

        if (option.blockPrefab == null)
        {
            Debug.LogWarning(
                "Block Prefab is missing. / Block Prefabが設定されていません。"
            );
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                "Spawn Point is missing. / Spawn Pointが設定されていません。"
            );
            return;
        }

        // 当前卡牌对应的方块を生成
        // 現在のカードに対応するブロックを生成する
        GameObject spawnedBlock = Instantiate(
            option.blockPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        // 将流程管理器传递给生成的方块
        // 生成したブロックにフローマネージャーを渡す
        BlockLanding landing = spawnedBlock.GetComponent<BlockLanding>();
        BlockMoveController controller = spawnedBlock.GetComponent<BlockMoveController>();

        if (landing != null)
        {
            landing.SetFlowManager(flowManager);
            controller.SetFlowManager(flowManager);
        }
        else
        {
            Debug.LogWarning(
                "Spawned block has no BlockLanding component. / " +
                "生成したブロックにBlockLandingがありません。"
            );
        }

        // 选择结束，隐藏卡牌UI
        // 選択終了後、カードUIを非表示にする
        if (flowManager != null)
        {
            flowManager.CloseBlockSelection();
        }
    }

    private void SelectNextCard()
    {
        currentIndex++;
        if (currentIndex >= cards.Length)
            currentIndex = 0;

        UpdateCardVisuals();
    }

    private void SelectPreviousCard()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = cards.Length - 1;

        UpdateCardVisuals();
    }

    private void UpdateCardVisuals()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].SetSelected(i == currentIndex);
        }
    }
}
