using UnityEngine;

/// <summary>
/// ワールドマップの台座1つ分のステージ情報
/// Assets/Story/WorldMap/Stages/ に1ステージ1ファイルで置く（ビルダーが仮データを作る）
/// </summary>
[CreateAssetMenu(menuName = "Story/World Map Stage", fileName = "Stage_")]
public class WorldMapStageData : ScriptableObject
{
    [Header("── 基本 ──────────────────────")]
    [Tooltip("保存のキーになるID。後から変えるとクリア記録が消えるので注意")]
    public string stageId = "1-1";
    [Tooltip("画面に出すステージ番号（例：1-2）")]
    public string stageNumber = "1-1";
    public string stageName = "ステージ名";

    [Header("── 遷移 ──────────────────────")]
    [Tooltip("決定で読み込むシーン名（Build Settings に入っていること）。空なら「準備中」で入れない")]
    public string sceneName = "";

    [Header("── 解放 ──────────────────────")]
    [Tooltip("最初から入れるステージは ON")]
    public bool unlockedByDefault;
    [Tooltip("このステージをクリアすると解放される（unlockedByDefault が OFF のとき）")]
    public WorldMapStageData unlockAfter;

    [Header("── 詳細表示 ────────────────────")]
    [Tooltip("詳細に出す主な報酬")]
    public string[] rewards = new string[0];
}
