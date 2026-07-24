using UnityEngine;
/*
----------------------------------------
【修改 / 変更】
增加了HP属性。
HP属性が追加されました。
【修改人 / 担当者】
Deng Guangpeng
トウ　コウホウ
【修改日期 / 日付】
2026/07/08
---------------------------------------
*/
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
    [Header("── HP ──────────────────────")]
    //トウが追加した部分
    public float HP = 100f;//HP
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
    public float blockCarryPeakHeight = 1.5f; // 最高高度への上乗せ幅
    public float blockCarryRiseDuration = 0.3f; // 最高高度まで上がる時間
    public float blockCarrySettleHeight = 0.5f; // 落ち着く高さの上乗せ幅
    public float blockCarrySettleDuration = 0.4f; // 落ち着くまでの時間
    public float blockCarryHoldDuration = 2f;   // ブロックを持ち続ける時間
    public float blockCarrySwayAmount = 0.2f; // 持っている間の揺れ幅
    public float blockCarrySwaySpeed = 1.5f; // 持っている間の揺れ速さ
    public float blockExitDuration = 3f;   // ブロックを落とした後、離脱するまでの時間
    [Header("── FlyingBeam専用 ────────────")]
    public float beamFlightYMin = 5f;    // 停止するY座標のランダム下限
    public float beamFlightYMax = 9f;    // 停止するY座標のランダム上限
    public int warpCount = 3;    // 攻撃ポイントまでのワープ回数
    public float warpTotalDuration = 3f;   // ワープ移動の合計時間（秒）
    public float warpDeviation = 1f;   // 中継点のランダムなブレ幅
    public float warpFadeDuration = 0.15f; // ワープ時のフェードイン/アウト時間
    public float hoverAmplitude = 0.3f;  // 攻撃待機中の上下揺れ幅
    public float hoverFrequency = 2f;    // 攻撃待機中の上下揺れ速さ
    public float beamFreezeInDuration = 0.1f; // 揺れ停止→攻撃までのクッション時間
    public float beamFreezeOutDuration = 0.1f; // 攻撃→揺れ再開までのクッション時間
    public float beamDuration = 0.4f;  // ビーム照射時間（秒）
    public float beamStartWidth = 0.12f; // ビーム根元の太さ
    public float beamEndWidth = 0.05f; // ビーム先端の太さ
    public Color beamStartColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    public Color beamEndColor = new Color(1f, 0.8f, 0.3f, 0.6f);
}