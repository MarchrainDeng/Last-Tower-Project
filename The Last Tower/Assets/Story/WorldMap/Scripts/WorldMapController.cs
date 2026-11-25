using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ワールドマップ（ステージ選択）
///
/// 乗り物で自由に走り回り、台座に近づくと簡易情報、決定で詳細、もう一度決定でステージ開始
/// 詳細を出している間は移動を止める。キャンセルで詳細を閉じて移動を再開
/// 出入口（門）で決定するとワールド選択へ戻る
///
/// 【操作】
/// - キーボード: WASD / 矢印キーで8方向移動、Space/Enter で決定、Esc/Backspace で戻る
/// - パッド  : Dpad(またはスティック)で移動、Aで決定、Bで戻る
///
/// 【デバッグ（エディタのみ）】
/// - F5: 近くの台座のステージをクリア扱いにする（星3・60秒）
/// - F9: ワールドマップの記録をすべて消して読み直す
/// </summary>
public class WorldMapController : MonoBehaviour
{
    [Header("── 参照 ──────────────────────")]
    public WorldMapVehicle vehicle;
    public WorldMapCameraFollow cameraFollow;
    public WorldMapDetailPanel detailPanel;
    public WorldMapSpot[] spots;
    [Tooltip("記録が何も無いときの出現場所（ふつうは出入口）")]
    public WorldMapSpot defaultSpot;

    [Header("── 入力 ───────────────────────")]
    public float stickDeadZone = 0.4f;

    [Header("── SE ────────────────────────")]
    public AudioSource audioSource;
    public AudioClip approachSE;
    public AudioClip confirmSE;
    public AudioClip cancelSE;
    public AudioClip deniedSE;

    WorldMapSpot nearest;
    bool leaving;

    void Start()
    {
        Time.timeScale = 1f;
        if (detailPanel != null) detailPanel.Hide();

        PlaceVehicle();

        // 初クリアで戻ってきたら、その台座にタワーを建てる
        string cleared = WorldMapProgress.JustClearedStageId;
        WorldMapProgress.JustClearedStageId = null;
        if (!string.IsNullOrEmpty(cleared))
        {
            var p = FindSpot(cleared) as WorldMapPedestal;
            if (p != null) p.PlayTowerRise();
        }
    }

    // 入場した台座の手前に復帰する。指定が無ければ最後に挑戦した台座、それも無ければ出入口
    void PlaceVehicle()
    {
        string id = WorldMapProgress.ArrivalSpotId;
        WorldMapProgress.ArrivalSpotId = null;
        if (string.IsNullOrEmpty(id)) id = WorldMapProgress.LastStageId;

        var spot = FindSpot(id);
        if (spot == null) spot = defaultSpot;

        if (vehicle != null && spot != null)
            vehicle.Teleport(spot.FrontPosition, spot.Position - spot.FrontPosition); // 台座の方を向いて立つ

        if (cameraFollow != null) cameraFollow.SnapToTarget();
    }

    WorldMapSpot FindSpot(string id)
    {
        if (string.IsNullOrEmpty(id) || spots == null) return null;
        foreach (var s in spots)
            if (s != null && s.SpotId == id) return s;
        return null;
    }

    void Update()
    {
        if (leaving) return;

#if UNITY_EDITOR
        DebugKeys();
#endif

        if (detailPanel != null && detailPanel.IsOpen)
        {
            vehicle.SetInput(Vector2.zero);
            UpdateDetail();
            return;
        }

        vehicle.SetInput(ReadMoveDirection());
        UpdateNearest();

        if (nearest != null && ConfirmPressed())
            Enter(nearest);
    }

    // ─── 台座への接近 ─────────────────────────────────────────────
    void UpdateNearest()
    {
        WorldMapSpot best = null;
        float bestDistance = float.MaxValue;
        Vector2 pos = vehicle.Position;

        foreach (var s in spots)
        {
            if (s == null) continue;
            float d = Vector2.Distance(pos, s.Position);
            if (d <= s.approachRadius && d < bestDistance)
            {
                best = s;
                bestDistance = d;
            }
        }

        if (best == nearest) return;
        if (nearest != null) nearest.SetNear(false);
        nearest = best;
        if (nearest != null)
        {
            nearest.SetNear(true);
            if (nearest.CanEnter) PlaySE(approachSE);
        }
    }

    // ─── 決定 ───────────────────────────────────────────────────
    void Enter(WorldMapSpot spot)
    {
        if (!spot.CanEnter)
        {
            // 未解放ステージには入れない
            PlaySE(deniedSE);
            return;
        }

        if (spot is WorldMapTownGate)
        {
            PlaySE(confirmSE);
            leaving = true;
            vehicle.SetInput(Vector2.zero);
            WorldMapProgress.GoToWorldSelect();
            return;
        }

        var pedestal = spot as WorldMapPedestal;
        if (pedestal == null || pedestal.stage == null || detailPanel == null) return;

        PlaySE(confirmSE);
        detailPanel.Show(pedestal.stage);
    }

    void UpdateDetail()
    {
        if (CancelPressed())
        {
            PlaySE(cancelSE);
            detailPanel.Hide();
            return;
        }

        if (!ConfirmPressed()) return;

        var pedestal = nearest as WorldMapPedestal;
        if (pedestal == null || pedestal.stage == null) return;

        if (string.IsNullOrEmpty(pedestal.stage.sceneName))
        {
            PlaySE(deniedSE);
            detailPanel.SetHint(true);
            return;
        }

        PlaySE(confirmSE);
        leaving = true;
        WorldMapProgress.StartStage(pedestal.stage);
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

        if (kb.f5Key.wasPressedThisFrame && nearest is WorldMapPedestal p && p.stage != null && p.CanEnter)
        {
            bool first = WorldMapProgress.RecordClear(p.stage.stageId, 60f, 3);
            WorldMapProgress.JustClearedStageId = null;
            foreach (var s in spots) if (s != null) s.Refresh();
            if (first) p.PlayTowerRise();
            Debug.Log("[WorldMap] デバッグ：" + p.stage.stageId + " をクリア扱いにしました");
        }

        if (kb.f9Key.wasPressedThisFrame)
        {
            WorldMapProgress.ResetAll();
            Debug.Log("[WorldMap] デバッグ：記録を消しました");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
#endif
}

