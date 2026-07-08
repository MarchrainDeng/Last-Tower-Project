using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
处理敌人生命值相关事件

敵のライフ値に関するイベントを処理する

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ

【创建日期 / 作成日】
2026/07/08

---------------------------------------
*/

public class EnemyHealth : MonoBehaviour
{
    // 敌人属性
    // 敵ステータス
    private EnemyStatsHolder statsHolder;

    private void Awake()
    {
        // 获取敌人属性
        // 敵ステータスを取得する
        statsHolder = GetComponent<EnemyStatsHolder>();
    }

    /// <summary>
    /// 受到伤害
    /// ダメージを受ける
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (statsHolder == null)
            return;

        // 扣除生命值
        // HPを減少させる
        statsHolder.stats.HP -= damage;

        // 防止生命值小于0
        // HPが0未満にならないようにする
        statsHolder.stats.HP = Mathf.Max(0, statsHolder.stats.HP);

        Debug.Log($"Enemy HP : {statsHolder.stats.HP}");

        // 判断是否死亡
        // 死亡判定
        if (statsHolder.stats.HP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 敌人死亡
    /// 敵が死亡する
    /// </summary>
    private void Die()
    {
        // TODO：以后播放死亡动画、掉落金币等
        // TODO：今後、死亡アニメーションやドロップ処理などを追加

        Destroy(gameObject);
        Debug.Log(gameObject + " is dead.");
    }
}
