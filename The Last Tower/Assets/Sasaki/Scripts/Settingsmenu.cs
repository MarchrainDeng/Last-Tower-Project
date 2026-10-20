using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// 設定メニュー（進行画面仕様）
///
/// 【構成】
/// - サウンドセクション：Master / BGM / SFX（スライダー）
/// - ローカライズセクション：EN / JA / ZH / KO（国旗ボタン、2x2配置）
///
/// 【操作】
/// - スティック左右：セクション切り替え（サウンド⇄ローカライズ）
/// - Aボタン：そのセクションの項目に移動
/// - サウンド内：スティック上下でMaster→BGM→SFXを移動、左右で数値調整
/// - ローカライズ内：スティック上下左右でEN/JA/ZH/KOを移動、Aで確定
/// - Bボタン / 右クリック：項目選択中ならセクション選択に戻る、セクション選択中なら閉じる
/// - マウス：ホバーで選択、クリックで決定、スライダーはドラッグで調整
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

    [Header("── UI：全体 ────────────────────")]
    public GameObject settingsPanel;
    public RectTransform cursorObject; // フォーカス中の対象の左端に表示
    public float cursorOffsetX = 40f;  // 対象の左端からさらにどれだけ離すか

    [Header("── UI：セクションタイトル ─────────")]
    public TMP_Text soundSectionLabel;
    public TMP_Text localizeSectionLabel;
    public TMP_Text homeSectionLabel;

    [Header("── ホーム ──────────────────────")]
    public string homeSceneName = "MainMenu";
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
    public TMP_Text currentLanguageLabel;      // 現在選択中の言語を表示するUIテキスト

    [Header("── デバッグ表示（Inspectorで確認用） ──")]
    [SerializeField] string currentLanguageDebug; // 現在の言語（読み取り専用表示）

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
    public string openAnimTrigger = "Open";
    public float openAnimDuration = 0.3f;
    public string closeAnimTrigger = "Close";
    public float closeAnimDuration = 0.3f;

    // ─── セクション ───────────────────────────────────────────────
    enum Section { Sound, Localize, Home }
    Section currentSection = Section.Sound;

    // ─── サウンド内の項目 ───────────────────────────────────────────
    enum SoundItem { Master, BGM, SFX }
    SoundItem soundFocus = SoundItem.Master;

    // ─── ローカライズ内の項目（2x2: 0=EN,1=JA / 2=ZH,3=KO） ─────────
    int localizeFocus = 0; // デフォルトEN

    // モード：セクション選択中か、セクション内の項目を操作中か
    bool isInsideSection = false;

    // サウンド項目内：決定して数値編集モードに入っているか
    bool isEditingSound = false;

    float navInputTimer = 0f;
    bool isOpen = false;
    int openedFrame = -1;          // 開いたフレーム（開くのに使った決定入力をメニュー内で拾わないため）
    Slider draggingSlider = null;  // マウスでドラッグ中のスライダー

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
        isEditingSound = false;
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

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        // 現在の言語表示は保存済み値を反映（カーソル位置は動かさない）
        string savedLang = PlayerPrefs.GetString("Language", "en");
        int savedIndex = System.Array.IndexOf(LangCodes, savedLang);
        int displayIndex = savedIndex >= 0 ? savedIndex : localizeFocus;
        UpdateCurrentLanguageDisplay(displayIndex);
        ApplyLocale(savedLang);
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
        if (Time.frameCount == openedFrame) return;

        // Bボタン / 右クリック：編集中なら編集モードを抜ける、項目操作中ならセクション選択に戻る、それ以外なら閉じる
        if (MenuInput.CancelPressed())
        {
            draggingSlider = null;
            if (isEditingSound)
            {
                isEditingSound = false;
                UpdateHighlight();
            }
            else if (isInsideSection)
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

        if (HandleMouseInput()) return;

        if (isInsideSection)
            HandleSectionInput();
        else
            HandleSectionSelect();
    }

    float StickX() => Gamepad.current != null ? Gamepad.current.leftStick.x.ReadValue() : 0f;
    float StickY() => Gamepad.current != null ? Gamepad.current.leftStick.y.ReadValue() : 0f;

    // ─── マウス操作 ─────────────────────────────────────────────────
    // ホバーで選択、クリックで決定。マウスで操作したフレームは true を返す
    bool HandleMouseInput()
    {
        if (Mouse.current == null) return false;

        // スライダーをドラッグ中は離すまで値を追従させる
        if (draggingSlider != null)
        {
            if (MenuInput.LeftHeld())
            {
                SetSliderFromPointer(draggingSlider);
                return true;
            }
            draggingSlider = null;
        }

        bool moved = MenuInput.PointerMoved();
        bool clicked = MenuInput.LeftClicked();
        if (!moved && !clicked) return false;

        // セクションタイトル：ホバーで選択、クリックで決定
        Section? hoveredSection =
            IsOver(soundSectionLabel) ? Section.Sound :
            IsOver(localizeSectionLabel) ? Section.Localize :
            IsOver(homeSectionLabel) ? Section.Home :
            (Section?)null;
        if (hoveredSection.HasValue)
        {
            if (clicked)
            {
                isInsideSection = false;
                isEditingSound = false;
                SetSection(hoveredSection.Value);
                ConfirmSection();
            }
            else if (!isInsideSection)
            {
                SetSection(hoveredSection.Value);
            }
            return true;
        }

        // サウンド：ホバーで選択、クリックで編集、スライダーはドラッグで調整
        SoundItem[] soundItems = { SoundItem.Master, SoundItem.BGM, SoundItem.SFX };
        Slider[] sliders = { masterSlider, bgmSlider, sfxSlider };
        TMP_Text[] labels = { masterLabel, bgmLabel, sfxLabel };
        for (int i = 0; i < soundItems.Length; i++)
        {
            bool overSlider = sliders[i] != null && MenuInput.IsPointerOver(sliders[i].GetComponent<RectTransform>());
            if (!overSlider && !IsOver(labels[i])) continue;

            FocusSoundItem(soundItems[i]);
            if (clicked)
            {
                isEditingSound = true;
                if (overSlider)
                {
                    draggingSlider = sliders[i];
                    SetSliderFromPointer(draggingSlider);
                }
                UpdateHighlight();
            }
            return true;
        }

        // ローカライズ：国旗ホバーで選択、クリックで確定
        for (int i = 0; i < flagButtons.Length; i++)
        {
            if (flagButtons[i] == null || !MenuInput.IsPointerOver(flagButtons[i].rectTransform)) continue;

            FocusFlag(i);
            if (clicked)
                ConfirmLanguage();
            return true;
        }

        return false;
    }

    static bool IsOver(TMP_Text label)
    {
        return label != null && MenuInput.IsPointerOver(label.rectTransform);
    }

    void FocusSoundItem(SoundItem item)
    {
        if (currentSection == Section.Sound && isInsideSection && soundFocus == item) return;

        if (soundFocus != item) isEditingSound = false;
        currentSection = Section.Sound;
        isInsideSection = true;
        soundFocus = item;
        PlaySE(moveSE);
        UpdateHighlight();
    }

    void FocusFlag(int index)
    {
        if (currentSection == Section.Localize && isInsideSection && localizeFocus == index) return;

        currentSection = Section.Localize;
        isInsideSection = true;
        isEditingSound = false;
        localizeFocus = index;
        PlaySE(moveSE);
        UpdateHighlight();
    }

    void SetSliderFromPointer(Slider slider)
    {
        slider.normalizedValue = MenuInput.PointerNormalizedX(slider.GetComponent<RectTransform>());
    }

    // ─── セクション選択モード（左右でサウンド⇄ローカライズ） ───────
    void HandleSectionSelect()
    {
        navInputTimer -= Time.unscaledDeltaTime;

        float stickX = StickX();
        bool dpadRight = Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame;
        bool dpadLeft = Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;

        if (navInputTimer <= 0f)
        {
            bool moveRight = stickX > stickDeadZone || dpadRight;
            bool moveLeft = stickX < -stickDeadZone || dpadLeft;

            if (moveRight)
            {
                CycleSection(1);
                navInputTimer = navInputCooldown;
            }
            else if (moveLeft)
            {
                CycleSection(-1);
                navInputTimer = navInputCooldown;
            }
        }

        // 決定
        if (MenuInput.ConfirmPressed())
        {
            ConfirmSection();
        }
    }

    void ConfirmSection()
    {
        if (currentSection == Section.Home)
        {
            // Homeは即座に実行（下の項目へは移動しない）
            PlaySE(confirmSE);
            GoToTitle();
        }
        else
        {
            // Sound/Localizeは下の項目にフォーカス移動
            isInsideSection = true;
            PlaySE(confirmSE);
            UpdateHighlight();
        }
    }

    void CycleSection(int direction)
    {
        int count = System.Enum.GetValues(typeof(Section)).Length;
        int next = ((int)currentSection + direction + count) % count;
        SetSection((Section)next);
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
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

    // ─── サウンド項目操作 ───────────────────────────────────────────
    // 未編集時：左右でMaster⇄BGM⇄SFXのフォーカス移動、決定で編集モードへ
    // 編集時　：左右で数値調整、Bで編集モードを抜ける
    void HandleSoundInput()
    {
        if (isEditingSound)
        {
            HandleSoundEditing();
            return;
        }

        navInputTimer -= Time.unscaledDeltaTime;
        float vertical = StickY();
        float horizontal = StickX();

        if (navInputTimer <= 0f)
        {
            // Master(上段) ⇄ BGM/SFX(下段) は上下移動
            if (vertical > stickDeadZone && soundFocus != SoundItem.Master)
            {
                SetSoundFocus(SoundItem.Master);
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone && soundFocus == SoundItem.Master)
            {
                SetSoundFocus(SoundItem.BGM); // Masterから下がる時はBGMに入る
                navInputTimer = navInputCooldown;
            }
            // BGM ⇄ SFX は左右移動（Master中は無効）
            else if (horizontal > stickDeadZone && soundFocus == SoundItem.BGM)
            {
                SetSoundFocus(SoundItem.SFX);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone && soundFocus == SoundItem.SFX)
            {
                SetSoundFocus(SoundItem.BGM);
                navInputTimer = navInputCooldown;
            }
        }

        // 決定：編集モードに入る
        if (MenuInput.ConfirmPressed())
        {
            isEditingSound = true;
            PlaySE(confirmSE);
            UpdateHighlight();
        }
    }

    // ─── サウンド数値編集モード（左右で数値調整のみ） ────────────────
    void HandleSoundEditing()
    {
        navInputTimer -= Time.unscaledDeltaTime;
        float horizontal = StickX();

        if (navInputTimer <= 0f)
        {
            if (horizontal > stickDeadZone)
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

    void SetSoundFocus(SoundItem next)
    {
        if (soundFocus == next) return;
        soundFocus = next;
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
        navInputTimer -= Time.unscaledDeltaTime;
        float vertical = StickY();
        float horizontal = StickX();

        if (navInputTimer <= 0f)
        {
            // 2x2レイアウト: 0=EN(左上) 1=JA(右上) 2=ZH(左下) 3=KO(右下)
            if (vertical > stickDeadZone && localizeFocus >= 2)
            {
                MoveLocalizeFocus(localizeFocus - 2);
                navInputTimer = navInputCooldown;
            }
            else if (vertical < -stickDeadZone && localizeFocus < 2)
            {
                MoveLocalizeFocus(localizeFocus + 2);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal > stickDeadZone && (localizeFocus % 2 == 0))
            {
                MoveLocalizeFocus(localizeFocus + 1);
                navInputTimer = navInputCooldown;
            }
            else if (horizontal < -stickDeadZone && (localizeFocus % 2 == 1))
            {
                MoveLocalizeFocus(localizeFocus - 1);
                navInputTimer = navInputCooldown;
            }
        }

        if (MenuInput.ConfirmPressed())
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
        UpdateCurrentLanguageDisplay(localizeFocus);
        ApplyLocale(LangCodes[localizeFocus]);
    }

    // ─── Unity Localizationの言語を切り替える ────────────────────────
    // LangCodes と Unity側のLocale Codeが違う場合はここで変換する
    // （例：中国語は "zh" ではなく "zh-Hans" になっていることが多い）
    void ApplyLocale(string code)
    {
        // Unity LocalizationはLocaleの読み込みが非同期のため、
        // 初期化完了前に AvailableLocales へアクセスすると
        // 「Locales PreloadOperation has not been initialized」エラーになり、
        // 言語が適用されないまま終わってしまう。
        // そのため初期化の完了を待ってから適用する。
        var initOperation = LocalizationSettings.InitializationOperation;

        if (initOperation.IsDone)
        {
            ApplyLocaleImmediate(code);
        }
        else
        {
            // コルーチンではなくコールバックを使う
            // （GameObjectが非アクティブでも確実に実行されるようにするため）
            initOperation.Completed += _ => ApplyLocaleImmediate(code);
        }
    }

    /// <summary>
    /// Localizationの初期化完了後に実際のLocale切り替えを行う
    /// </summary>
    void ApplyLocaleImmediate(string code)
    {
        string localeCode = code switch
        {
            "en" => "en",
            "ja" => "ja",
            "zh" => "zh",
            "ko" => "ko",
            _ => code
        };

        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
        else
        {
            Debug.LogWarning($"[SettingsMenu] Locale '{localeCode}' が見つかりません。Localization Settingsの Available Locales を確認してください。");
        }
    }

    // ─── 現在選択中の言語をUI・Inspectorに表示 ──────────────────────
    void UpdateCurrentLanguageDisplay(int index)
    {
        string code = LangCodes[index];
        string displayName = code switch
        {
            "en" => "English",
            "ja" => "Japanese",
            "zh" => "Chinese",
            "ko" => "Korean",
            _ => code
        };

        if (currentLanguageLabel != null)
            currentLanguageLabel.text = displayName;

        currentLanguageDebug = $"{code} ({displayName})";
    }

    // ─── ハイライト更新 ─────────────────────────────────────────────
    void UpdateHighlight()
    {
        // セクションタイトルの明るさ
        if (soundSectionLabel != null)
            soundSectionLabel.color = (currentSection == Section.Sound) ? sectionActiveColor : sectionInactiveColor;
        if (localizeSectionLabel != null)
            localizeSectionLabel.color = (currentSection == Section.Localize) ? sectionActiveColor : sectionInactiveColor;
        if (homeSectionLabel != null)
            homeSectionLabel.color = (currentSection == Section.Home) ? sectionActiveColor : sectionInactiveColor;

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

        // カーソル位置：現在選択中の対象（セクション or 項目）の左端に表示
        if (cursorObject != null)
        {
            RectTransform target = GetCursorTarget();
            if (target != null)
            {
                float leftEdgeX = GetLeftEdgeWorldX(target);
                var pos = cursorObject.position;
                pos.x = leftEdgeX - cursorOffsetX;
                pos.y = target.position.y;
                cursorObject.position = pos;
            }
        }
    }

    // ─── 対象RectTransformの左端のワールドX座標を取得 ────────────────
    float GetLeftEdgeWorldX(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners[0] = 左下, corners[1] = 左上, corners[2] = 右上, corners[3] = 右下
        return corners[0].x;
    }

    // ─── カーソルの追従先を決定 ─────────────────────────────────────
    RectTransform GetCursorTarget()
    {
        if (!isInsideSection)
        {
            // セクション選択中：セクションタイトルの位置
            TMP_Text label = currentSection switch
            {
                Section.Sound => soundSectionLabel,
                Section.Localize => localizeSectionLabel,
                Section.Home => homeSectionLabel,
                _ => null
            };
            return label != null ? label.rectTransform : null;
        }

        // 項目選択中：Sound/Localizeそれぞれの現在フォーカス項目
        if (currentSection == Section.Sound)
        {
            // スライダーを編集中はスライダー自体の左にカーソルを合わせる
            if (isEditingSound)
            {
                RectTransform sliderTarget = soundFocus switch
                {
                    SoundItem.Master => masterSlider != null ? masterSlider.GetComponent<RectTransform>() : null,
                    SoundItem.BGM => bgmSlider != null ? bgmSlider.GetComponent<RectTransform>() : null,
                    SoundItem.SFX => sfxSlider != null ? sfxSlider.GetComponent<RectTransform>() : null,
                    _ => null
                };
                if (sliderTarget != null)
                    return sliderTarget;
            }

            TMP_Text label = soundFocus switch
            {
                SoundItem.Master => masterLabel,
                SoundItem.BGM => bgmLabel,
                SoundItem.SFX => sfxLabel,
                _ => null
            };
            return label != null ? label.rectTransform : null;
        }
        else if (currentSection == Section.Localize)
        {
            if (localizeFocus >= 0 && localizeFocus < flagButtons.Length && flagButtons[localizeFocus] != null)
                return flagButtons[localizeFocus].rectTransform;
        }

        return null;
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
            StartCoroutine(OpenWithAnimation());
        }
    }

    IEnumerator OpenWithAnimation()
    {
        isOpen = true;
        openedFrame = Time.frameCount;
        draggingSlider = null;
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
        GameStateManager.SetPaused(true);

        // 仕様：最初はサウンドセクションが選択されている
        currentSection = Section.Sound;
        isInsideSection = false;
        isEditingSound = false;
        navInputTimer = navInputCooldown;

        // アニメーションが落ち着くまでカーソルを隠す
        if (cursorObject != null)
            cursorObject.gameObject.SetActive(false);

        if (animator != null)
        {
            animator.SetTrigger(openAnimTrigger);
            yield return new WaitForSecondsRealtime(openAnimDuration);
        }
        else
        {
            // Animatorが無い場合もレイアウト確定のため1フレーム待つ
            yield return null;
        }

        UpdateHighlight();

        if (cursorObject != null)
            cursorObject.gameObject.SetActive(true);
    }

    IEnumerator CloseWithAnimation()
    {
        isOpen = false;
        isInsideSection = false;
        isEditingSound = false;

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