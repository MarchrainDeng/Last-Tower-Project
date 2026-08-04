using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadVibrationManager : MonoBehaviour
{
    public static GamepadVibrationManager Instance;

    private Coroutine vibrationCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 振動を再生する
    /// </summary>
    /// <param name="lowFrequency">低周波モーターの強さ (0~1)</param>
    /// <param name="highFrequency">高周波モーターの強さ (0~1)</param>
    /// <param name="duration">再生時間（秒）</param>
    public void PlayVibration(float lowFrequency, float highFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        if (vibrationCoroutine != null)
        {
            StopCoroutine(vibrationCoroutine);
        }

        vibrationCoroutine = StartCoroutine(VibrationCoroutine(lowFrequency, highFrequency, duration));
    }

    /// <summary>
    /// 振動を即座に停止する
    /// </summary>
    public void StopVibration()
    {
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(0f, 0f);

        if (vibrationCoroutine != null)
        {
            StopCoroutine(vibrationCoroutine);
            vibrationCoroutine = null;
        }
    }

    private IEnumerator VibrationCoroutine(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);

        // Time.timeScale = 0（設定画面などポーズ中）でも正しく時間経過させるため
        // WaitForSeconds ではなく WaitForSecondsRealtime を使う
        yield return new WaitForSecondsRealtime(duration);

        Gamepad.current.SetMotorSpeeds(0f, 0f);
        vibrationCoroutine = null;
    }

    private void OnDisable()
    {
        StopVibration();
    }

    private void OnApplicationQuit()
    {
        StopVibration();
    }
}