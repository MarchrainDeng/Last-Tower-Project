using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ストーリーモードの拠点「マイタウン」
///
/// プレイヤーはマップを自由に移動でき、利用できる場所の前で決定を押すとその場所のUIを表示する
/// カメラは固定。最初はプレイヤーが画面の真ん中から登場する
/// 奥行き（奥ほど小さく・遅く・霧がかる）は MyTownWorld が管理する
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

    [Header("── 釣り ───────────────────────")]
    public MyTownFishing fishing;
    [Tooltip("池の前に来た時にプレイヤーの頭上に出す案内")]
    public GameObject pondPrompt;
    [Tooltip("一番手前の地面からこの距離以内なら池の前とみなす（Canvas座標）")]
    public float pondReach = 50f;
    [Tooltip("池の横幅の半分（Canvas座標）")]
    public float pondHalfWidth = 500f;

    [Header("── 入力 ───────────────────────")]
    public float stickDeadZone = 0.5f;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip confirmSE;

    int selectedIndex = -1;
    MyTownWorld world;

    void Start()
    {
        Time.timeScale = 1f;
        if (player != null) world = player.GetComponentInParent<MyTownWorld>();

        for (int i = 0; i < locations.Length; i++)
            locations[i].Init(this, i);

        if (panel != null) panel.Hide();
        if (pondPrompt != null) pondPrompt.SetActive(false);

        if (player != null) player.Appear();
    }

    void Update()
    {
        // 釣り中は釣り画面に操作を任せる
        if (fishing != null && fishing.IsActive) return;

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

        // 建物の前でなければ、池の前で釣りができる
        bool nearPond = selectedIndex < 0 && IsNearPond();
        if (pondPrompt != null && pondPrompt.activeSelf != nearPond) pondPrompt.SetActive(nearPond);

        if (ConfirmPressed())
        {
            if (selectedIndex >= 0) OpenSelected();
            else if (nearPond) StartFishing();
        }
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
        Vector2 front = new Vector2(r.center.x, r.yMin - frontOffset * DepthScaleAt(r.yMin));
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

    // ─── 釣り ───────────────────────────────────────────────────
    // 池をクリックしたら、岸のクリックした位置まで歩いて釣りを始める（MyTownPond から呼ばれる）
    public void ClickPond(Vector2 screenPosition)
    {
        if (panel != null && panel.IsOpen) return;
        if (fishing == null || fishing.IsActive) return;

        if (player == null)
        {
            StartFishing();
            return;
        }

        var space = (RectTransform)player.transform.parent;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenPosition, null, out var local);
        float shoreY = world != null ? world.nearY : player.Position.y;
        player.WalkTo(new Vector2(Mathf.Clamp(local.x, -pondHalfWidth, pondHalfWidth), shoreY), StartFishing);
    }

    bool IsNearPond()
    {
        if (fishing == null || player == null || world == null) return false;
        return player.Position.y <= world.nearY + pondReach && Mathf.Abs(player.Position.x) <= pondHalfWidth;
    }

    void StartFishing()
    {
        if (fishing == null || fishing.IsActive) return;
        if (player != null) player.StopWalking();
        if (pondPrompt != null) pondPrompt.SetActive(false);
        Select(-1);
        PlaySE(confirmSE);
        fishing.Begin(null);
    }

    // ─── 選択 ───────────────────────────────────────────────────
    // プレイヤーに一番近い、解放済みの場所。近くに無ければ -1
    int FindNearestLocation()
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < locations.Length; i++)
        {
            if (!locations[i].IsUnlocked) continue;

            Rect r = GetLocalRect(locations[i]);
            // 奥の建物は小さく見えるので、入れる距離も奥行きに合わせて縮める
            float d = DistanceToRect(player.Position, r);
            if (d <= interactDistance * DepthScaleAt(r.yMin) && d < bestDistance)
            {
                best = i;
                bestDistance = d;
            }
        }
        return best;
    }

    float DepthScaleAt(float y)
    {
        return world != null ? world.ScaleAt(y) : 1f;
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
