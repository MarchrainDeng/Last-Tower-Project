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
    public float knockbackThreshold = 30f;  // この累積ダメージでノックバック発動
    public float knockbackDistance = 4f;   // 後退距離
    public float knockbackDuration = 0.3f; // 後退にかかる時間（秒）
    public float returnDuration = 0.5f; // 戻るのにかかる時間（秒）
    public float knockbackArcHeight = 2f;   // 弧の高さ

    [Header("── 小突く（Poke）専用 ─────────")]
    public float pokeDistance = 1.5f;  // 突き出す距離
    public float pokeDuration = 0.15f; // 突き出す/引く時間（片道）

    [Header("── 攻撃順 ──────────────────────")]
    public BossPhaseOrder phaseOrder = BossPhaseOrder.HarassFirst;

    [Header("── 攻撃セット ──────────────────")]
    public List<BossActionData> harassActions = new(); // 妨害アクション
    public List<BossActionData> attackActions = new(); // 攻撃アクション
}