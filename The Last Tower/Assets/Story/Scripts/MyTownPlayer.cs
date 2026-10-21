using System;
using UnityEngine;

/// <summary>
/// マイタウンを歩き回るプレイヤー（今は仮の人型シルエット）
/// 入力は MyTownController が読み、ここでは移動・自動移動・登場演出だけを行う
/// 座標は親（MyTownWorld）のローカル座標で、pivotは足元。奥ほど小さく・遅くなる
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MyTownPlayer : MonoBehaviour
{
    [Header("── 移動 ──────────────────────")]
    public float moveSpeed = 600f;     // 一番手前での1秒あたりの移動量（Canvas座標）
    [Tooltip("奥に行くと上下の移動が遅く見えるよう、縦の速さにかける倍率")]
    public float verticalSpeedRate = 0.7f;

    [Header("── 登場演出 ────────────────────")]
    public Vector2 startPosition = new Vector2(0f, -180f); // 登場位置（画面の真ん中あたりの地面）
    public float appearDuration = 0.4f;

    RectTransform rect;
    MyTownWorld world;
    float appearTimer;

    // クリックした場所まで自動で歩く
    Vector2? walkTarget;
    Action onArrive;

    public Vector2 Position => rect.localPosition;
    public bool IsAppearing => appearTimer < appearDuration;
    public float DepthScale => world != null ? world.ScaleAt(Position.y) : 1f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        world = GetComponentInParent<MyTownWorld>();
    }

    /// <summary>
    /// 画面の真ん中から登場させる
    /// </summary>
    public void Appear()
    {
        rect.localPosition = ClampToArea(startPosition);
        appearTimer = 0f;
        rect.localScale = Vector3.zero;
        walkTarget = null;
    }

    void Update()
    {
        float pop = 1f;
        if (IsAppearing)
        {
            appearTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(appearTimer / appearDuration);
            // 少し大きくなってから戻る（イーズアウトバック）
            pop = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
        }
        rect.localScale = Vector3.one * DepthScale * pop;
    }

    /// <summary>
    /// 入力方向に1フレーム分移動する。入力があれば自動移動は取り消す
    /// </summary>
    public void Move(Vector2 direction)
    {
        float speed = moveSpeed * DepthScale * Time.unscaledDeltaTime;

        if (direction != Vector2.zero)
        {
            walkTarget = null;
            direction.y *= verticalSpeedRate;
            SetPosition(Position + direction * speed);
            return;
        }

        if (walkTarget.HasValue)
        {
            Vector2 next = Vector2.MoveTowards(Position, walkTarget.Value, speed);
            SetPosition(next);

            if ((next - walkTarget.Value).sqrMagnitude < 1f)
            {
                walkTarget = null;
                var callback = onArrive;
                onArrive = null;
                callback?.Invoke();
            }
        }
    }

    /// <summary>
    /// 指定位置まで歩き、着いたら arrived を呼ぶ
    /// </summary>
    public void WalkTo(Vector2 target, Action arrived)
    {
        walkTarget = ClampToArea(target);
        onArrive = arrived;
    }

    public void StopWalking()
    {
        walkTarget = null;
        onArrive = null;
    }

    void SetPosition(Vector2 position)
    {
        rect.localPosition = ClampToArea(position);
    }

    Vector2 ClampToArea(Vector2 position)
    {
        return world != null ? world.Clamp(position) : position;
    }
}
