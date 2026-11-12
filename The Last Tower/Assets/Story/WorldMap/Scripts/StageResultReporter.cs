using UnityEngine;

/// <summary>
/// ステージのシーンに置いて、結果をワールドマップへ伝える部品
/// クリア演出・失敗演出・ポーズメニューのボタンなどの UnityEvent からこのメソッドを呼ぶ
/// （コードから直接 WorldMapProgress.FinishStage を呼んでもよい）
///
/// 星の判定は未定なので、今は starsOnClear の固定値を渡す
/// </summary>
public class StageResultReporter : MonoBehaviour
{
    [Tooltip("クリア時に渡す星の数（判定方法が決まるまでの仮）")]
    [Range(0, 3)] public int starsOnClear = 1;

    public void ReportClear()
    {
        WorldMapProgress.FinishStage(WorldMapProgress.StageResult.Clear, starsOnClear);
    }

    public void ReportClearWithStars(int stars)
    {
        WorldMapProgress.FinishStage(WorldMapProgress.StageResult.Clear, stars);
    }

    public void ReportFail()
    {
        WorldMapProgress.FinishStage(WorldMapProgress.StageResult.Fail);
    }

    public void ReportRetire()
    {
        WorldMapProgress.FinishStage(WorldMapProgress.StageResult.Retire);
    }
}
