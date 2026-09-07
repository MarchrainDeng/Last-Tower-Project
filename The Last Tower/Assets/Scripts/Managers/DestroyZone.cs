using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    [Header("Destroy Setting")]
    public string[] destroyTags =
    {
        "Bullet",
        "TowerBlock"
    };

    [Header("Stats")]
    // 「落としたブロック」としてカウントするタグ
    // ブロックのタグ（destroyTagsにも含めておくこと）
    [SerializeField]
    private string blockTag = "TowerBlock";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ブロックはタグが親（ピースのルート）に付いていて、
        // Colliderは子セル（Untagged）側に付いている。
        // そのため other をそのまま判定するとタグが一致せず、
        // ブロックが削除されず「落としたブロック」もカウントされない。
        // Rigidbody2Dはルートに付いているのでそこから親を解決する。
        GameObject target = ResolveTarget(other);

        if (target == null)
            return;

        foreach (string destroyTag in destroyTags)
        {
            if (target.CompareTag(destroyTag))
            {
                // 追加：ブロックが削除される時だけ「落としたブロック」としてカウント
                if (destroyTag == blockTag && GameStatsManager.Instance != null)
                    GameStatsManager.Instance.OnBlockDropped();

                Destroy(target);
                return;
            }
        }
    }

    /// <summary>
    /// 判定対象となるGameObjectを解決する
    /// （子セルのColliderが入ってきた場合はピースのルートを返す）
    /// </summary>
    private GameObject ResolveTarget(Collider2D other)
    {
        if (other == null)
            return null;

        // Rigidbody2Dはピースのルートに付いている
        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.gameObject;

        return other.gameObject;
    }
}
