using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ストーリーモードの拠点「マイタウン」
///
/// プレイヤーはマップを自由に移動でき、利用できる場所の前で決定を押すとその場所のUIを表示する
/// カメラは固定。最初はプレイヤーが画面の真ん中から登場する
///
/// 【操作】
/// - キーボード: WASD / 矢印キーで移動、Enter/Space で決定、Esc/Backspace で戻る
/// - パッド  : Dpad(またはスティック)で移動、Aで決定、Bで戻る
/// - マウス  : 場所をクリックするとプレイヤーがそこまで歩いてUIを表示、右クリックで戻る
/// </summary>
public class MyTownController : MonoBehaviour
{
    [Header("── 場所 ──────────────────────")]
    public MyTownLocationButton[] locations;
    public MyTownLocationPanel panel;

    [Header("── プレイヤー ────────────────────")]
    public MyTownPlayer player;
    [Tooltip("場所からこの距離以内に近づくと決定で入れる（Canvas座標）")]
    public float interactDistance = 60f;
    [Tooltip("クリックで歩いていく時、場所の下端からどれだけ手前で止まるか")]
    public float frontOffset = 50f;

    [Header("── 入力 ───────────────────────")]
    public float stickDeadZone = 0.5f;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip confirmSE;

    int selectedIndex = -1;

    void Start()
    {
        Time.timeScale = 1f;

        for (int i = 0; i < locations.Length; i++)
            locations[i].Init(this, i);

        if (panel != null) panel.Hide();

        if (player != null) player.Appear();
    }

    void Update()
    {
        if (panel != null && panel.IsOpen)
        {
            if (CancelPressed() || ConfirmPressed()) ClosePanel();
            return;
        }

        if (player == null || player.IsAppearing) return;

        player.Move(ReadMoveDirection());

        int nearest = FindNearestLocation();
        if (nearest != selectedIndex)
        {
            Select(nearest);
            if (nearest >= 0) PlaySE(moveSE);
        }

        if (ConfirmPressed()) OpenSelected();
    }

    // ─── 入力 ───────────────────────────────────────────────────
    Vector2 ReadMoveDirection()
    {
        Vector2 dir = Vector2.zero;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            dir += pad.dpad.ReadValue();

            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude > stickDeadZone) dir += stick;
        }

        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
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
    // クリックした場所の前までプレイヤーを歩かせ、着いたらUIを表示する
    public void ClickLocation(int index)
    {
        if (panel != null && panel.IsOpen) return;
        if (!locations[index].IsUnlocked) return;

        if (player == null)
        {
            Select(index);
            OpenSelected();
            return;
        }

        Rect r = GetLocalRect(locations[index]);
        Vector2 front = new Vector2(r.center.x, r.yMin - frontOffset);
        player.WalkTo(front, () =>
        {
            Select(index);
            OpenSelected();
        });
    }

    // 「戻る」ボタン用
    public void ClosePanel()
    {
        if (panel != null) panel.Hide();
    }

    // ─── 選択 ───────────────────────────────────────────────────
    // プレイヤーに一番近い、解放済みの場所。近くに無ければ -1
    int FindNearestLocation()
    {
        int best = -1;
        float bestDistance = interactDistance;
        for (int i = 0; i < locations.Length; i++)
        {
            if (!locations[i].IsUnlocked) continue;

            float d = DistanceToRect(player.Position, GetLocalRect(locations[i]));
            if (d <= bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }
        return best;
    }

    // 場所の範囲をプレイヤーの親（Canvas）の座標で取得
    Rect GetLocalRect(MyTownLocationButton location)
    {
        var corners = new Vector3[4];
        ((RectTransform)location.transform).GetWorldCorners(corners);
        Transform space = player.transform.parent;
        Vector2 min = space.InverseTransformPoint(corners[0]);
        Vector2 max = space.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static float DistanceToRect(Vector2 point, Rect rect)
    {
        float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
        float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    void Select(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < locations.Length; i++)
            locations[i].SetSelected(i == selectedIndex);
    }

    void OpenSelected()
    {
        if (selectedIndex < 0 || panel == null) return;
        if (player != null) player.StopWalking();
        PlaySE(confirmSE);
        panel.Show(locations[selectedIndex].displayName);
    }

    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
