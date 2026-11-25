using UnityEngine;

/// <summary>
/// ワールド選択マップの1ワールド分の情報
/// Assets/Story/WorldSelect/Worlds/ に1ワールド1ファイルで置く（ビルダーが仮データを作る）
///
/// 【解放】
/// - unlockedByDefault が ON なら最初から入れる
/// - そうでなければ unlockAfter に入れたワールドが「全部」クリア済みになると解放
///   （例：W2 は W1、W3〜W6 は W2、W7 は W3〜W6）
///
/// 【クリア】
/// clearStageId のステージをクリアしたら、このワールドはクリア扱い（ふつうは最後のステージ）
/// </summary>
[CreateAssetMenu(menuName = "Story/World Data", fileName = "World_")]
public class WorldData : ScriptableObject
{
    [Header("── 基本 ──────────────────────")]
    [Tooltip("保存のキーになるID。後から変えると記録がずれるので注意")]
    public string worldId = "W1";
    [Tooltip("画面に出すワールド番号")]
    public int worldNumber = 1;
    public string worldName = "ワールド名";
    [Tooltip("詳細に出す説明（1行ずつ）")]
    [TextArea(2, 5)] public string description = "";
    [Tooltip("ボスワールドなら ON（表示が少し変わるだけ）")]
    public bool isBoss;

    [Header("── 遷移 ──────────────────────")]
    [Tooltip("決定で読み込むステージマップのシーン名（Build Settings に入っていること）。空なら「準備中」で入れない")]
    public string stageMapScene = "";
    [Tooltip("このワールドのステージ（詳細に「何ステージ中いくつクリア」を出すのに使う）")]
    public WorldMapStageData[] stages = new WorldMapStageData[0];

    [Header("── 解放とクリア ─────────────────")]
    public bool unlockedByDefault;
    [Tooltip("ここに入れたワールドを全部クリアすると解放（unlockedByDefault が OFF のとき）")]
    public WorldData[] unlockAfter = new WorldData[0];
    [Tooltip("このステージIDをクリアしたらワールドクリア。空ならワールド単位の記録（デバッグ用）で判定")]
    public string clearStageId = "";

    /// <summary>クリア判定に使う記録のID</summary>
    public string ClearRecordId => string.IsNullOrEmpty(clearStageId) ? "__world_" + worldId : clearStageId;

    public string NumberLabel => isBoss ? "WORLD " + worldNumber + "  BOSS" : "WORLD " + worldNumber;
}
