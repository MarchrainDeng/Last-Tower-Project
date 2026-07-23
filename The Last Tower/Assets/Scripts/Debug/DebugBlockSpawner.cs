using UnityEngine;
using UnityEngine.InputSystem;

public class DebugBlockSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    // 要生成的方块预制体
    // 生成するブロックのプレハブ
    public GameObject[] powerBlockPrefab;
    public GameObject[] attackBlockPrefab;

    // 生成位置
    // 生成位置
    public Transform spawnPoint;

    private void Update()
    {
        // 按下 J 键时生成方块
        // Jキーを押した時にブロックを生成する
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            SpawnPowerBlock();
        }

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            SpawnAttackBlock();
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                SpawnPowerBlock();
            }

            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                SpawnAttackBlock();
            }
        }
    }

    private void SpawnPowerBlock()
    {
        if (powerBlockPrefab == null || spawnPoint == null|| powerBlockPrefab.Length == 0)
        {
            // 缺少预制体或生成点
            // プレハブまたは生成位置が設定されていない
            Debug.LogWarning("Block prefab or spawn point is missing.");
            return;
        }

        // 在指定位置生成方块
        // 指定位置にブロックを生成する
        int randomIndex = Random.Range(0, powerBlockPrefab.Length);

        // 生成方块
        // ブロックを生成する
        Instantiate(powerBlockPrefab[randomIndex], spawnPoint.position, Quaternion.identity);
    }

    private void SpawnAttackBlock()
    {
        if (attackBlockPrefab == null || spawnPoint == null || attackBlockPrefab.Length == 0)
        {
            // 缺少预制体或生成点
            // プレハブまたは生成位置が設定されていない
            Debug.LogWarning("Block prefab or spawn point is missing.");
            return;
        }

        // 在指定位置生成方块
        // 指定位置にブロックを生成する
        int randomIndex = Random.Range(0, attackBlockPrefab.Length);

        // 生成方块
        // ブロックを生成する
        Instantiate(attackBlockPrefab[randomIndex], spawnPoint.position, Quaternion.identity);
    }
}
