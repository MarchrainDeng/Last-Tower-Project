using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 釣りのお助けロボット
/// - サーチ：周りの魚の影を光らせて種類と大きさを教える
/// - アタリ：本アタリの瞬間に「!!」で知らせる（つついているだけの時は「…まだ」）
/// - アシスト：ファイト中に一度だけ糸を一緒に引っぱってくれる
/// 判断は MyTownFishing が行い、ここでは見た目（浮遊・目の色・吹き出し・スキャンの輪）だけを扱う
/// クリックされたら MyTownFishing.RobotAction を呼ぶ
/// </summary>
public class FishingRobot : MonoBehaviour, IPointerClickHandler
{
    public MyTownFishing owner;
    public RectTransform body;
    public Image eye;
    public RectTransform bubble;
    public TMP_Text bubbleText;
    public Image scanRing;

    public Color eyeNormal = new Color(0.4f, 0.9f, 1f);
    public Color eyeAlert = new Color(1f, 0.3f, 0.25f);
    public Color eyeAssist = new Color(1f, 0.85f, 0.3f);

    public float scanRadius = 1500f;
    public float scanDuration = 1.2f;

    RectTransform rect;
    Vector2 basePosition;
    float bubbleTimer, alertTimer, scanTimer = -1f, jump;
    bool assisting;

    public Vector2 Position => basePosition;
    public RectTransform Rect => rect;
    // スキャンの輪が広がる速さ（魚が光るタイミングを合わせるのに使う）
    public float ScanSpeed => scanRadius / scanDuration;

    void Awake()
    {
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;
        if (bubble != null) bubble.gameObject.SetActive(false);
        if (scanRing != null) scanRing.gameObject.SetActive(false);
    }

    public void Say(string text, float duration = 1.6f)
    {
        if (bubble == null) return;
        bubbleText.text = text;
        bubbleTimer = duration;
        bubble.gameObject.SetActive(true);
        bubble.localScale = Vector3.one * 1.25f;
    }

    public void Alert(string text)
    {
        alertTimer = 0.8f;
        jump = 1f;
        Say(text, 0.9f);
    }

    public void Cheer(string text)
    {
        jump = 1f;
        Say(text, 2.2f);
    }

    public void SetAssist(bool on)
    {
        assisting = on;
    }

    public void PlayScan()
    {
        scanTimer = 0f;
        jump = 0.5f;
        if (scanRing != null)
        {
            scanRing.gameObject.SetActive(true);
            scanRing.rectTransform.anchoredPosition = basePosition + new Vector2(0f, 60f);
        }
    }

    public void HideBubble()
    {
        bubbleTimer = 0f;
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        // ふわふわ浮く。知らせる時は跳ねる、アシスト中は踏ん張って震える
        jump = Mathf.MoveTowards(jump, 0f, dt * 2.5f);
        float hop = Mathf.Sin(jump * Mathf.PI) * 40f;
        Vector2 offset = new Vector2(0f, Mathf.Sin(t * 3f) * 6f + hop);
        if (assisting) offset += new Vector2(Mathf.Sin(t * 60f) * 3f, -6f);
        rect.anchoredPosition = basePosition + offset;
        if (body != null) body.localRotation = Quaternion.Euler(0f, 0f, assisting ? 12f : Mathf.Sin(t * 1.7f) * 4f);

        if (eye != null)
        {
            if (alertTimer > 0f) alertTimer -= dt;
            Color c = assisting ? eyeAssist : alertTimer > 0f ? eyeAlert : eyeNormal;
            // まばたき
            float blink = Mathf.Repeat(t, 3.3f) < 0.12f ? 0.2f : 1f;
            eye.color = c;
            eye.rectTransform.localScale = new Vector3(1f, blink * (alertTimer > 0f ? 1.4f : 1f), 1f);
        }

        if (bubble != null && bubble.gameObject.activeSelf)
        {
            bubbleTimer -= dt;
            bubble.localScale = Vector3.Lerp(bubble.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 18f));
            if (bubbleTimer <= 0f) bubble.gameObject.SetActive(false);
        }

        if (scanRing != null && scanTimer >= 0f)
        {
            scanTimer += dt;
            float p = scanTimer / scanDuration;
            float size = scanRadius * 2f * p;
            scanRing.rectTransform.sizeDelta = new Vector2(size, size);
            var c = eyeNormal;
            c.a = 0.22f * (1f - p);
            scanRing.color = c;
            if (p >= 1f)
            {
                scanTimer = -1f;
                scanRing.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && owner != null) owner.RobotAction();
    }
}
