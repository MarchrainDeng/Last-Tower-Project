/// <summary>
/// ステージマップからワールド選択へ戻る出入口
/// ワールド選択から来たときはこの手前に出る
/// </summary>
public class WorldMapTownGate : WorldMapSpot
{
    public string displayName = "ワールド選択";

    public override string SpotId => WorldMapProgress.TownGateId;
    public override bool CanEnter => true;

    protected override string BuildSignText()
    {
        return displayName + "\n<size=80%>決定：戻る</size>";
    }
}
