using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面メニュー
///
/// 【操作】
/// - スティック上下 / マウスホバーで選択
/// - Aボタン / クリックで決定
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

    [Header("── SE ──────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;    // 選択を変えた時
    public AudioClip confirmSE; // 選択確定時

    [Header("── 振動（選択変更） ────────────")]
    public float moveVibrationLow = 0.2f;
    public float moveVibrationHigh = 0.2f;
    public float moveVibrationDuration = 0.1f;

    [Header("── 振動（決定） ──────────────")]
    public float confirmVibrationLow = 0.6f;
    public float confirmVibrationHigh = 0.6f;
    public float confirmVibrationDuration = 0.2f;

    int selectedIndex = 0;
    float inputTimer = 0f;

    void Start()
    {
        // タイトル画面である以上、必ずポーズ状態を解除する
        // （リザルト画面や別シーンから持ち越された古いポーズ状態を防ぐ）
        GameStateManager.SetPaused(false);
        Time.timeScale = 1f;

        UpdateVisuals();
    }

    void Update()
    {
        // 設定画面が開いている間は操作を無効化
        if (GameStateManager.IsPaused) return;

        // スタッフロール表示中はBボタン / クリックで閉じる
        if (staffRollObject != null && staffRollObject.activeSelf)
        {
            if (MenuInput.CancelPressed() || MenuInput.ConfirmPressed() || MenuInput.LeftClicked())
                staffRollObject.SetActive(false);
            return;
        }

        inputTimer -= Time.unscaledDeltaTime;

        float vertical = Gamepad.current != null ? Gamepad.current.leftStick.y.ReadValue() : 0f;

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

        // マウス：ホバーで選択、クリックで決定
        int hovered = GetHoveredIndex();
        if (hovered >= 0 && MenuInput.PointerMoved())
            SetSelection(hovered);

        if (hovered >= 0 && MenuInput.LeftClicked())
        {
            SetSelection(hovered);
            ConfirmSelection();
            return;
        }

        if (MenuInput.ConfirmPressed())
        {
            ConfirmSelection();
        }
    }

    int GetHoveredIndex()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            var image = menuItems[i].targetImage;
            if (image != null && MenuInput.IsPointerOver(image.rectTransform))
                return i;
        }
        return -1;
    }

    void SetSelection(int index)
    {
        if (index == selectedIndex) return;
        selectedIndex = index;
        UpdateVisuals();
        PlaySE(moveSE);
    }

    void MoveSelection(int direction)
    {
        selectedIndex = (selectedIndex + direction + menuItems.Length) % menuItems.Length;
        UpdateVisuals();
        PlaySE(moveSE);

        if (GamepadVibrationManager.Instance != null)
            GamepadVibrationManager.Instance.PlayVibration(moveVibrationLow, moveVibrationHigh, moveVibrationDuration);
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
        PlaySE(confirmSE);

        if (GamepadVibrationManager.Instance != null)
            GamepadVibrationManager.Instance.PlayVibration(confirmVibrationLow, confirmVibrationHigh, confirmVibrationDuration);

        var item = menuItems[selectedIndex];

        switch (item.action)
        {
            case MenuAction.StartGame:
                //SceneManager.LoadScene(gameSceneName);
                SceneFadeManager.Instance.LoadScene(gameSceneName);
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

    // ─── SE再生 ───────────────────────────────────────────────────
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}