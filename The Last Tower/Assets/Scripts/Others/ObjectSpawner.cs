using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
在指定位置生成物体，并平滑移动到目标位置。

指定位置にオブジェクトを生成し、
目標位置までスムーズに移動する。

----------------------------------------
*/

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn")]

    // 要生成的预制体
    // 生成するプレハブ
    [SerializeField]
    private GameObject prefab;

    // 生成位置
    // 生成位置
    [SerializeField]
    private Transform spawnPoint;

    // 目标位置
    // 目標位置
    [SerializeField]
    private Transform targetPoint;

    // 移动速度
    // 移動速度
    [SerializeField]
    private float moveSpeed = 5f;

    /// <summary>
    /// 生成并移动
    /// 生成して移動する
    /// </summary>
    /*
    public void SpawnAndMove()
    {
        if (prefab == null ||
            spawnPoint == null ||
            targetPoint == null)
        {
            return;
        }

        GameObject obj = Instantiate(
            prefab,
            spawnPoint.position,
            Quaternion.identity
        );

        StartCoroutine(
            MoveCoroutine(
                obj.transform,
                targetPoint.position
            )
        );
    }*/

    public void SpawnAndMove(
    GameObject prefab,
    Vector3 spawnPosition,
    Vector3 targetPosition)
    {
        GameObject obj = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        StartCoroutine(
            MoveCoroutine(
                obj.transform,
                targetPosition
            )
        );
    }

    /// <summary>
    /// 平滑移动
    /// スムーズに移動する
    /// </summary>
    private IEnumerator MoveCoroutine(
        Transform target,
        Vector3 destination)
    {
        while (Vector3.Distance(
                   target.position,
                   destination) > 0.01f)
        {
            target.position =
                Vector3.MoveTowards(
                    target.position,
                    destination,
                    moveSpeed * Time.deltaTime
                );

            yield return null;
        }

        target.position = destination;
    }

}