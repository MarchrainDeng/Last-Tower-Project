using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Move Settings")]

    // 相机移动速度
    // カメラ移動速度
    [SerializeField]
    private float moveSpeed = 3f;

    // 相机缩放速度
    // カメラズーム速度
    [SerializeField]
    private float sizeSpeed = 3f;

    private Camera cam;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    /// <summary>
    /// 平滑移动相机到指定位置和Size
    /// 指定位置とSizeへスムーズに移動する
    /// </summary>
    public void MoveToTarget()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(
            MoveCameraCoroutine(
                0.9f,
                6f
            )
        );
    }

    /// <summary>
    /// 平滑移动相机
    /// カメラをスムーズに移動する
    /// </summary>
    private IEnumerator MoveCameraCoroutine(
        float targetY,
        float targetSize)
    {
        while (true)
        {
            // 平滑移动Y
            // Y座標をスムーズに移動
            Vector3 pos = transform.position;

            pos.y = Mathf.MoveTowards(
                pos.y,
                targetY,
                moveSpeed * Time.deltaTime
            );

            transform.position = pos;

            // 平滑缩放
            // Sizeをスムーズに変更
            cam.orthographicSize =
                Mathf.MoveTowards(
                    cam.orthographicSize,
                    targetSize,
                    sizeSpeed * Time.deltaTime
                );

            // 到达目标
            // 目標到達
            if (Mathf.Abs(pos.y - targetY) < 0.01f &&
                Mathf.Abs(cam.orthographicSize - targetSize) < 0.01f)
            {
                pos.y = targetY;
                transform.position = pos;
                cam.orthographicSize = targetSize;
                break;
            }

            yield return null;
        }

        moveCoroutine = null;
    }
}