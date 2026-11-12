/// <summary>
/// ワールドマップからマイタウンへ戻る出入口
/// マイタウンから来たときはこの手前に出る
/// </summary>
public class WorldMapTownGate : WorldMapSpot
{
    public string displayName = "マイタウン";

    public override string SpotId => WorldMapProgress.TownGateId;
    public override bool CanEnter => true;

    protected override string BuildSignText()
    {
        return displayName + "\n<size=80%>決定：戻る</size>";
    }
}
