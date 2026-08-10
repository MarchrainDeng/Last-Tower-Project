using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

/// <summary>
/// 設定メニュー（進行画面仕様）
///
/// 【構成】
/// - サウンドセクション：Master / BGM / SFX（スライダー）
/// - ローカライズセクション：EN / JA / ZH / KO（国旗ボタン、2x2配置）
///
/// 【操作】
/// - スティック左右：セクション切り替え（サウンド⇄ローカライズ）
/// - 決定ボタン：そのセクションの項目に移動
/// - サウンド内：スティック上下でMaster→BGM→SFXを移動、左右で数値調整
/// - ローカライズ内：スティック上下左右でEN/JA/ZH/KOを移動、決定で確定
/// - Bボタン：項目選択中ならセクション選択に戻る、セクション選択中なら閉じる
///
/// 【Inspectorでアサインするもの】
/// - settingsPanel : 設定UIのルートGameObject
/// - soundLabel / localizeLabel : セクションタイトルのTMP_Text
/// - masterSlider / bgmSlider / sfxSlider : サウンドの3スライダー
/// - masterLabel / bgmLabel / sfxLabel : サウンド項目のラベル
/// - flagButtons : EN/JA/ZH/KO の4ボタン（Imageで枠のハイライトを想定）
/// - cursorObject : 左端に表示するカーソル
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("── 操作 ──────────────────────")]
    public Key toggleKey = Key.Escape;
    public GamepadButton toggleGamepadButton = GamepadButton.Start;
    public GamepadButton closeGamepadButton = GamepadButton.East; // B

    [Header("── UI：全体 ────────────────────")]
    public GameObject settingsPanel;
    public RectTransform cursorObject; // フォーカス中セクションの左に表示

    [Header("── UI：セクションタイトル ─────────")]
    public TMP_Text soundSectionLabel;
    public TMP_Text localizeSectionLabel;
    public Color sectionActiveColor = Color.white;
    public Color sectionInactiveColor = new Color(1f, 1f, 1f, 0.4f); // 少し暗く

    [Header("── UI：サウンド項目 ──────────────")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public TMP_Text masterLabel;
    public TMP_Text bgmLabel;
    public TMP_Text sfxLabel;

    [Header("── UI：ローカライズ項目（国旗ボタン） ──")]
    public Image[] flagButtons = new Image[4]; // 0:EN 1:JA 2:ZH 3:KO（2x2配置）

    [Header("── ハイライト色 ────────────────")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("── コントローラー操作 ────────────")]
    public float stickDeadZone = 0.5f;
    public float sliderStep = 0.05f;
    public float navInputCooldown = 0.2f;

    [Header("── SE ──────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip confirmSE;

    [Header("── アニメーション ────────────────")]
    public Animator animator;
    public string closeAnimTrigger = "Close";
    public float closeAnimDuration = 0.3f;

    // ─── セクション ───────────────────────────────────────────────
    enum Section { Sound, Localize }
    Section currentSection = Section.Sound;

    // ─── サウンド内の項目 ───────────────────────────────────────────
    enum SoundItem { Master, BGM, SFX }
    SoundItem soundFocus = SoundItem.Master;

    // ─── ローカライズ内の項目（2x2: 0=EN,1=JA / 2=ZH,3=KO） ─────────
    int localizeFocus = 1; // デフォルトJA

    // モード：セクション選択中か、セクション内の項目を操作中か
    bool isInsideSection = false;

    float navInputTimer = 0f;
    bool isOpen = false;

    static readonly string[] LangCodes = { "en", "ja", "zh", "ko" };

    // ─── シングルトン化 ─────────────────────────────────────────────
    public static SettingsMenu Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isOpen = false;
        isInsideSection = false;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Start()
    {
        settingsPanel.SetActive(false);

        // 保存済み値を反映
        if (AudioManager.Instance != null)
        {
            masterSlider.value = AudioManager.Instance.GetMasterVolume();
            bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
        }

        string saved = PlayerPrefs.GetString("Language", "ja");
        for (int i = 0; i < LangCodes.Length; i++)
            if (LangCodes[i] == saved) { localizeFocus = i; break; }

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    void Update()
    {
        bool keyboardPressed = Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current[toggleGamepadButton].wasPressedThisFrame;

        if (keyboardPressed || gamepadPressed)
        {
            Toggle();
            return;
        }

        if (!isOpen) return;

        // Bボタン：項目操作中ならセクション選択に戻る、それ以外なら閉じる
        if (Gamepad.current != null && Gamepad.current[closeGamepadButton].wasPressedThisFrame)
        {
            if (isInsideSection)
            {
                isInsideSection = false;
                UpdateHighlight();
            }
            else
            {
                Toggle();
            }
            return;
        }

        if (isInsideSection)
            HandleSectionInput();
        else
            HandleSectionSelect();
    }

    // ─── セクション選択モード（左右でサウンド⇄ローカライズ） ───────
    void HandleSectionSelect()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (horizontal > stickDeadZone)
            {
                SetSection(Section.Localize);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                SetSection(Section.Sound);
                navInputTimer = navInputCooldown;
            }
        }

        // 決定：そのセクションの項目に入る
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            isInsideSection = true;
            PlaySE(confirmSE);
            UpdateHighlight();
        }
    }

    void SetSection(Section next)
    {
        if (currentSection == next) return;
        currentSection = next;
        PlaySE(moveSE);
        UpdateHighlight();
    }

    // ─── セクション内操作 ───────────────────────────────────────────
    void HandleSectionInput()
    {
        if (currentSection == Section.Sound)
            HandleSoundInput();
        else
            HandleLocalizeInput();
    }

    // ─── サウンド項目操作（上下で項目移動、左右で数値調整） ─────────
    void HandleSoundInput()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;
        float vertical = Gamepad.current.leftStick.y.ReadValue();
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            if (vertical > stickDeadZone)
            {
                MoveSoundFocus(-1);
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone)
            {
                MoveSoundFocus(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal > stickDeadZone)
            {
                AdjustSoundSlider(1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone)
            {
                AdjustSoundSlider(-1);
                navInputTimer = navInputCooldown;
            }
        }
    }

    void MoveSoundFocus(int direction)
    {
        int count = System.Enum.GetValues(typeof(SoundItem)).Length;
        int next = ((int)soundFocus + direction + count) % count;
        soundFocus = (SoundItem)next;
        PlaySE(moveSE);
        UpdateHighlight();
    }

    void AdjustSoundSlider(int direction)
    {
        switch (soundFocus)
        {
            case SoundItem.Master: masterSlider.value = Mathf.Clamp01(masterSlider.value + direction * sliderStep); break;
            case SoundItem.BGM: bgmSlider.value = Mathf.Clamp01(bgmSlider.value + direction * sliderStep); break;
            case SoundItem.SFX: sfxSlider.value = Mathf.Clamp01(sfxSlider.value + direction * sliderStep); break;
        }
    }

    // ─── ローカライズ項目操作（2x2を上下左右で移動、決定で確定） ────
    void HandleLocalizeInput()
    {
        if (Gamepad.current == null) return;

        navInputTimer -= Time.unscaledDeltaTime;
        float vertical = Gamepad.current.leftStick.y.ReadValue();
        float horizontal = Gamepad.current.leftStick.x.ReadValue();

        if (navInputTimer <= 0f)
        {
            // 2x2レイアウト: 0=EN(左上) 1=JA(右上) 2=ZH(左下) 3=KO(右下)
            if (vertical > stickDeadZone && localizeFocus >= 2)
            {
                MoveLocalizeFocus(localizeFocus - 2);
            }
            else if (vertical < -stickDeadZone && localizeFocus < 2)
            {
                MoveLocalizeFocus(localizeFocus + 2);
            }
            else if (horizontal > stickDeadZone && (localizeFocus % 2 == 0))
            {
                MoveLocalizeFocus(localizeFocus + 1);
            }
            else if (horizontal < -stickDeadZone && (localizeFocus % 2 == 1))
            {
                MoveLocalizeFocus(localizeFocus - 1);
            }
            else
            {
                return;
            }
            navInputTimer = navInputCooldown;
        }

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ConfirmLanguage();
        }
    }

    void MoveLocalizeFocus(int next)
    {
        localizeFocus = Mathf.Clamp(next, 0, 3);
        PlaySE(moveSE);
        UpdateHighlight();
    }

    void ConfirmLanguage()
    {
        PlayerPrefs.SetString("Language", LangCodes[localizeFocus]);
        PlayerPrefs.Save();
        PlaySE(confirmSE);
        UpdateHighlight();
    }

    // ─── ハイライト更新 ─────────────────────────────────────────────
    void UpdateHighlight()
    {
        // セクションタイトルの明るさ
        if (soundSectionLabel != null)
            soundSectionLabel.color = (currentSection == Section.Sound) ? sectionActiveColor : sectionInactiveColor;
        if (localizeSectionLabel != null)
            localizeSectionLabel.color = (currentSection == Section.Localize) ? sectionActiveColor : sectionInactiveColor;

        // サウンド項目のハイライト（項目操作中のみ強調）
        bool soundActive = isInsideSection && currentSection == Section.Sound;
        SetLabelColor(masterLabel, soundActive && soundFocus == SoundItem.Master);
        SetLabelColor(bgmLabel, soundActive && soundFocus == SoundItem.BGM);
        SetLabelColor(sfxLabel, soundActive && soundFocus == SoundItem.SFX);

        // 国旗ボタンのハイライト
        bool localizeActive = isInsideSection && currentSection == Section.Localize;
        for (int i = 0; i < flagButtons.Length; i++)
        {
            if (flagButtons[i] == null) continue;
            flagButtons[i].color = (localizeActive && localizeFocus == i) ? selectedColor : normalColor;
        }

        // カーソル位置：現在のセクションの左に表示
        if (cursorObject != null)
        {
            var target = currentSection == Section.Sound ? soundSectionLabel : localizeSectionLabel;
            if (target != null)
            {
                var pos = cursorObject.position;
                pos.y = target.transform.position.y;
                cursorObject.position = pos;
            }
        }
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

            // 仕様：最初はサウンドセクションが選択されている
            currentSection = Section.Sound;
            isInsideSection = false;
            navInputTimer = navInputCooldown;
            UpdateHighlight();
        }
    }

    IEnumerator CloseWithAnimation()
    {
        isOpen = false;
        isInsideSection = false;

        if (animator != null)
            animator.SetTrigger(closeAnimTrigger);

        yield return new WaitForSecondsRealtime(closeAnimDuration);

        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        GameStateManager.SetPaused(false);
    }

    // ─── コールバック ─────────────────────────────────────────────
    public void OnMasterChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    public void OnBGMChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
    }

    // ─── SE再生 ───────────────────────────────────────────────────
    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}