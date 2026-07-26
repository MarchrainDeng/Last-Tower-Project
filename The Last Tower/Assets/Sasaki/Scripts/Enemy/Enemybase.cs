using UnityEngine;
using System.Collections;

// ═══════════════════════════════════════════════════════════════════
//  EnemyBase
// ═══════════════════════════════════════════════════════════════════
public class EnemyBase : MonoBehaviour
{
    protected EnemyStats stats;
    protected TowerHP towerHP;
    protected Transform towerTransform;

    public void Init(EnemyStats s, TowerHP hp, Transform tower)
    {
        stats = s;
        towerHP = hp;
        towerTransform = tower;
        OnInit();
    }

    protected virtual void OnInit() { }
    protected virtual IEnumerator BehaviorLoop() { yield break; }

    void Start() => StartCoroutine(BehaviorLoop());

    protected bool ReachedTarget(Vector2 target, float threshold = 0.3f)
        => Vector2.Distance(transform.position, target) < threshold;

    protected void MoveToward(Vector2 target, float speed)
        => transform.position = Vector2.MoveTowards(
               transform.position, target, speed * Time.deltaTime);

    // ─── 画面の高さに対する割合(0〜1)をワールド座標Yに変換 ─────────
    // 例: GetWorldYFromViewportRatio(0.6f) → 画面下から60%の高さのワールドY
    // カメラのサイズ・位置がHeightLineManagerで動的に変わっても追従する
    protected float GetWorldYFromViewportRatio(float ratio)
    {
        var cam = Camera.main;
        if (cam == null) return transform.position.y;

        float camBottom = cam.transform.position.y - cam.orthographicSize;
        float camHeight = cam.orthographicSize * 2f;
        return camBottom + camHeight * ratio;
    }
}