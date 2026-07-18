using System.Collections;
using UnityEngine;

/*
----------------------------------------
【功能 / 機能】
控制相机震动。

カメラのシェイクを制御する。

【负责人 / 担当】
Deng Guangpeng
トウ　コウホウ
----------------------------------------
*/

public class CameraShake : MonoBehaviour
{
    [Header("Shake")]

    // 默认震动时间
    // デフォルトのシェイク時間
    public float duration = 0.15f;

    // 默认震动幅度
    // デフォルトのシェイク強度
    public float magnitude = 0.08f;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// 播放震动
    /// シェイクを再生する
    /// </summary>
    public void Shake()
    {
        Shake(duration, magnitude);
    }

    /// <summary>
    /// 自定义震动参数
    /// パラメータ指定でシェイクを再生する
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float timer = 0;

        while (timer < duration)
        {
            Vector2 offset = Random.insideUnitCircle * magnitude;

            transform.localPosition = originalPosition +
                                      new Vector3(offset.x, offset.y, 0);

            timer += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
    }
}