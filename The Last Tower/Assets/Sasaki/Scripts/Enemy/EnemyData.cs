using UnityEngine;

public enum EnemyType
{
    Melee,        // 近接：地上を歩いて台座下まで移動 → 塔を殴る
    FlyingBlock,  // 飛行A：飛びながら接近 → 邪魔ブロックを置く
    FlyingBeam,   // 飛行B：空中停止 → ビームで遠距離攻撃
}

[System.Serializable]
public class EnemyStats
{
    public EnemyType type;

    [Header("── 移動 ──────────────────────")]
    public float moveSpeed = 3f;    // 移動速度
    public float stopX = 5f;    // 台座からの停止距離

    [Header("── 攻撃 ──────────────────────")]
    public float attackDamage = 10f;   // 1回の攻撃ダメージ
    public float attackRate = 1.5f;  // 攻撃間隔（秒）

    [Header("── 見た目 ────────────────────")]

    [Header("── 飛行共通 ──────────────────")]
    public float flightY = 8f;    // 飛行・停止するY座標
    public float flightYMin = 6f;    // 飛行高さランダム下限（FlyingBlock）
    public float flightYMax = 12f;   // 飛行高さランダム上限（FlyingBlock）
    public float exitSpeedMultiplier = 1.5f; // 離脱速度倍率（FlyingBlock）

    [Header("── FlyingBlock専用 ───────────")]

    [Header("── FlyingBeam専用 ────────────")]
    public float beamDuration = 0.4f;  // ビーム照射時間（秒）
    public float beamStartWidth = 0.12f; // ビーム根元の太さ
    public float beamEndWidth = 0.05f; // ビーム先端の太さ
    public Color beamStartColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    public Color beamEndColor = new Color(1f, 0.8f, 0.3f, 0.6f);
}