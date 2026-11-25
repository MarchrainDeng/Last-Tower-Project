using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ワールド選択マップ
///
/// 1枚のマップにワールドが並び、カーソルでワールドを選ぶ。決定で詳細、もう一度決定でそのワールドのステージマップへ
/// 未解放のワールドにもカーソルは乗るが、入れない（詳細に解放条件を出す）
///
/// 【操作】
/// - マウス    : ワールドに乗せると選択、クリックで決定。詳細の外をクリック／右クリックで閉じる
/// - キーボード: WASD／矢印キーでその方向のワールドへカーソル移動、Space/Enter で決定、Esc/Backspace で戻る
/// - パッド    : Dpad(またはスティック)でカーソル移動、Aで決定、Bで戻る
/// 詳細が出ていないときに「戻る」でマイタウンへ
///
/// 【デバッグ（エディタのみ）】
/// - F5: 選択中のワールドをクリア扱いにする
/// - F9: 記録をすべて消して読み直す
/// </summary>
public class WorldSelectController : MonoBehaviour
{
    [Header("── 参照 ──────────────────────")]
    public WorldSelectNode[] nodes;
    [Tooltip("記録が何も無いときに選んでおくワールド（ふつうはワールド1）")]
    public WorldSelectNode defaultNode;
    public WorldSelectDetailPanel detailPanel;
    public Camera cam;

    [Header("── カーソル ────────────────────")]
    public Transform cursor;
    public float cursorMoveTime = 0.18f;
    public float cursorBobHeight = 0.12f;
    public float cursorBobSpeed = 5f;

    [Header("── 入力 ───────────────────────")]
    public float stickDeadZone = 0.5f;
    [Tooltip("押しっぱなしで次へ進むまでの時間")]
    public float repeatDelay = 0.4f;
    public float repeatInterval = 0.2f;
    [Tooltip("この角度の内側にあるワールドだけを移動先の候補にする（入力方向との差の cos）")]
    [Range(0f, 1f)] public float directionThreshold = 0.3f;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip moveSE;
    public AudioClip confirmSE;
    public AudioClip cancelSE;
    public AudioClip deniedSE;

    int current = -1;
    bool leaving;

    Vector2 heldDirection;
    float repeatTimer;

    Vector2 cursorFrom, cursorTo;
    float cursorT = 1f;

    WorldSelectNode Current => current >= 0 && current < nodes.Length ? nodes[current] : null;

    void Start()
    {
        Time.timeScale = 1f;
        if (cam == null) cam = Camera.main;
        if (detailPanel != null) detailPanel.Hide();

        // 最後に入ったワールドを選んでおく。無ければ既定（ワールド1）
        int index = IndexOf(WorldMapProgress.LastWorldId);
        if (index < 0 && defaultNode != null) index = System.Array.IndexOf(nodes, defaultNode);
        if (index < 0) index = 0;
        Select(index, true);
    }

    int IndexOf(string worldId)
    {
        if (string.IsNullOrEmpty(worldId) || nodes == null) return -1;
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i] != null && nodes[i].WorldId == worldId) return i;
        return -1;
    }

    void Update()
    {
        UpdateCursor();
        if (leaving) return;

#if UNITY_EDITOR
        DebugKeys();
#endif

        if (detailPanel != null && detailPanel.IsOpen)
        {
            UpdateDetail();
            return;
        }

        UpdateNavigation();
        UpdateMouse();

        if (ConfirmPressed())
        {
            Open();
            return;
        }
        if (CancelPressed())
        {
            PlaySE(cancelSE);
            leaving = true;
            WorldMapProgress.GoToTown();
        }
    }

    // ─── 選択 ───────────────────────────────────────────────────
    void Select(int index, bool instant = false)
    {
        if (index == current || index < 0 || index >= nodes.Length || nodes[index] == null) return;
        if (Current != null) Current.SetSelected(false);
        current = index;
        Current.SetSelected(true);

        cursorFrom = cursor != null ? (Vector2)cursor.position : Current.CursorPosition;
        cursorTo = Current.CursorPosition;
        cursorT = instant ? 1f : 0f;
        if (!instant) PlaySE(moveSE);
    }

    void UpdateCursor()
    {
        if (cursor == null || Current == null) return;
        if (cursorT < 1f) cursorT = Mathf.Min(1f, cursorT + Time.deltaTime / Mathf.Max(0.01f, cursorMoveTime));
        float t = cursorT * cursorT * (3f - 2f * cursorT);
        Vector2 p = Vector2.Lerp(cursorFrom, cursorTo, t);
        p.y += Mathf.Abs(Mathf.Sin(Time.time * cursorBobSpeed)) * cursorBobHeight;
        cursor.position = new Vector3(p.x, p.y, cursor.position.z);
    }

    // 入力方向にあるワールドのうち、近くて方向が合っているものへ移る
    void UpdateNavigation()
    {
        // 一瞬だけ押されたキー（フレームの間に離された）も1回分の移動にする
        Vector2 tap = ReadTapDirection();
        if (tap != Vector2.zero)
        {
            heldDirection = tap;
            repeatTimer = repeatDelay;
            MoveToward(tap);
            return;
        }

        Vector2 dir = ReadDirection();
        if (dir == Vector2.zero)
        {
            heldDirection = Vector2.zero;
            return;
        }

        bool fresh = heldDirection == Vector2.zero || Vector2.Dot(heldDirection, dir) < 0.7f;
        if (fresh)
        {
            heldDirection = dir;
            repeatTimer = repeatDelay;
            MoveToward(dir);
            return;
        }

        repeatTimer -= Time.deltaTime;
        if (repeatTimer <= 0f)
        {
            repeatTimer = repeatInterval;
            MoveToward(dir);
        }
    }

    void MoveToward(Vector2 dir)
    {
        if (Current == null) return;
        Vector2 from = Current.Position;
        int best = -1;
        float bestScore = float.MaxValue;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (i == current || nodes[i] == null) continue;
            Vector2 d = nodes[i].Position - from;
            float dist = d.magnitude;
            if (dist < 0.01f) continue;
            float cos = Vector2.Dot(dir.normalized, d / dist);
            if (cos < directionThreshold) continue;
            float score = dist * (1f + (1f - cos) * 2f); // 方向のずれが大きいほど遠い扱い
            if (!nodes[i].IsUnlocked) score *= 1.15f;    // 同じくらいの候補なら、入れるワールドを優先
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        if (best >= 0) Select(best);
    }

    void UpdateMouse()
    {
        int hovered = NodeUnderPointer();
        if (hovered >= 0 && hovered != current && MenuInput.PointerMoved()) Select(hovered);
        if (hovered >= 0 && MenuInput.LeftClicked())
        {
            Select(hovered);
            Open();
        }
    }

    int NodeUnderPointer()
    {
        if (cam == null || Mouse.current == null) return -1;
        Vector2 p = cam.ScreenToWorldPoint(MenuInput.PointerPosition());
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i] != null && nodes[i].Contains(p)) return i;
        return -1;
    }

    // ─── 決定 ───────────────────────────────────────────────────
    void Open()
    {
        if (Current == null || Current.world == null || detailPanel == null) return;
        PlaySE(Current.IsUnlocked ? confirmSE : deniedSE);
        detailPanel.Show(Current.world);
    }

    void UpdateDetail()
    {
        bool clicked = MenuInput.LeftClicked();
        bool clickedInside = clicked && detailPanel.IsPointerOverFrame();

        if (CancelPressed() || (clicked && !clickedInside))
        {
            PlaySE(cancelSE);
            detailPanel.Hide();
            return;
        }

        if (!ConfirmPressed() && !clickedInside) return;

        var world = Current != null ? Current.world : null;
        if (world == null) return;

        if (!Current.IsUnlocked || string.IsNullOrEmpty(world.stageMapScene))
        {
            PlaySE(deniedSE);
            detailPanel.SetHint(true);
            return;
        }

        PlaySE(confirmSE);
        leaving = true;
        WorldMapProgress.EnterWorld(world);
    }

    // ─── 入力 ───────────────────────────────────────────────────
    Vector2 ReadDirection()
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

        return dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.zero;
    }

    static Vector2 ReadTapDirection()
    {
        Vector2 dir = Vector2.zero;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) dir.x -= 1f;
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) dir.x += 1f;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) dir.y -= 1f;
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) dir.y += 1f;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.left.wasPressedThisFrame) dir.x -= 1f;
            if (pad.dpad.right.wasPressedThisFrame) dir.x += 1f;
            if (pad.dpad.down.wasPressedThisFrame) dir.y -= 1f;
            if (pad.dpad.up.wasPressedThisFrame) dir.y += 1f;
        }

        return dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.zero;
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

    void PlaySE(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    void DebugKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f5Key.wasPressedThisFrame && Current != null && Current.world != null && Current.IsUnlocked)
        {
            WorldMapProgress.RecordClear(Current.world.ClearRecordId, 60f, 3);
            WorldMapProgress.JustClearedStageId = null;
            foreach (var n in nodes) if (n != null) n.Refresh();
            Debug.Log("[WorldSelect] デバッグ：" + Current.world.worldId + " をクリア扱いにしました");
        }

        if (kb.f9Key.wasPressedThisFrame)
        {
            WorldMapProgress.ResetAll();
            Debug.Log("[WorldSelect] デバッグ：記録を消しました");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
#endif
}
