using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

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
///
/// 【配置（画像基準）】
///   Brightness(左上)  ⇄  Language(右上)
///          ↕
///        Volume(左下)
///          ↕
///     Home(下部中央)
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

    [Header("── SE ──────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;    // 選択を変えた時
    public AudioClip confirmSE; // 選択確定時

    [Header("── アニメーション ────────────────")]
    public Animator animator;
    public string closeAnimTrigger = "Close"; // 閉じる時だけ再生
    public float closeAnimDuration = 0.3f;   // 再生時間（この間はPanelを消さず待つ）

    // 配置ベースの選択項目
    enum SettingsFocus { Brightness, Language, Volume, HomeButton }
    SettingsFocus focusedItem = SettingsFocus.Brightness;
    float navInputTimer = 0f;

    // Dropdown編集モード（Languageにフォーカス中、Aボタンで入る）
    bool isEditingLanguage = false;

    // スライダー編集モード（Volume/Brightnessにフォーカス中、Aボタンで入る）
    bool isEditingSlider = false;

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

        // シーン切り替え時に開いたままにならないよう強制的に閉じる
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // シーンが切り替わったら必ず閉じた状態にする
        isOpen = false;
        isEditingLanguage = false;
        isEditingSlider = false;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        Time.timeScale = 1f;
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
        {
            Toggle();
            return; // 開閉した同じフレームでは他の入力を処理しない
        }

        if (!isOpen) return;

        // 開いている間はBボタンでも閉じられる（編集モード中はBで編集を抜けるだけ）
        if (Gamepad.current != null && Gamepad.current[closeGamepadButton].wasPressedThisFrame)
        {
            if (isEditingLanguage)
            {
                isEditingLanguage = false;
                return;
            }
            if (isEditingSlider)
            {
                isEditingSlider = false;
                return;
            }

            Toggle();
            return;
        }

        if (isEditingLanguage)
            HandleLanguageEditing();
        else if (isEditingSlider)
            HandleSliderEditing();
        else
            HandleControllerNavigation();
    }

    // ─── コントローラーでのメニュー操作（配置ベース） ───────────────
    void HandleControllerNavigation()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;

        float vertical = Gamepad.current.leftStick.y.ReadValue();
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (vertical > stickDeadZone)
            {
                GamepadVibrationManager.Instance?.PlayVibration(0.3f, 0.8f, 0.15f);
                MoveFocusUp();
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone)
            {
                GamepadVibrationManager.Instance?.PlayVibration(0.3f, 0.8f, 0.15f);
                MoveFocusDown();
                navInputTimer = navInputCooldown;
            }
            else if (horizontal > stickDeadZone)
            {
                MoveFocusRight();
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                MoveFocusLeft();
                navInputTimer = navInputCooldown;
            }
        }

        // Aボタンで決定
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            GamepadVibrationManager.Instance?.PlayVibration(0.3f, 0.8f, 0.15f);
            ConfirmFocusedItem();
        }
    }

    // ─── 配置ベースの移動 ───────────────────────────────────────────
    // Brightness(左上) ⇄ Language(右上)
    //        ↕
    //      Volume(左下)
    //        ↕
    //   Home(下部中央)
    void MoveFocusUp()
    {
        switch (focusedItem)
        {
            case SettingsFocus.Volume: SetFocus(SettingsFocus.Brightness); break;
            case SettingsFocus.HomeButton: SetFocus(SettingsFocus.Volume); break;
        }
    }

    void MoveFocusDown()
    {
        switch (focusedItem)
        {
            case SettingsFocus.Brightness: SetFocus(SettingsFocus.Volume); break;
            case SettingsFocus.Language: SetFocus(SettingsFocus.Volume); break;
            case SettingsFocus.Volume: SetFocus(SettingsFocus.HomeButton); break;
        }
    }

    void MoveFocusRight()
    {
        if (focusedItem == SettingsFocus.Brightness) SetFocus(SettingsFocus.Language);
    }

    void MoveFocusLeft()
    {
        if (focusedItem == SettingsFocus.Language) SetFocus(SettingsFocus.Brightness);
    }

    void SetFocus(SettingsFocus next)
    {
        focusedItem = next;
        UpdateHighlight();
        PlaySE(moveSE);
    }

    // ─── スライダー調整（Brightness/Volume） ────────────────────────
    void AdjustSlider(int direction)
    {
        if (focusedItem == SettingsFocus.Brightness)
            brightnessSlider.value = Mathf.Clamp01(brightnessSlider.value + direction * sliderStep);
        else if (focusedItem == SettingsFocus.Volume)
            volumeSlider.value = Mathf.Clamp01(volumeSlider.value + direction * sliderStep);
    }

    // ─── Aボタンで決定 ──────────────────────────────────────────────
    void ConfirmFocusedItem()
    {
        switch (focusedItem)
        {
            case SettingsFocus.Language:
                // Dropdown編集モードに入る
                isEditingLanguage = true;
                PlaySE(confirmSE);
                break;

            case SettingsFocus.Brightness:
            case SettingsFocus.Volume:
                // スライダー編集モードに入る
                isEditingSlider = true;
                PlaySE(confirmSE);
                break;

            case SettingsFocus.HomeButton:
                OnHomeButton();
                break;

            default:
                // Brightness/Volumeは決定操作なし
                break;
        }
    }

    // ─── Dropdown編集モード中の操作 ─────────────────────────────────
    void HandleLanguageEditing()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;

        float vertical = Gamepad.current.leftStick.y.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (vertical > stickDeadZone)
            {
                ChangeLanguageValue(-1);
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone)
            {
                ChangeLanguageValue(1);
                navInputTimer = navInputCooldown;
            }
        }

        // Aボタンで決定して編集モードを抜ける
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            isEditingLanguage = false;
            PlaySE(confirmSE);
        }
    }

    // ─── スライダー編集モード中の操作（左右で調整、Bで抜ける） ─────
    void HandleSliderEditing()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;

        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (horizontal > stickDeadZone)
            {
                AdjustSlider(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                AdjustSlider(-1);
                navInputTimer = navInputCooldown;
            }
        }
    }

    void ChangeLanguageValue(int direction)
    {
        int optionCount = languageDropdown.options.Count;
        int nextValue = (languageDropdown.value + direction + optionCount) % optionCount;
        languageDropdown.value = nextValue;
        PlaySE(moveSE);
    }

    // ─── 選択中の項目のテキスト色を変える ──────────────────────────
    void UpdateHighlight()
    {
        SetLabelColor(brightnessLabel, focusedItem == SettingsFocus.Brightness);
        SetLabelColor(languageLabel, focusedItem == SettingsFocus.Language);
        SetLabelColor(volumeLabel, focusedItem == SettingsFocus.Volume);
        SetLabelColor(homeButtonLabel, focusedItem == SettingsFocus.HomeButton);
    }

    void SetLabelColor(TMP_Text label, bool isSelected)
    {
        if (label == null) return;
        label.color = isSelected ? selectedColor : normalColor;
    }

    // ─── 開閉 ─────────────────────────────────────────────────────
    public void Toggle()
    {
        if (isOpen)
        {
            StartCoroutine(CloseWithAnimation());
        }
        else
        {
            isOpen = true;
            settingsPanel.SetActive(true);
            Time.timeScale = 0f;
            GameStateManager.SetPaused(true);

            focusedItem = SettingsFocus.Brightness;
            isEditingLanguage = false;
            isEditingSlider = false;
            navInputTimer = navInputCooldown; // 開いた直後の入力の暴発を防ぐ
            UpdateHighlight();
        }
    }

    // 閉じる時だけAnimationを再生してから非表示にする
    IEnumerator CloseWithAnimation()
    {
        isOpen = false;
        isEditingLanguage = false;
        isEditingSlider = false;

        if (animator != null)
            animator.SetTrigger(closeAnimTrigger);

        // アニメーション再生中は時間を止めないよう unscaled で待つ
        yield return new WaitForSecondsRealtime(closeAnimDuration);

        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        GameStateManager.SetPaused(false);
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
        PlaySE(confirmSE);
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    // ─── SE再生 ───────────────────────────────────────────────────
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}