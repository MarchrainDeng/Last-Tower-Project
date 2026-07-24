using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  FlyingBeamEnemy  ─  空中停止 → ビームで遠距離攻撃
// ═══════════════════════════════════════════════════════════════════
public class FlyingBeamEnemy : EnemyBase
{
    LineRenderer beamLine;
    SpriteRenderer sr;
    float _stopX;
    float _stopY;
    bool _isFiring = false; // ビーム発射中はふわふわ揺れを止める
    float _hoverPhase = 0f;  // 揺れの位相を途切れさせないための通しタイマー

    protected override void OnInit()
    {
        sr = GetComponent<SpriteRenderer>();

        // 停止するY座標をランダムで決定（個体ごとに変化を出す）
        _stopY = Random.Range(stats.beamFlightYMin, stats.beamFlightYMax);

        // スポーン位置が台座より左なら左側、右なら右側で停止
        float side = transform.position.x < towerTransform.position.x ? -1f : 1f;
        _stopX = towerTransform.position.x + side * stats.stopX;

        beamLine = gameObject.AddComponent<LineRenderer>();
        beamLine.startWidth = stats.beamStartWidth;
        beamLine.endWidth = stats.beamEndWidth;
        beamLine.material = new Material(Shader.Find("Sprites/Default"));
        beamLine.startColor = stats.beamStartColor;
        beamLine.endColor = stats.beamEndColor;
        beamLine.positionCount = 2;
        beamLine.enabled = false;

        gameObject.name += $"_stopX{_stopX:F1}_stopY{_stopY:F1}";
    }

    protected override IEnumerator BehaviorLoop()
    {
        yield return StartCoroutine(WarpToStopPosition());

        while (!towerHP.IsDead)
        {
            yield return StartCoroutine(HoverUntilFire());
        }
    }

    // ─── ワープ移動：直線を分割し、各中継点にランダムなブレを加えてワープ ──
    IEnumerator WarpToStopPosition()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(_stopX, _stopY, transform.position.z);

        int warpCount = stats.warpCount;
        float totalTime = stats.warpTotalDuration;
        float intervalTime = totalTime / warpCount;

        // 待機時間からフェード分を差し引く（フェードアウト＋フェードインの合計）
        float fadeEach = stats.warpFadeDuration;
        float waitTime = Mathf.Max(0f, intervalTime - fadeEach * 2f);

        for (int i = 1; i <= warpCount; i++)
        {
            float t = (float)i / warpCount;
            Vector3 basePos = Vector3.Lerp(startPos, endPos, t);

            // 最終地点（i == warpCount）はブレさせず、正確な攻撃ポイントに着地させる
            Vector3 warpPos = basePos;
            if (i < warpCount)
            {
                float offsetX = Random.Range(-stats.warpDeviation, stats.warpDeviation);
                float offsetY = Random.Range(-stats.warpDeviation, stats.warpDeviation);
                warpPos += new Vector3(offsetX, offsetY, 0f);
            }

            yield return new WaitForSeconds(waitTime);

            // 消える瞬間：フェードアウト
            yield return StartCoroutine(FadeSprite(1f, 0f, fadeEach));

            transform.position = warpPos;

            // 現れる瞬間：フェードイン
            yield return StartCoroutine(FadeSprite(0f, 1f, fadeEach));
        }
    }

    // ─── SpriteRendererのアルファをフェードさせる ──────────────────
    IEnumerator FadeSprite(float from, float to, float duration)
    {
        if (sr == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, timer / duration);
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        var final = sr.color;
        sr.color = new Color(final.r, final.g, final.b, to);
    }

    // ─── 攻撃地点で常時ふわふわ揺れ、発射の瞬間だけ静止 ────────────
    // 挙動: ワープ→攻撃位置→ホバリング→攻撃時→停止(freezeIn)→攻撃→停止解除(freezeOut)→ホバリング再開
    IEnumerator HoverUntilFire()
    {
        float waitTimer = 0f;

        while (waitTimer < stats.attackRate)
        {
            // _hoverPhaseは常時進み続ける通しタイマーなので、
            // 止まって再開しても揺れが途切れず自然につながる
            _hoverPhase += Time.deltaTime;

            float hoverOffset = Mathf.Sin(_hoverPhase * stats.hoverFrequency) * stats.hoverAmplitude;
            transform.position = new Vector3(_stopX, _stopY + hoverOffset, transform.position.z);

            waitTimer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(FireBeam());
    }

    IEnumerator FireBeam()
    {
        _isFiring = true;

        // 現在のふわふわ位置でそのままピタッと止める
        Vector3 freezePos = transform.position;

        // 停止クッション：揺れが止まってから少し間を置く
        yield return new WaitForSeconds(stats.beamFreezeInDuration);
        transform.position = freezePos;

        beamLine.enabled = true;
        float timer = 0f;

        while (timer < stats.beamDuration)
        {
            transform.position = freezePos; // 攻撃中も完全に静止
            beamLine.SetPosition(0, transform.position);
            beamLine.SetPosition(1, towerTransform.position + Vector3.up * 2f);
            timer += Time.deltaTime;
            yield return null;
        }

        beamLine.enabled = false;
        towerHP.TakeDamage(stats.attackDamage);

        // 停止解除クッション：攻撃後もしばらく静止を維持してから揺れ再開
        yield return new WaitForSeconds(stats.beamFreezeOutDuration);
        transform.position = freezePos;

        _isFiring = false;
    }
}