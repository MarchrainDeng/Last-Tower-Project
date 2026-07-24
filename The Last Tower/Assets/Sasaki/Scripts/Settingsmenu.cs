using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 設定メニュー
///
/// 【Inspectorでアサインするもの】
/// - settingsPanel   : 設定UIのルートGameObject
/// - volumeSlider    : 全体音量スライダー
/// - brightnessSlider: 明るさスライダー
/// - languageDropdown: 言語ドロップダウン
/// - brightnessOverlay: 明るさ調整用の全画面黒Image
///
/// 【ボタンのOnClickに登録するもの】
/// - OnHomeButton()
///
/// 【Dropdownのオプション順】
///   0: Japanese, 1: Chinese, 2: Korean, 3: English
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("── シーン ──────────────────────")]
    public string homeSceneName = "MainMenu";

    [Header("── 操作 ──────────────────────")]
    public Key toggleKey = Key.Escape;              // 開閉に使うキーボードキー
    public GamepadButton toggleGamepadButton = GamepadButton.Start; // 開閉に使うコントローラーボタン
    public GamepadButton closeGamepadButton = GamepadButton.East;  // 開いている間、閉じる専用ボタン（Bボタン）

    [Header("── UI ─────────────────────────")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown languageDropdown;
    public Image brightnessOverlay;  // 全画面の黒Image（raycastTarget=false推奨）

    [Header("── 選択ハイライト用ラベル ─────────")]
    public TMP_Text volumeLabel;
    public TMP_Text brightnessLabel;
    public TMP_Text languageLabel;
    public TMP_Text homeButtonLabel;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("── コントローラー操作 ────────────")]
    public float stickDeadZone = 0.5f;
    public float sliderStep = 0.05f; // スライダーを一度に動かす量
    public float navInputCooldown = 0.2f;  // 連続入力を防ぐ間隔

    // 選択可能な項目（上から順）
    enum SettingsFocus { Volume, Brightness, Language, HomeButton }
    SettingsFocus focusedItem = SettingsFocus.Volume;
    float navInputTimer = 0f;

    bool isOpen = false;

    // ─── シングルトン化：シーンをまたいで同じ設定UIを使い回す ────────
    public static SettingsMenu Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoadはルートオブジェクトにしか使えないため、
        // 自分自身ではなく一番上の親（Canvas）を対象にする
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        // 起動時は閉じた状態
        settingsPanel.SetActive(false);

        // 保存済み値を反映
        volumeSlider.value = AudioListener.volume;
        brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 1f);
        OnBrightnessChanged(brightnessSlider.value);

        string saved = PlayerPrefs.GetString("Language", "ja");
        string[] codes = { "ja", "zh", "ko", "en" };
        for (int i = 0; i < codes.Length; i++)
            if (codes[i] == saved) { languageDropdown.value = i; break; }

        // リスナー登録
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    void Update()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current[toggleGamepadButton].wasPressedThisFrame;

        if (keyboardPressed || gamepadPressed)
            Toggle();

        if (!isOpen) return;

        // 開いている間はBボタンでも閉じられる
        if (Gamepad.current != null && Gamepad.current[closeGamepadButton].wasPressedThisFrame)
        {
            Toggle();
            return;
        }

        HandleControllerNavigation();
    }

    // ─── コントローラーでのメニュー操作 ────────────────────────────
    void HandleControllerNavigation()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;

        float vertical = Gamepad.current.leftStick.y.ReadValue();
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        // 上下で選択項目を移動
        if (navInputTimer <= 0f)
        {
            if (vertical > stickDeadZone)
            {
                MoveFocus(-1);
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone)
            {
                MoveFocus(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal > stickDeadZone)
            {
                AdjustFocusedItem(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                AdjustFocusedItem(-1);
                navInputTimer = navInputCooldown;
            }
        }

        // Aボタンでホームボタンを押す
        if (focusedItem == SettingsFocus.HomeButton && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            OnHomeButton();
        }
    }

    void MoveFocus(int direction)
    {
        int itemCount = System.Enum.GetValues(typeof(SettingsFocus)).Length;
        int next = ((int)focusedItem + direction + itemCount) % itemCount;
        focusedItem = (SettingsFocus)next;
        UpdateHighlight();
    }

    // ─── 選択中の項目のテキスト色を変える ──────────────────────────
    void UpdateHighlight()
    {
        SetLabelColor(volumeLabel, focusedItem == SettingsFocus.Volume);
        SetLabelColor(brightnessLabel, focusedItem == SettingsFocus.Brightness);
        SetLabelColor(languageLabel, focusedItem == SettingsFocus.Language);
        SetLabelColor(homeButtonLabel, focusedItem == SettingsFocus.HomeButton);
    }

    void SetLabelColor(TMP_Text label, bool isSelected)
    {
        if (label == null) return;
        label.color = isSelected ? selectedColor : normalColor;
    }

    void AdjustFocusedItem(int direction)
    {
        switch (focusedItem)
        {
            case SettingsFocus.Volume:
                volumeSlider.value = Mathf.Clamp01(volumeSlider.value + direction * sliderStep);
                break;

            case SettingsFocus.Brightness:
                brightnessSlider.value = Mathf.Clamp01(brightnessSlider.value + direction * sliderStep);
                break;

            case SettingsFocus.Language:
                int optionCount = languageDropdown.options.Count;
                int nextValue = (languageDropdown.value + direction + optionCount) % optionCount;
                languageDropdown.value = nextValue;
                break;

            case SettingsFocus.HomeButton:
                // Aボタンで実行（AdjustFocusedItemでは何もしない）
                break;
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;
        GameStateManager.SetPaused(isOpen);

        if (isOpen)
        {
            focusedItem = SettingsFocus.Volume;
            UpdateHighlight();
        }
    }

    // ─── コールバック ─────────────────────────────────────────────
    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    public void OnBrightnessChanged(float value)
    {
        PlayerPrefs.SetFloat("Brightness", value);
        if (brightnessOverlay == null) return;
        var c = brightnessOverlay.color;
        brightnessOverlay.color = new Color(c.r, c.g, c.b, 1f - value);
    }

    public void OnLanguageChanged(int index)
    {
        string[] langs = { "ja", "zh", "ko", "en" };
        PlayerPrefs.SetString("Language", langs[index]);
        PlayerPrefs.Save();
    }

    public void OnHomeButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }
}