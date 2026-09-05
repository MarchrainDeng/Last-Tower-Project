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

    [Header("Final Bullet")]
    [SerializeField] private GameObject finalBulletPrefab;

    // 炮弹生成位置
    // 砲弾の生成位置
    [SerializeField] private Transform bulletSpawnPoint;

    // 炮弹向上移动距离
    // 砲弾の上方向への移動距離
    [SerializeField] private float bulletMoveDistance = 10f;

    // 炮弹移动时间
    // 砲弾の移動時間
    [SerializeField] private float bulletMoveDuration = 2f;

    private GameObject finalCannon;

    [SerializeField] GameObject UI_1;
    [SerializeField] GameObject UI_2;

    [SerializeField] GameObject finalBoomObject;

    [Header("Final Result")]
    // 动画开始后等待多久显示最终结果选择
    // アニメーション開始後、最終結果選択を表示するまでの待機時間
    [SerializeField] private float resultShowDelay = 2f;

    // 最终结果选择器
    // 最終結果選択コントローラー
    [SerializeField] private FinalResultChooser finalResultChooser;

    [SerializeField] BlockManager blockManager;

    [SerializeField] BGMManager bgmManager;

    [SerializeField] AudioClip cannonShoot;
    [SerializeField] AudioClip cannonBoom;

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

        if (blockManager != null)
        {
            blockManager.SetFinalAttackStarted();
        }

        bgmManager.FadeOutBGM(0.5f, 2f);

        // 隐藏倒计时UI
        // カウントダウンUIを非表示にする
        if (countdownUI != null)
        {
            countdownUI.SetActive(false);
        }

        UI_1.SetActive(false);
        UI_2.SetActive(false);

        // 相机移动
        // カメラを移動する
        yield return MoveCameraToTarget();

        Debug.Log("相机移动完成");

        GameObject targetObject =
        GameObject.Find("FinalGunPivot");

        if (targetObject != null)
        {
            // 等待炮台旋转完成
            yield return RotateObject(
                targetObject.transform,
                270f,
                3f
            );
        }

        // 后续最终演出写在这里
        // この後に最終演出を追加する

        // 发射炮弹
        // 砲弾を発射する
        yield return FireFinalBullet();

        // =========================
        // 下一段演出写这里
        // =========================

        Debug.Log("炮弹上升演出完成");
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

        Quaternion targetRotation =
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

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            target.rotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        // 确保最终角度准确
        target.rotation = targetRotation;
    }

    /// <summary>
    /// 生成最终炮弹，并让炮弹与相机同步向上移动
    /// 最終砲弾を生成し、カメラと一緒に上方向へ移動する
    /// </summary>
    private IEnumerator FireFinalBullet()
    {
        SFXManager.Instance.PlaySFX(cannonShoot);

        if (finalBulletPrefab == null ||
            bulletSpawnPoint == null ||
            mainCamera == null)
        {
            yield break;
        }

        // 生成炮弹
        // 砲弾を生成する
        GameObject bullet =
            Instantiate(
                finalBulletPrefab,
                bulletSpawnPoint.position,
                bulletSpawnPoint.rotation
            );

        Vector3 bulletStartPosition =
            bullet.transform.position;

        Vector3 bulletTargetPosition =
            bulletStartPosition +
            Vector3.up * bulletMoveDistance;

        Vector3 cameraStartPosition =
            mainCamera.transform.position;

        Vector3 cameraTargetPosition =
            cameraStartPosition +
            Vector3.up * bulletMoveDistance;

        float timer = 0f;

        while (timer < bulletMoveDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / bulletMoveDuration
                );

            // 缓入缓出
            // イージング
            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            // 炮弹向上移动
            // 砲弾を上方向へ移動する
            bullet.transform.position =
                Vector3.Lerp(
                    bulletStartPosition,
                    bulletTargetPosition,
                    t
                );

            // 相机同步向上移动
            // カメラも同時に上方向へ移動する
            mainCamera.transform.position =
                Vector3.Lerp(
                    cameraStartPosition,
                    cameraTargetPosition,
                    t
                );

            yield return null;
        }

        // 保证最终位置准确
        // 最終位置を確実に設定する
        bullet.transform.position =
            bulletTargetPosition;

        mainCamera.transform.position =
            cameraTargetPosition;

        SFXManager.Instance.PlaySFX(cannonBoom);

        // 炮弹到达后立即销毁
        // 砲弾が到着したらすぐに削除する
        Destroy(bullet);


        if (finalBoomObject != null)
        {
            finalBoomObject.SetActive(true);
        }

        // 等待一段时间
        // 一定時間待機する
        yield return new WaitForSeconds(resultShowDelay);

        // 显示最终结果选择
        // 最終結果選択を表示する
        if (finalResultChooser != null)
        {
            bgmManager.PlayWinBGM();
            finalResultChooser.Show();
        }
    }
}
