using UnityEngine;

[System.Serializable]
public class BlockOption
{
    // 方块类型名
    // ブロックタイプ名
    public string typeName;

    // 卡牌显示用图片
    // カード表示用画像
    public Sprite cardSprite;

    // 实际生成的方块Prefab
    // 実際に生成するブロックPrefab
    public GameObject blockPrefab;
}
