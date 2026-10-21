using System;
using UnityEngine;

/// <summary>
/// マイタウンを歩き回るプレイヤー（今は紫の四角）
/// 入力は MyTownController が読み、ここでは移動・自動移動・登場演出だけを行う
/// 座標は親（Canvas）のローカル座標。奥行きはなく普通のトップダウン
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MyTownPlayer : MonoBehaviour
{
    [Header("── 移動 ──────────────────────")]
    public float moveSpeed = 600f;     // 1秒あたりの移動量（Canvas座標）
    public float areaMargin = 20f;     // 画面端からの余白

    [Header("── 登場演出 ────────────────────")]
    public float appearDuration = 0.4f;

    RectTransform rect;
    RectTransform area;
    float appearTimer;

    // クリックした場所まで自動で歩く
    Vector2? walkTarget;
    Action onArrive;

    public Vector2 Position => rect.localPosition;
    public bool IsAppearing => appearTimer < appearDuration;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        area = rect.parent as RectTransform;
    }

    /// <summary>
    /// 画面の真ん中から登場させる
    /// </summary>
    public void Appear()
    {
        rect.localPosition = Vector3.zero;
        appearTimer = 0f;
        rect.localScale = Vector3.zero;
        walkTarget = null;
    }

    void Update()
    {
        if (!IsAppearing) return;

        appearTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(appearTimer / appearDuration);
        // 少し大きくなってから戻る（イーズアウトバック）
        float s = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
        rect.localScale = Vector3.one * s;
    }

    /// <summary>
    /// 入力方向に1フレーム分移動する。入力があれば自動移動は取り消す
    /// </summary>
    public void Move(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            walkTarget = null;
            SetPosition(Position + direction * moveSpeed * Time.unscaledDeltaTime);
            return;
        }

        if (walkTarget.HasValue)
        {
            Vector2 next = Vector2.MoveTowards(Position, walkTarget.Value, moveSpeed * Time.unscaledDeltaTime);
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
        if (area == null) return position;

        Rect r = area.rect;
        Vector2 half = rect.rect.size * 0.5f + Vector2.one * areaMargin;
        position.x = Mathf.Clamp(position.x, r.xMin + half.x, r.xMax - half.x);
        position.y = Mathf.Clamp(position.y, r.yMin + half.y, r.yMax - half.y);
        return position;
    }
}
