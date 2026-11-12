using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 見下ろしマップの前後関係：足元のYが下（手前）のものほど前に描く
/// 動くもの（プレイヤー）に付けて毎フレーム更新する。動かない物はビルダーが OrderFor で並び順を入れる
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public class WorldMapDepthSort : MonoBehaviour
{
    SortingGroup group;

    public static int OrderFor(float y) => Mathf.RoundToInt(-y * 20f);

    void Awake()
    {
        group = GetComponent<SortingGroup>();
    }

    void LateUpdate()
    {
        group.sortingOrder = OrderFor(transform.position.y);
    }
}
