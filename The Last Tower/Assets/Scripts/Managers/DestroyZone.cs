using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    [Header("Destroy Setting")]
    // 可以被销毁的Tag
    // 削除対象Tag
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
                Destroy(other.gameObject);
                return;
            }
        }
    }
}
