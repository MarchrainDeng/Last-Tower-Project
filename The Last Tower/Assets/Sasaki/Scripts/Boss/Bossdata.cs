using UnityEngine;
using System.Collections.Generic;

// ─── 攻撃の種類 ───────────────────────────────────────────────────
public enum BossActionType
{
    Punch,      // 妨害：パンチで地面を揺らす
    Paint,      // 妨害：画面にペイントボールを投げる
    Juggling,   // 妨害：ブロックを掴んでジャグリング
    Poke,       // 攻撃：タワーを小突く
    Flick,      // 攻撃：デコピン（高ダメージ・吹き飛ばし）
}

public enum BossPhaseOrder
{
    HarassFirst,    // 妨害→攻撃
    AttackFirst,    // 攻撃→妨害
    Alternate,      // 交互
    Random,         // 完全ランダム
}

// ─── 攻撃1つ分の設定 ─────────────────────────────────────────────
[System.Serializable]
public class BossActionData
{
    public BossActionType type;
    [Range(0f, 1f)]
    public float weight = 1f;   // 出現確率の重み
    public float damage = 10f;  // タワーへのダメージ
    public float cooldown = 2f;   // 次の行動までの待機（秒）
    public float range = 3f;   // 攻撃発動距離
}

// ─── 片手分の設定 ─────────────────────────────────────────────────
[System.Serializable]
public class BossHandData
{
    [Header("── HP ────────────────────────")]
    public float maxHP = 100f;

    [Header("── 移動 ────────────────────────")]
    public float moveSpeed = 2f;
    public float approachStopX = 3f;   // タワーからの停止距離

    [Header("── ノックバック ────────────────")]
    public int knockbackHitThreshold = 3;    // この被弾回数でノックバック発動
    public float knockbackDistance = 4f;   // 後退距離
    public float knockbackDuration = 0.3f; // 後退にかかる時間（秒）
    public float returnDuration = 0.5f; // 戻るのにかかる時間（秒）
    public float knockbackArcHeight = 2f;   // 弧の高さ

    [Header("── 小突く（Poke）専用 ─────────")]
    public float pokeDistance = 1.5f;  // 突き出す距離
    public float pokeDuration = 0.15f; // 突き出す/引く時間（片道）

    [Header("── パンチ（Punch）専用 ─────────")]
    public float punchShakeAmplitude = 0.3f; // 揺れの振幅
    public float punchShakeDuration = 0.5f; // 揺れの持続時間
    public float punchShakeFrequency = 25f;  // 揺れの周波数

    [Header("── ジャグリング（Juggling）専用 ──")]
    public string towerBlockTag = "TowerBlock"; // タワーブロックに付けるTag
    public float jugglingHoldOffsetY = 0.5f; // 手からの保持オフセット
    public float jugglingHeight = 0.5f; // 揺れの高さ
    public float jugglingFrequency = 8f;   // 揺れの速さ
    public float jugglingDuration = 2f;   // ジャグリングを続ける時間

    [Header("── ペイント（Paint）専用 ────────")]
    public float paintDuration = 2f;  // ペイントUIを表示する時間

    [Header("── デコピン（Flick）専用 ────────")]
    public float flickForceX = 8f;   // 横方向の吹き飛ばし力
    public float flickForceY = 6f;   // 上方向の吹き飛ばし力
    public float flickTorque = 5f;   // 回転力

    [Header("── アニメーション ────────────")]
    public string punchAnimTrigger = "Punch";
    public float punchAnimDuration = 0.5f;  // Punchアニメーションの再生時間

    public string pokeAnimTrigger = "Tuttuki";
    public float pokeAnimDuration = 0.3f;  // Tuttukiアニメーションの再生時間

    public string flickAnimTrigger = "Dekopin";
    public float flickAnimDuration = 0.5f;  // Dekopinアニメーションの再生時間

    [Header("── 攻撃順 ──────────────────────")]
    public BossPhaseOrder phaseOrder = BossPhaseOrder.HarassFirst;

    [Header("── 攻撃セット ──────────────────")]
    public List<BossActionData> harassActions = new(); // 妨害アクション
    public List<BossActionData> attackActions = new(); // 攻撃アクション
}