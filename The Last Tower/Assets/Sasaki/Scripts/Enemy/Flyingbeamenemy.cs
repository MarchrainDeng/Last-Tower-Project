using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  FlyingBeamEnemy  ─  空中停止 → ビームで遠距離攻撃
// ═══════════════════════════════════════════════════════════════════
public class FlyingBeamEnemy : EnemyBase
{
    LineRenderer beamLine;

    protected override void OnInit()
    {
        // 最初から指定のY座標で出現
        transform.position = new Vector3(transform.position.x, stats.flightY, 0f);

        // スポーン位置が台座より左なら左側、右なら右側で停止
        float side = transform.position.x < towerTransform.position.x ? -1f : 1f;
        float stopX = towerTransform.position.x + side * stats.stopX;

        beamLine = gameObject.AddComponent<LineRenderer>();
        beamLine.startWidth = stats.beamStartWidth;
        beamLine.endWidth = stats.beamEndWidth;
        beamLine.material = new Material(Shader.Find("Sprites/Default"));
        beamLine.startColor = stats.beamStartColor;
        beamLine.endColor = stats.beamEndColor;
        beamLine.positionCount = 2;
        beamLine.enabled = false;

        // 停止X位置を保存
        gameObject.name += $"_stopX{stopX:F1}";
        GetComponent<FlyingBeamEnemy>()._stopX = stopX;
    }

    float _stopX;

    protected override IEnumerator BehaviorLoop()
    {
        var stopPos = new Vector2(_stopX, transform.position.y);
        while (!ReachedTarget(stopPos, 0.3f))
        {
            MoveToward(stopPos, stats.moveSpeed);
            yield return null;
        }

        while (!towerHP.IsDead)
        {
            yield return StartCoroutine(FireBeam());
            yield return new WaitForSeconds(stats.attackRate);
        }
    }

    IEnumerator FireBeam()
    {
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
    }
}