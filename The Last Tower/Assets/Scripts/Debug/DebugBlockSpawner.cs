using UnityEngine;
using UnityEngine.InputSystem;

public class DebugBlockSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    // 要生成的方块预制体
    // 生成するブロックのプレハブ
    public GameObject blockPrefab;

    // 生成位置
    // 生成位置
    public Transform spawnPoint;

    private void Update()
    {
        // 按下 J 键时生成方块
        // Jキーを押した時にブロックを生成する
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            SpawnBlock();
        }
    }

    private void SpawnBlock()
    {
        if (blockPrefab == null || spawnPoint == null)
        {
            // 缺少预制体或生成点
            // プレハブまたは生成位置が設定されていない
            Debug.LogWarning("Block prefab or spawn point is missing.");
            return;
        }

        // 在指定位置生成方块
        // 指定位置にブロックを生成する
        Instantiate(blockPrefab, spawnPoint.position, Quaternion.identity);
    }
}
