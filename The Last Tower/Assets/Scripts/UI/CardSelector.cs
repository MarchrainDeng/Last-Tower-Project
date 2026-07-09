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

    [Header("Input")]
    // 摇杆触发阈值
    // スティック入力の判定値
    public float stickThreshold = 0.5f;

    // 摇杆回中阈值
    // スティック中央戻り判定値
    public float returnThreshold = 0.2f;

    private int currentIndex = 0;
    private bool stickReturnedToCenter = true;

    private void Start()
    {
        RefreshCards();
        UpdateCardVisuals();
    }

    private void Update()
    {
        HandleSelection();
        HandleConfirm();
    }

    private void RefreshCards()
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
    }

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

    private void SpawnSelectedBlock()
    {
        BlockOption option = cards[currentIndex].GetOption();

        if (option == null || option.blockPrefab == null || spawnPoint == null)
            return;

        Instantiate(option.blockPrefab, spawnPoint.position, Quaternion.identity);
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
