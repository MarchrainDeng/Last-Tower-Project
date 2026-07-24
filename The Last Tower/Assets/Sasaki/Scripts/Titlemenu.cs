using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面メニュー
///
/// 【操作】
/// - スティック上下で選択
/// - Aボタンで決定
///
/// 【Inspectorでアサインするもの】
/// - menuItems : 4項目分の設定（下記MenuItemクラス参照）
/// - gameSceneName : ゲーム開始で遷移するシーン名
/// - settingsMenu  : SettingsMenuコンポーネント
/// - staffRollObject : スタッフロールのGameObject（SetActiveで表示）
/// </summary>
public class TitleMenu : MonoBehaviour
{
    public enum MenuAction
    {
        StartGame,   // ゲームスタート
        OpenSettings,// 設定
        StaffRoll,   // スタッフロール
        Quit,        // 終了
    }

    [System.Serializable]
    public class MenuItem
    {
        public MenuAction action;
        public Image targetImage;   // この項目のImage
        public Sprite normalSprite;  // 通常時の画像
        public Sprite selectedSprite;// 選択中の画像
    }

    [Header("── メニュー項目（上から順） ──────")]
    public MenuItem[] menuItems = new MenuItem[4];

    [Header("── ゲームスタート ──────────────")]
    public string gameSceneName = "GameScene";

    [Header("── スタッフロール ──────────────")]
    public GameObject staffRollObject;

    [Header("── 入力設定 ────────────────────")]
    public float stickDeadZone = 0.5f;
    public float inputCooldown = 0.2f; // 連続入力を防ぐ間隔

    [Header("── 透明度設定 ──────────────────")]
    [Range(0, 255)] public int normalAlpha = 120; // 非選択時の透明度（0-255）
    [Range(0, 255)] public int selectedAlpha = 255; // 選択中の透明度（0-255）

    int selectedIndex = 0;
    float inputTimer = 0f;

    void Start()
    {
        UpdateVisuals();
    }

    void Update()
    {
        if (Gamepad.current == null) return;

        // 設定画面が開いている間は操作を無効化
        if (GameStateManager.IsPaused) return;

        // スタッフロール表示中はBボタンで閉じる
        if (staffRollObject != null && staffRollObject.activeSelf)
        {
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
                staffRollObject.SetActive(false);
            return;
        }

        inputTimer -= Time.unscaledDeltaTime;

        float vertical = Gamepad.current.leftStick.y.ReadValue();

        if (inputTimer <= 0f)
        {
            if (vertical > stickDeadZone)
            {
                MoveSelection(-1);
                inputTimer = inputCooldown;
            }
            else if (vertical < -stickDeadZone)
            {
                MoveSelection(1);
                inputTimer = inputCooldown;
            }
        }

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ConfirmSelection();
        }
    }

    void MoveSelection(int direction)
    {
        selectedIndex = (selectedIndex + direction + menuItems.Length) % menuItems.Length;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            var item = menuItems[i];
            if (item.targetImage == null) continue;

            bool isSelected = (i == selectedIndex);

            item.targetImage.sprite = isSelected
                ? item.selectedSprite
                : item.normalSprite;

            var c = item.targetImage.color;
            float alpha = (isSelected ? selectedAlpha : normalAlpha) / 255f;
            item.targetImage.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    void ConfirmSelection()
    {
        var item = menuItems[selectedIndex];

        switch (item.action)
        {
            case MenuAction.StartGame:
                SceneManager.LoadScene(gameSceneName);
                break;

            case MenuAction.OpenSettings:
                if (SettingsMenu.Instance != null)
                    SettingsMenu.Instance.Toggle();
                else
                    Debug.LogWarning("[TitleMenu] SettingsMenu.Instance が見つかりません");
                break;

            case MenuAction.StaffRoll:
                if (staffRollObject != null)
                    staffRollObject.SetActive(true);
                break;

            case MenuAction.Quit:
                Application.Quit();
#if UNITY_EDITOR
                Debug.Log("[TitleMenu] Application.Quit()（エディタでは実際には終了しません）");
#endif
                break;
        }
    }
}