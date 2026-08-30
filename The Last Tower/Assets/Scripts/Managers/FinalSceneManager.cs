using System.Collections;
using UnityEngine;

public class FinalSequenceManager : MonoBehaviour
{
    public static FinalSequenceManager Instance;

    [Header("References")]
    [SerializeField] private GameObject countdownUI;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;

    [Header("Camera Settings")]
    [SerializeField] private float cameraMoveDuration = 2f;

    // 最终相机大小
    // 最終的なカメラサイズ
    [SerializeField] private float targetCameraSize = 6f;

    private GameObject finalCannon;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 开始最终攻击演出
    /// 最終攻撃演出を開始する
    /// </summary>
    public void StartFinalAttackSequence()
    {
        StartCoroutine(FinalAttackSequence());
    }

    private IEnumerator FinalAttackSequence()
    {
        // 隐藏倒计时UI
        // カウントダウンUIを非表示にする
        if (countdownUI != null)
        {
            countdownUI.SetActive(false);
        }

        // 相机移动
        // カメラを移動する
        yield return MoveCameraToTarget();

        Debug.Log("相机移动完成");

        GameObject targetObject =
        GameObject.Find("FinalGunPivot");

        if (targetObject != null)
        {
            StartCoroutine(
                RotateObject(
                    targetObject.transform,
                    270f,
                    2f
                )
            );
        }

        // 后续最终演出写在这里
        // この後に最終演出を追加する
    }

    private IEnumerator MoveCameraToTarget()
    {
        if (mainCamera == null || cameraTarget == null)
            yield break;

        Vector3 startPosition =
            mainCamera.transform.position;

        Vector3 targetPosition =
            cameraTarget.position;

        // 保持相机原来的Z坐标
        // カメラの元のZ座標を維持する
        targetPosition.z = startPosition.z;

        // 记录相机开始时的Size
        // 開始時のカメラサイズを保存する
        float startCameraSize =
            mainCamera.orthographicSize;

        float timer = 0f;

        while (timer < cameraMoveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / cameraMoveDuration
            );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            // 平滑移动相机
            // カメラを滑らかに移動する
            mainCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            // 平滑改变相机Size
            // カメラサイズを滑らかに変更する
            mainCamera.orthographicSize =
                Mathf.Lerp(
                    startCameraSize,
                    targetCameraSize,
                    t
                );

            yield return null;
        }

        // 确保最终值准确
        // 最終値を確実に設定する
        mainCamera.transform.position =
            targetPosition;

        mainCamera.orthographicSize =
            targetCameraSize;
    }

    private IEnumerator RotateObject(
    Transform target,
    float targetAngle,
    float duration)
    {
        Quaternion startRotation =
            target.rotation;

        Quaternion endRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            target.rotation =
                Quaternion.Lerp(
                    startRotation,
                    endRotation,
                    t
                );

            yield return null;
        }

        target.rotation =
            endRotation;
    }
}
