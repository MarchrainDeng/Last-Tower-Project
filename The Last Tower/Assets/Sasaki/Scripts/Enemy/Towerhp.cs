using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class TowerHP : MonoBehaviour
{
    [Header("HP設定")]
    public float maxHP = 100f;

    [Header("台座")]
    public Rigidbody2D pedestalRb;

    [Header("揺れ設定")]
    public float shakeAmplitude = 0.3f;
    public float shakeDuration = 0.5f;
    public float shakeFrequency = 25f;

    [Header("UI")]
    public Slider hpSlider;
    public Text hpText;    // 未アサインなら自動生成
    public Image colorTargetImage; // 色を変えるImage（未アサインならhpSliderのfillRectを使用）

    [Header("── HPゲージ色（5段階グラデーション） ──")]
    [Tooltip("この割合(%)以上でcolor1")]
    [Range(0f, 100f)] public float threshold1 = 80f;
    public Color color1 = new Color(0x00 / 255f, 0xC8 / 255f, 0x53 / 255f); // #00C853

    [Tooltip("この割合(%)以上でcolor2")]
    [Range(0f, 100f)] public float threshold2 = 60f;
    public Color color2 = new Color(0x7E / 255f, 0xD3 / 255f, 0x21 / 255f); // #7ED321

    [Tooltip("この割合(%)以上でcolor3")]
    [Range(0f, 100f)] public float threshold3 = 40f;
    public Color color3 = new Color(0xFF / 255f, 0xEB / 255f, 0x3B / 255f); // #FFEB3B

    [Tooltip("この割合(%)以上でcolor4")]
    [Range(0f, 100f)] public float threshold4 = 20f;
    public Color color4 = new Color(0xFF / 255f, 0x98 / 255f, 0x00 / 255f); // #FF9800

    [Tooltip("threshold4未満の色")]
    public Color color5 = new Color(0xD5 / 255f, 0x00 / 255f, 0x00 / 255f); // #D50000

    public System.Action OnDead;

    float currentHP;
    bool triggered75, triggered50, triggered25;

    public bool IsDead => currentHP <= 0f;

    void Awake()
    {
        currentHP = maxHP;
    }

    void Start()
    {
        if (hpSlider == null) hpSlider = CreateHPBar();
        UpdateBar();

        // 台座を物理で動かないよう固定
        if (pedestalRb != null)
        {
            pedestalRb.bodyType = RigidbodyType2D.Kinematic;
            pedestalRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }


    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        currentHP = Mathf.Max(0f, currentHP - amount);
        UpdateBar();
        CheckThresholds();
        if (IsDead) OnDead?.Invoke();
    }

    void CheckThresholds()
    {
        float ratio = currentHP / maxHP;
        if (!triggered75 && ratio <= 0.75f) { triggered75 = true; StartCoroutine(ShakePedestal()); }
        if (!triggered50 && ratio <= 0.50f) { triggered50 = true; StartCoroutine(ShakePedestal()); }
        if (!triggered25 && ratio <= 0.25f) { triggered25 = true; StartCoroutine(ShakePedestal()); }
    }

    IEnumerator ShakePedestal()
    {
        if (pedestalRb == null) yield break;
        Vector2 origin = pedestalRb.position;
        float timer = 0f;

        // 揺れ中だけ移動を許可
        pedestalRb.constraints = RigidbodyConstraints2D.FreezeRotation;

        while (timer < shakeDuration)
        {
            float envelope = Mathf.Sin(timer / shakeDuration * Mathf.PI);
            float offset = Mathf.Sin(timer * shakeFrequency) * shakeAmplitude * envelope;
            pedestalRb.MovePosition(origin + new Vector2(offset, 0f));
            timer += Time.deltaTime;
            yield return null;
        }

        pedestalRb.MovePosition(origin);

        // 揺れ終わったら再び固定
        pedestalRb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // ─── デバッグ用：Xキーで揺れ確認 ───────────────────────────
    void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
            StartCoroutine(ShakePedestal());
    }

    void UpdateBar()
    {
        if (hpSlider != null)
            hpSlider.value = currentHP / maxHP;

        // 色を変える対象：Inspectorで指定があればそちら、なければSliderのfillRectを使用
        Image target = colorTargetImage;
        if (target == null)
            target = hpSlider?.fillRect?.GetComponent<Image>();

        if (target != null)
            target.color = GetGradientColor();

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
    }

    // ─── HP残量に応じた5段階の色を返す ─────────────────────────────
    Color GetGradientColor()
    {
        float hpPercent = (currentHP / maxHP) * 100f;

        if (hpPercent >= threshold1) return color1;
        if (hpPercent >= threshold2) return color2;
        if (hpPercent >= threshold3) return color3;
        if (hpPercent >= threshold4) return color4;
        return color5;
    }

    Slider CreateHPBar()
    {
        var canvasGo = new GameObject("TowerHPCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = new GameObject("HPBarBG");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.1f, 0.92f);
        bgRect.anchorMax = new Vector2(0.9f, 0.97f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var sliderGo = new GameObject("HPSlider");
        sliderGo.transform.SetParent(bg.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(4f, 4f);
        sliderRect.offsetMax = new Vector2(-4f, -4f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.offsetMin = faRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = color1;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        slider.fillRect = fillRect;

        var label = new GameObject("Label");
        label.transform.SetParent(bg.transform, false);
        var txt = label.AddComponent<Text>();
        txt.text = "TOWER HP";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        var lblRect = label.GetComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = lblRect.offsetMax = Vector2.zero;

        // HP数字テキスト（バーの右に表示）
        var hpTextGo = new GameObject("HPText");
        hpTextGo.transform.SetParent(bg.transform, false);
        hpText = hpTextGo.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 14;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.text = $"{maxHP} / {maxHP}";
        var hpTextRect = hpTextGo.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.offsetMin = hpTextRect.offsetMax = Vector2.zero;

        return slider;
    }
}