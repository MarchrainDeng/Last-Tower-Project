using UnityEngine;
using System.Collections;

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

}