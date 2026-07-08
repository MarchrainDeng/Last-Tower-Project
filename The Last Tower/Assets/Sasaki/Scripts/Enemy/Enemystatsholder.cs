using UnityEngine;

/// <summary>
/// 敵PrefabにアタッチしてInspectorでステータスを設定するコンポーネント
/// EnemyBase と一緒にアタッチする
/// </summary>
public class EnemyStatsHolder : MonoBehaviour
{
    public EnemyStats stats = new();
}