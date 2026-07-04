using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  EnemyBase
// ═══════════════════════════════════════════════════════════════════
public abstract class EnemyBase : MonoBehaviour
{
    protected EnemyStats stats;
    protected TowerHP towerHP;
    protected Transform towerTransform;

    public void Init(EnemyStats s, TowerHP hp, Transform tower)
    {
        stats = s;
        towerHP = hp;
        towerTransform = tower;
        BuildVisual(s.color);
        OnInit();
    }

    protected virtual void OnInit() { }
    protected abstract IEnumerator BehaviorLoop();

    void Start() => StartCoroutine(BehaviorLoop());

    protected bool ReachedTarget(Vector2 target, float threshold = 0.3f)
        => Vector2.Distance(transform.position, target) < threshold;

    protected void MoveToward(Vector2 target, float speed)
        => transform.position = Vector2.MoveTowards(
               transform.position, target, speed * Time.deltaTime);

    void BuildVisual(Color color)
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = color;
        sr.sortingOrder = 5;
    }
}

// ═══════════════════════════════════════════════════════════════════
//  MeleeEnemy  ─  地上を歩いて台座下まで移動 → 塔を殴る
// ═══════════════════════════════════════════════════════════════════
public class MeleeEnemy : EnemyBase
{
    protected override IEnumerator BehaviorLoop()
    {
        // スポーン側に応じて台座から stopX の距離で停止
        float side = transform.position.x < towerTransform.position.x ? -1f : 1f;
        var dest = new Vector2(towerTransform.position.x + side * stats.stopX, transform.position.y);
        while (!ReachedTarget(dest, 0.4f))
        {
            MoveToward(dest, stats.moveSpeed);
            yield return null;
        }

        while (!towerHP.IsDead)
        {
            towerHP.TakeDamage(stats.attackDamage);
            yield return new WaitForSeconds(stats.attackRate);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  FlyingBlockEnemy  ─  飛行接近 → 邪魔ブロックを上に置く
// ═══════════════════════════════════════════════════════════════════
public class FlyingBlockEnemy : EnemyBase
{
    float flightY;

    protected override void OnInit()
    {
        flightY = Random.Range(stats.flightYMin, stats.flightYMax);
        transform.position = new Vector3(transform.position.x, flightY, 0f);
    }

    protected override IEnumerator BehaviorLoop()
    {
        var flyTarget = new Vector2(
            towerTransform.position.x + Random.Range(-1f, 1f),
            flightY
        );
        while (!ReachedTarget(flyTarget, 0.4f))
        {
            MoveToward(flyTarget, stats.moveSpeed);
            yield return null;
        }

        DropBlock();
        towerHP.TakeDamage(stats.attackDamage);

        var exit = new Vector2(transform.position.x + 20f, flightY);
        float timer = 0f;
        while (timer < 3f)
        {
            MoveToward(exit, stats.moveSpeed * stats.exitSpeedMultiplier);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void DropBlock()
    {
        var go = new GameObject("ObstacleBlock");
        go.transform.position = transform.position + Vector3.down * 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one * 0.9f;

        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = stats.obstacleBlockColor;
        sr.sortingOrder = 3;
    }
}

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