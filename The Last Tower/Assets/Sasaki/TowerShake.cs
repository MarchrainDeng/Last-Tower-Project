using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 【使い方】
/// 1. 空のGameObjectに このスクリプトをアタッチ
/// 2. Play → 左クリックで揺らす
/// 
/// ブロックはスタート時に自動生成、台座もコード内で自動生成
/// </summary>
public class TowerShake : MonoBehaviour
{
    [Header("ブロック設定")]
    public int blockCount = 6;
    public float cellSize = 1f;
    public float spawnHeight = 14f;   // 台座から何Unity単位上からドロップするか

    [Header("揺らし設定")]
    [Range(1f, 40f)] public float shakeForce = 12f;

    // ─── ミノ定義（I,O,T,L,J,S,Z）────────────────────────────────
    static readonly Vector2Int[][] SHAPES = {
        new[]{ new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // I
        new[]{ new Vector2Int(0,0),  new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // O
        new[]{ new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) }, // T
        new[]{ new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) }, // L
        new[]{ new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1)}, // J
        new[]{ new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // S
        new[]{ new Vector2Int(0,0),  new Vector2Int(1,0), new Vector2Int(-1,1),new Vector2Int(0,1) }, // Z
    };
    static readonly Color[] COLORS = {
        new Color(0f,   0.83f, 1f),    // I シアン
        new Color(1f,   0.87f, 0f),    // O 黄
        new Color(0.8f, 0.27f, 1f),    // T 紫
        new Color(1f,   0.55f, 0f),    // L オレンジ
        new Color(0.27f,0.53f, 1f),    // J 青
        new Color(0.27f,0.93f, 0.27f), // S 緑
        new Color(1f,   0.27f, 0.27f), // Z 赤
    };

    // ─── 内部 ─────────────────────────────────────────────────────
    List<Rigidbody2D> blocks = new();
    Rigidbody2D pedestalRb;
    float pedestalTopY;

    // ─── 起動 ─────────────────────────────────────────────────────
    void Start()
    {
        SetupCamera();
        pedestalTopY = CreatePedestal();
        StartCoroutine(SpawnBlocks());
    }

    // ─── 更新：左クリックで揺らす ─────────────────────────────────
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            Shake();
    }

    // ─── 揺らし ───────────────────────────────────────────────────
    void Shake()
    {
        // 台座を横に振動させる
        StartCoroutine(ShakePedestal());

        // 全ブロックにランダムインパルス
        foreach (var rb in blocks)
        {
            if (rb == null) continue;
            rb.AddForce(new Vector2(
                Random.Range(-shakeForce, shakeForce),
                Random.Range(0f, shakeForce * 0.4f)
            ), ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-shakeForce, shakeForce) * 0.2f, ForceMode2D.Impulse);
        }
    }

    IEnumerator ShakePedestal()
    {
        float duration = 0.5f;
        float frequency = 25f;
        float amplitude = 0.25f;
        float timer = 0f;
        Vector3 origin = pedestalRb.transform.position;

        while (timer < duration)
        {
            float envelope = Mathf.Sin(timer / duration * Mathf.PI); // 滑らかに減衰
            float offset = Mathf.Sin(timer * frequency) * amplitude * envelope;
            pedestalRb.MovePosition(origin + new Vector3(offset, 0f, 0f));
            timer += Time.deltaTime;
            yield return null;
        }
        pedestalRb.MovePosition(origin);
    }

    // ─── ブロック生成 ─────────────────────────────────────────────
    IEnumerator SpawnBlocks()
    {
        for (int i = 0; i < blockCount; i++)
        {
            int idx = Random.Range(0, SHAPES.Length);
            SpawnBlock(idx, pedestalTopY + spawnHeight);
            yield return new WaitForSeconds(0.4f);
        }
    }

    void SpawnBlock(int shapeIdx, float y)
    {
        var cells = SHAPES[shapeIdx];
        var color = COLORS[shapeIdx];

        // 親（Rigidbody2D を持つ）
        var go = new GameObject($"Block_{shapeIdx}_{blocks.Count}");
        go.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), y, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.angularDamping = 2f;
        rb.linearDamping = 0.3f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 各セルにCollider + Sprite
        foreach (var cell in cells)
        {
            // Collider用（スケール1のまま）
            var cellGo = new GameObject("cell");
            cellGo.transform.SetParent(go.transform, false);
            cellGo.transform.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);

            var col = cellGo.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * cellSize * 0.95f;

            // Sprite用（子に分離してスケールをかける）
            var cellVisual = new GameObject("visual");
            cellVisual.transform.SetParent(cellGo.transform, false);
            cellVisual.transform.localScale = Vector3.one * cellSize;

            var sr = cellVisual.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSquareSprite();
            sr.color = color;
            sr.sortingOrder = 1;
        }

        blocks.Add(rb);
    }

    // ─── 台座生成 ─────────────────────────────────────────────────
    /// <returns>台座の上面Y座標</returns>
    float CreatePedestal()
    {
        float w = 8f, h = 0.5f;
        var go = new GameObject("Pedestal");
        go.transform.position = new Vector3(0f, 0f, 0f);

        // Kinematic にすることで MovePosition で動かせる
        pedestalRb = go.AddComponent<Rigidbody2D>();
        pedestalRb.bodyType = RigidbodyType2D.Kinematic;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(w, h);

        // Spriteは子オブジェクトに分離（親のscaleをColliderに影響させない）
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(w, h, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSquareSprite();
        sr.color = new Color(0.3f, 0.3f, 0.45f);

        return go.transform.position.y + h * 0.5f;
    }

    // ─── カメラ ───────────────────────────────────────────────────
    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = 11f;
        cam.transform.position = new Vector3(0f, 7f, -10f);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.18f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    // ─── 汎用スプライト生成 ───────────────────────────────────────
    Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var fill = new Color[] {
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white,
            Color.white, Color.white, Color.white, Color.white,
        };
        tex.SetPixels(fill);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}