using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP演出仕様書：HPが赤くなった時の緊張感演出
///
/// 【概要】
/// ・TowerHPが赤ゲージ域（threshold4未満）に入ったら演出開始
/// ・画面の側（四辺）が pulseInterval 秒周期で暗くなる（脈打つビネット）
/// ・緊張感のある心臓音（Heartbeat）をループ再生
/// ・ゲームオーバーになったら演出もサウンドも停止する
///
/// 【使い方】
/// ・任意のGameObjectにアタッチし、towerHP に TowerHP をアサインする
/// ・heartbeatClip に Heartbeat の音声ファイルをアサインする
/// ・vignetteImage は未アサインなら実行時に自動生成する
///   （自前のビネット画像を使いたい場合はここにアサインする）
///
/// ※ TowerHP 側のコードには一切手を加えず、公開値の参照のみで判定している
/// </summary>
public class LowHPEffect : MonoBehaviour
{
    [Header("── 参照 ────────────────────────")]
    [SerializeField] private TowerHP towerHP;

    [Tooltip("画面の側を暗くするビネット用Image。未アサインなら自動生成する")]
    [SerializeField] private Image vignetteImage;

    [Tooltip("心臓音（Heartbeat）を再生するAudioSource。未アサインなら自動生成する")]
    [SerializeField] private AudioSource heartbeatSource;

    [SerializeField] private AudioClip heartbeatClip;

    [Header("── 発動条件 ────────────────────")]
    [Tooltip("HPがこの割合(%)未満になったら演出開始。TowerHPが赤くなる閾値(threshold4)に合わせる")]
    [Range(0f, 100f)]
    [SerializeField] private float activateThresholdPercent = 20f;

    [Tooltip("TowerHPのthreshold4を自動で使う（ONだとactivateThresholdPercentは無視される）")]
    [SerializeField] private bool useTowerHPThreshold = true;

    [Header("── 画面演出 ────────────────────")]
    [Tooltip("暗くなる周期（秒）")]
    [SerializeField] private float pulseInterval = 0.5f;

    [Tooltip("ビネットの色")]
    [SerializeField] private Color vignetteColor = new Color(0.6f, 0f, 0f);

    [Tooltip("脈の一番薄い時の濃さ")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.15f;

    [Tooltip("脈の一番濃い時の濃さ")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.55f;

    [Tooltip("演出開始・終了時のフェード時間（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("── サウンド ────────────────────")]
    [Range(0f, 1f)]
    [SerializeField] private float heartbeatVolume = 0.8f;

    // 演出中かどうか
    private bool isActive = false;

    // ゲームオーバー済みか（trueになったら二度と再開しない）
    private bool isGameOver = false;

    // 脈のタイマー
    private float pulseTimer = 0f;

    // 演出全体の強さ（0=完全に消えている 1=フル）
    private float intensity = 0f;

    void Start()
    {
        if (towerHP == null)
            towerHP = FindFirstObjectByType<TowerHP>();

        if (towerHP != null)
            towerHP.OnDead += OnGameOver;

        if (vignetteImage == null)
            vignetteImage = CreateVignette();

        if (heartbeatSource == null)
            heartbeatSource = CreateHeartbeatSource();

        // 最初は完全に見えない状態にしておく
        ApplyVignetteAlpha(0f);
    }

    void OnDestroy()
    {
        if (towerHP != null)
            towerHP.OnDead -= OnGameOver;
    }

    void Update()
    {
        if (isGameOver)
            return;

        bool shouldBeActive = ShouldActivate();

        if (shouldBeActive && !isActive)
            StartEffect();
        else if (!shouldBeActive && isActive)
            StopEffect();

        // 演出の強さをフェードさせる
        float target = isActive ? 1f : 0f;
        if (fadeDuration > 0f)
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime / fadeDuration);
        else
            intensity = target;

        UpdatePulse();
        UpdateHeartbeatVolume();
    }

    /// <summary>
    /// HPが赤ゲージ域に入っているか
    /// </summary>
    bool ShouldActivate()
    {
        if (towerHP == null || towerHP.IsDead)
            return false;

        if (towerHP.maxHP <= 0f)
            return false;

        float hpPercent = (towerHP.currentHP / towerHP.maxHP) * 100f;

        float threshold = useTowerHPThreshold
            ? towerHP.threshold4
            : activateThresholdPercent;

        return hpPercent < threshold;
    }

    void StartEffect()
    {
        isActive = true;
        pulseTimer = 0f;

        if (heartbeatSource != null && heartbeatClip != null && !heartbeatSource.isPlaying)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.loop = true;
            heartbeatSource.Play();
        }
    }

    void StopEffect()
    {
        isActive = false;
    }

    /// <summary>
    /// ゲームオーバー時：演出とサウンドを完全に止める
    /// （勝利時など、外部から強制停止したい時にも呼べる）
    /// </summary>
    public void OnGameOver()
    {
        isGameOver = true;
        isActive = false;
        intensity = 0f;

        ApplyVignetteAlpha(0f);

        if (heartbeatSource != null)
            heartbeatSource.Stop();
    }

    /// <summary>
    /// pulseInterval秒周期で画面の側を暗くする
    /// </summary>
    void UpdatePulse()
    {
        if (vignetteImage == null)
            return;

        if (intensity <= 0f)
        {
            ApplyVignetteAlpha(0f);
            return;
        }

        pulseTimer += Time.deltaTime;
        if (pulseInterval > 0f && pulseTimer >= pulseInterval)
            pulseTimer -= pulseInterval;

        // 0→1→0 を pulseInterval 秒で一往復させる
        float phase = pulseInterval > 0f ? (pulseTimer / pulseInterval) : 0f;
        float wave = Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f + 0.5f;

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave) * intensity;
        ApplyVignetteAlpha(alpha);
    }

    void ApplyVignetteAlpha(float alpha)
    {
        if (vignetteImage == null)
            return;

        var c = vignetteColor;
        c.a = alpha;
        vignetteImage.color = c;
    }

    void UpdateHeartbeatVolume()
    {
        if (heartbeatSource == null)
            return;

        heartbeatSource.volume = heartbeatVolume * intensity;

        // 完全に消えたら再生も止める
        if (intensity <= 0f && heartbeatSource.isPlaying)
            heartbeatSource.Stop();
    }

    // ─── ビネットUIを自動生成する ─────────────────────────────────
    Image CreateVignette()
    {
        var canvasGo = new GameObject("LowHPVignetteCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 他のUIより手前に出しつつ、リザルト等より奥になるようにしておく
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();

        var imageGo = new GameObject("Vignette");
        imageGo.transform.SetParent(canvasGo.transform, false);

        var image = imageGo.AddComponent<Image>();
        image.sprite = CreateVignetteSprite();
        image.raycastTarget = false;

        var rect = imageGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }

    /// <summary>
    /// 中央が透明、画面の側にいくほど濃くなるビネット画像を生成する
    /// </summary>
    Sprite CreateVignetteSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 中心からの距離（0=中心, 1=四辺の中央）
                float nx = (x / (float)(size - 1)) * 2f - 1f;
                float ny = (y / (float)(size - 1)) * 2f - 1f;

                // 画面の「側」を暗くしたいので、縦横それぞれの端への近さで判定する
                float edge = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                // 中央付近は完全に透明、端に近づくほど濃くする
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, edge));

                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    // ─── 心臓音用AudioSourceを自動生成する ───────────────────────
    AudioSource CreateHeartbeatSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }
}
