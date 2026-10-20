using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ストーリーモードの拠点「マイタウン」
///
/// 【操作】
/// - マウス  : 場所をクリックでその場所のUIを表示、右クリックで戻る
/// - キーボード: A/← で左、D/→ で右の場所へ移動、Enter/Space で決定、Esc/Backspace で戻る
/// - パッド  : Dpad(またはスティック)で移動、Aで決定、Bで戻る
///
/// locations は画面の左→右の順に並べる
/// </summary>
public class MyTownController : MonoBehaviour
{
    [Header("── 場所（左→右の順） ──────────")]
    public MyTownLocationButton[] locations;
    public MyTownLocationPanel panel;

    [Header("── 2.5D 背景（選択位置に応じてずらす） ──")]
    public RectTransform[] parallaxLayers;
    public float[] parallaxStrength;

    [Header("── 入力 ───────────────────────")]
    public float stickDeadZone = 0.5f;
    public float inputCooldown = 0.2f;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip confirmSE;

    int selectedIndex = -1;
    float inputTimer;
    Vector2[] layerBasePositions;

    void Start()
    {
        Time.timeScale = 1f;

        for (int i = 0; i < locations.Length; i++)
            locations[i].Init(this, i);

        layerBasePositions = new Vector2[parallaxLayers.Length];
        for (int i = 0; i < parallaxLayers.Length; i++)
            layerBasePositions[i] = parallaxLayers[i].anchoredPosition;

        if (panel != null) panel.Hide();

        Select(FindUnlocked(-1, +1));
    }

    void Update()
    {
        inputTimer -= Time.unscaledDeltaTime;

        if (panel != null && panel.IsOpen)
        {
            if (CancelPressed() || ConfirmPressed()) ClosePanel();
            return;
        }

        int dir = ReadMoveDirection();
        if (dir != 0)
        {
            int next = FindUnlocked(selectedIndex, dir);
            if (next != selectedIndex) { Select(next); PlaySE(moveSE); }
        }

        if (ConfirmPressed()) OpenSelected();
    }

    // ─── 入力 ───────────────────────────────────────────────────
    int ReadMoveDirection()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) return -1;
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) return +1;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame) return -1;
            if (pad.dpad.right.wasPressedThisFrame) return +1;

            if (inputTimer <= 0f)
            {
                float x = pad.leftStick.x.ReadValue();
                if (x < -stickDeadZone) { inputTimer = inputCooldown; return -1; }
                if (x > stickDeadZone) { inputTimer = inputCooldown; return +1; }
            }
        }
        return 0;
    }

    static bool ConfirmPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
        return MenuInput.ConfirmPressed();
    }

    static bool CancelPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
        return MenuInput.CancelPressed();
    }

    // ─── マウス（MyTownLocationButton から呼ばれる） ─────────────
    public void HoverLocation(int index)
    {
        if (panel != null && panel.IsOpen) return;
        if (!locations[index].IsUnlocked || index == selectedIndex) return;
        Select(index);
        PlaySE(moveSE);
    }

    public void ClickLocation(int index)
    {
        if (panel != null && panel.IsOpen) return;
        if (!locations[index].IsUnlocked) return;
        Select(index);
        OpenSelected();
    }

    // 「戻る」ボタン用
    public void ClosePanel()
    {
        if (panel != null) panel.Hide();
    }

    // ─── 選択 ───────────────────────────────────────────────────
    // from の隣から dir 方向に最初の解放済みの場所を探す。無ければ from のまま
    int FindUnlocked(int from, int dir)
    {
        for (int i = from + dir; i >= 0 && i < locations.Length; i += dir)
            if (locations[i].IsUnlocked) return i;
        return from;
    }

    void Select(int index)
    {
        if (index < 0 || index >= locations.Length) return;
        selectedIndex = index;
        for (int i = 0; i < locations.Length; i++)
            locations[i].SetSelected(i == selectedIndex);
        UpdateParallax();
    }

    void OpenSelected()
    {
        if (selectedIndex < 0 || panel == null) return;
        PlaySE(confirmSE);
        panel.Show(locations[selectedIndex].displayName);
    }

    void UpdateParallax()
    {
        if (locations.Length < 2) return;
        // 選択位置を -1(左端)〜+1(右端) にして、レイヤーごとの強さでずらす
        float t = selectedIndex / (float)(locations.Length - 1) * 2f - 1f;
        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            float k = i < parallaxStrength.Length ? parallaxStrength[i] : 0f;
            parallaxLayers[i].anchoredPosition = layerBasePositions[i] + new Vector2(-t * k, 0f);
        }
    }

    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
