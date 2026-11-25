using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ワールドマップの進行（クリア状況・記録・解放状況・最後に挑戦した台座）を保存する
///
/// 【ステージ側から呼ぶもの】
/// - クリア      : WorldMapProgress.FinishStage(StageResult.Clear, stars)
/// - 失敗        : WorldMapProgress.FinishStage(StageResult.Fail)
/// - リタイア    : WorldMapProgress.FinishStage(StageResult.Retire)
/// どれもワールドマップへ戻り、入場した台座の手前に復帰する
/// 星の判定方法は未定なので、ステージ側で決めた数（0〜3）を渡す
/// クリアタイムは入場からの経過時間を自動で測る（ステージ側で測った値を渡すこともできる）
///
/// 保存は PlayerPrefs に JSON で1つにまとめている
/// </summary>
public static class WorldMapProgress
{
    public const string WorldSelectSceneName = "WorldSelectScene";
    public const string StageMapSceneName = "StageMapScene";
    public const string TownSceneName = "MyTownScene";

    /// <summary>ステージマップの出入口（ワールド選択へ戻る門）を表すID（復帰位置に使う）。保存データと合わせるため値は昔のまま</summary>
    public const string TownGateId = "__town";

    const string SaveKey = "WorldMap_Progress";

    public enum StageResult { Clear, Fail, Retire }

    [Serializable]
    public class StageRecord
    {
        public string stageId;
        public bool cleared;
        public bool unlocked;          // クリア等で解放された（最初から解放のステージは記録しない）
        public float bestTime = -1f;   // 秒。未クリアは -1
        public int stars;              // 0〜3（今までの最高）
        public int playCount;
    }

    [Serializable]
    class SaveData
    {
        public string lastStageId = "";
        public string lastWorldId = "";
        public List<StageRecord> stages = new List<StageRecord>();
    }

    static SaveData data;

    // ─── 今回の起動中だけ使う状態 ─────────────────────────────────
    // 挑戦中のステージ（ステージ側が FinishStage を呼ぶまで保持）
    static WorldMapStageData playingStage;
    static float playStartTime;

    /// <summary>
    /// 次にワールドマップを開いたとき、どの台座（またはマイタウン出入口）の手前に出すか
    /// null なら「続きから」として最後に挑戦した台座の手前
    /// </summary>
    public static string ArrivalSpotId { get; set; }

    /// <summary>ステージをクリアして戻ってきたとき、その台座のタワー出現演出を出すため</summary>
    public static string JustClearedStageId { get; set; }

    // ─── 読み取り ───────────────────────────────────────────────
    public static string LastStageId => Data.lastStageId;

    public static StageRecord GetRecord(string stageId)
    {
        foreach (var r in Data.stages)
            if (r.stageId == stageId) return r;
        return null;
    }

    public static bool IsCleared(WorldMapStageData stage)
    {
        var r = stage != null ? GetRecord(stage.stageId) : null;
        return r != null && r.cleared;
    }

    public static bool IsUnlocked(WorldMapStageData stage)
    {
        if (stage == null) return false;
        if (stage.unlockedByDefault) return true;
        var r = GetRecord(stage.stageId);
        if (r != null && (r.unlocked || r.cleared)) return true;
        // 前のステージがクリア済みなら解放（ステージの並びを後から変えても整合するように毎回判定する）
        return stage.unlockAfter != null && IsCleared(stage.unlockAfter);
    }

    // ─── ステージの開始と終了 ─────────────────────────────────────
    /// <summary>
    /// 台座からステージを開始する（WorldMapController から呼ぶ）
    /// </summary>
    public static void StartStage(WorldMapStageData stage)
    {
        if (stage == null || string.IsNullOrEmpty(stage.sceneName)) return;

        playingStage = stage;
        playStartTime = Time.realtimeSinceStartup;

        Data.lastStageId = stage.stageId;
        GetOrCreate(stage.stageId).playCount++;
        Save();

        LoadScene(stage.sceneName);
    }

    /// <summary>
    /// ステージを終えてワールドマップへ戻る
    /// clearTime を省略（負の値）すると、入場からの経過時間をクリアタイムにする
    /// </summary>
    public static void FinishStage(StageResult result, int stars = 0, float clearTime = -1f)
    {
        string stageId = playingStage != null ? playingStage.stageId : Data.lastStageId;

        if (result == StageResult.Clear && !string.IsNullOrEmpty(stageId))
        {
            if (clearTime < 0f) clearTime = Time.realtimeSinceStartup - playStartTime;
            RecordClear(stageId, clearTime, stars);
        }

        playingStage = null;
        ArrivalSpotId = stageId;
        LoadScene(StageMapSceneName);
    }

    /// <summary>
    /// クリアを記録する（ワールドマップのデバッグ操作からも使う）
    /// 初クリアなら true を返す
    /// </summary>
    public static bool RecordClear(string stageId, float clearTime, int stars)
    {
        var r = GetOrCreate(stageId);
        bool first = !r.cleared;
        r.cleared = true;
        if (clearTime >= 0f && (r.bestTime < 0f || clearTime < r.bestTime)) r.bestTime = clearTime;
        r.stars = Mathf.Max(r.stars, Mathf.Clamp(stars, 0, 3));
        Save();

        if (first) JustClearedStageId = stageId;
        return first;
    }

    /// <summary>
    /// ストーリー進行など、クリア以外の理由でステージを解放する
    /// </summary>
    public static void Unlock(string stageId)
    {
        GetOrCreate(stageId).unlocked = true;
        Save();
    }

    /// <summary>
    /// ワールドマップの記録をすべて消す（デバッグ用）
    /// </summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        data = null;
        ArrivalSpotId = null;
        JustClearedStageId = null;
    }

    // ─── ワールド ──────────────────────────────────────────────────
    /// <summary>最後に入ったワールド（ワールド選択に戻ったときのカーソル位置）</summary>
    public static string LastWorldId => Data.lastWorldId;

    public static bool IsWorldCleared(WorldData world)
    {
        var r = world != null ? GetRecord(world.ClearRecordId) : null;
        return r != null && r.cleared;
    }

    /// <summary>unlockAfter のワールドが全部クリア済みなら解放</summary>
    public static bool IsWorldUnlocked(WorldData world)
    {
        if (world == null) return false;
        if (world.unlockedByDefault) return true;
        if (world.unlockAfter == null || world.unlockAfter.Length == 0) return false;
        foreach (var w in world.unlockAfter)
            if (w != null && !IsWorldCleared(w)) return false;
        return true;
    }

    public static int ClearedStageCount(WorldData world)
    {
        int n = 0;
        if (world != null && world.stages != null)
            foreach (var s in world.stages)
                if (IsCleared(s)) n++;
        return n;
    }

    /// <summary>
    /// ワールド選択からそのワールドのステージマップへ入る（入口の前に出る）
    /// </summary>
    public static void EnterWorld(WorldData world)
    {
        if (world == null || string.IsNullOrEmpty(world.stageMapScene)) return;
        Data.lastWorldId = world.worldId;
        Save();
        ArrivalSpotId = TownGateId;
        LoadScene(world.stageMapScene);
    }

    // ─── シーンの行き来 ─────────────────────────────────────────────
    // マイタウン ⇄ ワールド選択 ⇄ ステージマップ ⇄ ステージ
    public static void GoToWorldSelect()
    {
        LoadScene(WorldSelectSceneName);
    }

    public static void GoToTown()
    {
        LoadScene(TownSceneName);
    }

    // ─── 内部 ───────────────────────────────────────────────────
    static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    static StageRecord GetOrCreate(string stageId)
    {
        var r = GetRecord(stageId);
        if (r != null) return r;
        r = new StageRecord { stageId = stageId };
        Data.stages.Add(r);
        return r;
    }

    static void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        data = null;
        if (!string.IsNullOrEmpty(json))
        {
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e) { Debug.LogWarning("[WorldMapProgress] 保存データを読めませんでした: " + e.Message); }
        }
        if (data == null) data = new SaveData();
        if (data.stages == null) data.stages = new List<StageRecord>();
    }

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        if (SceneFadeManager.Instance != null) SceneFadeManager.Instance.LoadScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }
}
