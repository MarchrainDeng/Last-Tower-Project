using UnityEngine;

/// <summary>
/// マイタウンの奥行き（遠近感）を管理する
///
/// 画面の上（奥）ほど小さく・遅く・霧がかって見えるようにする
/// - 歩ける範囲は奥ほど幅が狭い台形
/// - 子オブジェクト（建物・プレイヤー）は足元のY座標で並べ替え、手前ほど前に描く
///   （子のpivotは足元＝下端中央にしておく）
/// 座標はこのRectTransformのローカル座標（中心が原点）
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MyTownWorld : MonoBehaviour
{
    [Header("── 歩ける範囲（台形） ──────────")]
    public float nearY = -430f;        // 一番手前の足元の高さ
    public float farY = 60f;           // 一番奥（地平線の少し手前）の足元の高さ
    public float nearHalfWidth = 900f; // 手前での横幅の半分
    public float farHalfWidth = 560f;  // 奥での横幅の半分

    [Header("── 見た目 ──────────────────────")]
    public float nearScale = 1.1f;     // 手前での大きさ
    public float farScale = 0.5f;      // 奥での大きさ
    [Range(0f, 1f)] public float farFog = 0.55f; // 奥での霧の濃さ
    public Color fogColor = new Color(0.36f, 0.40f, 0.55f, 1f);

    /// <summary>
    /// 奥行き 0（手前）〜 1（奥）
    /// </summary>
    public float Depth(float y)
    {
        return Mathf.InverseLerp(nearY, farY, y);
    }

    public float ScaleAt(float y)
    {
        return Mathf.Lerp(nearScale, farScale, Depth(y));
    }

    public float FogAt(float y)
    {
        return Mathf.Lerp(0f, farFog, Depth(y));
    }

    public Color ApplyFog(Color color, float y)
    {
        Color c = Color.Lerp(color, fogColor, FogAt(y));
        c.a = color.a;
        return c;
    }

    /// <summary>
    /// 歩ける範囲（台形）に収める
    /// </summary>
    public Vector2 Clamp(Vector2 position)
    {
        position.y = Mathf.Clamp(position.y, nearY, farY);
        float halfWidth = Mathf.Lerp(nearHalfWidth, farHalfWidth, Depth(position.y));
        position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
        return position;
    }

    void LateUpdate()
    {
        SortByDepth();
    }

    /// <summary>
    /// 奥（Yが大きい）ものから順に並べ、手前のものを前に描く
    /// </summary>
    public void SortByDepth()
    {
        int count = transform.childCount;
        // 子は数個なので単純な挿入ソートで十分
        for (int i = 1; i < count; i++)
        {
            Transform current = transform.GetChild(i);
            int j = i;
            while (j > 0 && transform.GetChild(j - 1).localPosition.y < current.localPosition.y)
                j--;
            if (j != i) current.SetSiblingIndex(j);
        }
    }
}
