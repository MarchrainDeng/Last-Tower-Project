using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  FlyingBeamEnemy  ─  空中停止 → ビームで遠距離攻撃
// ═══════════════════════════════════════════════════════════════════
public class FlyingBeamEnemy : EnemyBase
{
    LineRenderer beamLine;
    float _stopX;
    float _stopY;
    bool _isFiring = false; // ビーム発射中はふわふわ揺れを止める

    protected override void OnInit()
    {
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

            yield return new WaitForSeconds(intervalTime);
            transform.position = warpPos;
        }
    }

    // ─── 攻撃地点で常時ふわふわ揺れ、発射の瞬間だけ静止 ────────────
    IEnumerator HoverUntilFire()
    {
        float hoverTimer = 0f;

        while (hoverTimer < stats.attackRate)
        {
            if (!_isFiring)
            {
                float hoverOffset = Mathf.Sin(hoverTimer * stats.hoverFrequency) * stats.hoverAmplitude;
                transform.position = new Vector3(_stopX, _stopY + hoverOffset, transform.position.z);
            }
            hoverTimer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(FireBeam());
    }

    IEnumerator FireBeam()
    {
        _isFiring = true;

        // 発射の瞬間は静止位置に固定
        transform.position = new Vector3(_stopX, _stopY, transform.position.z);

        beamLine.enabled = true;
        float timer = 0f;

        while (timer < stats.beamDuration)
        {
            beamLine.SetPosition(0, transform.position);
            beamLine.SetPosition(1, towerTransform.position + Vector3.up * 2f);
            timer += Time.deltaTime;
            yield return null;
        }

        beamLine.enabled = false;
        towerHP.TakeDamage(stats.attackDamage);

        _isFiring = false;
    }
}