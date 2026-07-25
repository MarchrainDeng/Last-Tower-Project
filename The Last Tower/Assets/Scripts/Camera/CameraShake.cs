using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]

    // 震动持续时间
    // シェイクの継続時間
    [SerializeField]
    private float shakeDuration = 0.2f;

    // 震动强度
    // シェイクの強さ
    [SerializeField]
    private float shakeMagnitude = 0.1f;

    // 当前震动协程
    // 現在のシェイクコルーチン
    private Coroutine shakeCoroutine;

    // 本次震动开始时的相机位置
    // 今回のシェイク開始時のカメラ位置
    private Vector3 shakeBasePosition;

    /// <summary>
    /// 开始相机震动
    /// カメラシェイクを開始する
    /// </summary>
    public void Shake()
    {
        // 如果上一次震动尚未结束，先停止
        // 前回のシェイクが終了していない場合は停止する
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);

            // 恢复上一次震动前的位置
            // 前回のシェイク前の位置へ戻す
            transform.position = shakeBasePosition;
        }

        // 每次震动时保存当前相机位置
        // シェイク開始時に現在のカメラ位置を保存する
        shakeBasePosition = transform.position;

        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            // 生成随机震动偏移
            // ランダムなシェイクオフセットを生成する
            Vector2 randomOffset =
                Random.insideUnitCircle * shakeMagnitude;

            transform.position =
                shakeBasePosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            yield return null;
        }

        // 返回本次震动开始前的位置
        // 今回のシェイク開始前の位置へ戻す
        transform.position = shakeBasePosition;

        shakeCoroutine = null;
    }

    /// <summary>
    /// 立即停止震动
    /// シェイクを即座に停止する
    /// </summary>
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.position = shakeBasePosition;
    }
}