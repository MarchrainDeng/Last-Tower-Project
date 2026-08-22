using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    [Header("Destroy Setting")]
    public string[] destroyTags =
    {
        "Bullet",
        "Block"
    };

    private void OnTriggerEnter2D(Collider2D other)
    {
        foreach (string tag in destroyTags)
        {
            if (other.CompareTag(tag))
            {
                // 追加：Blockタグが削除される時だけ「落としたブロック」としてカウント
                if (tag == "Block" && GameStatsManager.Instance != null)
                    GameStatsManager.Instance.OnBlockDropped();

                Destroy(other.gameObject);
                return;
            }
        }
    }
}
