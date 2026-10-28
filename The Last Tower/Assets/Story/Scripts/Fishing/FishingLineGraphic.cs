using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 釣り糸を描くUI
/// 竿先から餌（魚）までを曲線で結ぶ。張りが弱いとたるみ、強いとまっすぐ震える
/// 座標は親（釣り画面のカメラ）のローカル座標。このオブジェクトは原点に置く
/// </summary>
public class FishingLineGraphic : MaskableGraphic
{
    public Vector2 start;
    public Vector2 end;
    [Tooltip("たるみの大きさ（線の長さに対する割合）")]
    public float sag;
    [Tooltip("震えの大きさ")]
    public float vibration;
    public float thickness = 3f;
    public int segments = 28;

    float phase;

    public void Refresh(float deltaTime)
    {
        phase += deltaTime * 40f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 dir = end - start;
        float length = dir.magnitude;
        if (length < 1f) return;

        // 上から見た池なので、たるみは糸の横（竿を持つ手の側）へ膨らませる
        Vector2 normal = new Vector2(-dir.y, dir.x) / length;
        Vector2 control = (start + end) * 0.5f + normal * sag * length * 0.5f;

        Vector2 prev = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 p = Bezier(start, control, end, t);
            // 両端は固定して、真ん中ほど大きく震わせる
            p += normal * Mathf.Sin(phase + t * 18f) * vibration * Mathf.Sin(t * Mathf.PI);
            AddSegment(vh, prev, p);
            prev = p;
        }
    }

    static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    void AddSegment(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 0.0001f) return;
        Vector2 n = new Vector2(-d.y, d.x).normalized * thickness * 0.5f;

        int index = vh.currentVertCount;
        var v = UIVertex.simpleVert;
        v.color = color;
        v.position = a - n; vh.AddVert(v);
        v.position = a + n; vh.AddVert(v);
        v.position = b + n; vh.AddVert(v);
        v.position = b - n; vh.AddVert(v);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
