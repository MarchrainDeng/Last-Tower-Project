using System.Collections;
using UnityEngine;

/*
----------------------------------------
｡ｾｹｦﾄﾜ / 僂ﾄﾜ｡ｿ
ﾔﾚﾖｸｶｨﾎｻﾖﾃﾉ嵭ﾉﾎ・螢ｬｲ｢ﾆｽｻｬﾒﾆｶｯｵｽﾄｿｱ・ｻﾖﾃ｡｣

ﾖｸｶｨﾎｻﾖﾃ､ﾋ･ｪ･ﾖ･ｸ･ｧ･ｯ･ﾈ､嵭ﾉ､ｷ｡｢
ﾄｿ侏ﾎｻﾖﾃ､ﾞ､ﾇ･ｹ･爻`･ｺ､ﾋﾒﾆ・､ｹ､・｣

----------------------------------------
*/

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn")]

    // ﾒｪﾉ嵭ﾉｵﾄﾔ､ﾖﾆﾌ・
    // ﾉ嵭ﾉ､ｹ､・ﾗ･・ﾏ･ﾖ
    [SerializeField]
    private GameObject prefab;

    // ﾉ嵭ﾉﾎｻﾖﾃ
    // ﾉ嵭ﾉﾎｻﾖﾃ
    [SerializeField]
    private Transform spawnPoint;

    // ﾄｿｱ・ｻﾖﾃ
    // ﾄｿ侏ﾎｻﾖﾃ
    [SerializeField]
    private Transform targetPoint;

    // ﾒﾆｶｯﾋﾙｶﾈ
    // ﾒﾆ・ﾋﾙｶﾈ
    [SerializeField]
    private float moveSpeed = 5f;

    /// <summary>
    /// ﾉ嵭ﾉｲ｢ﾒﾆｶｯ
    /// ﾉ嵭ﾉ､ｷ､ﾆﾒﾆ・､ｹ､・
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

        //FinalSequenceManager.Instance.SetFinalCannon(obj);

        StartCoroutine(
            MoveCoroutine(
                obj.transform,
                targetPosition
            )
        );
    }

    /// <summary>
    /// ﾆｽｻｬﾒﾆｶｯ
    /// ･ｹ･爻`･ｺ､ﾋﾒﾆ・､ｹ､・
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