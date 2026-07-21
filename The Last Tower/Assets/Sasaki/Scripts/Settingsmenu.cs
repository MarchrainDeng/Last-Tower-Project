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

    [Header("── UI ─────────────────────────")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Slider brightnessSlider;
    public TMP_Dropdown languageDropdown;
    public Image brightnessOverlay;  // 全画面の黒Image（raycastTarget=false推奨）

    bool isOpen = false;

    void Start()
    {
        // 起動時は閉じた状態
            Time.timeScale = 1;      
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
    }

    void Toggle()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;
        GameStateManager.SetPaused(isOpen);
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