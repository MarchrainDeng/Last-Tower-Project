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
    /// 手柄震动
    /// ゲームパッドを振動させる
    /// </summary>
    /// <param name="lowFrequency">低频马达(0~1)</param>
    /// <param name="highFrequency">高频马达(0~1)</param>
    /// <param name="duration">持续时间(秒)</param>
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
    /// 停止震动
    /// 振動を停止する
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

        yield return new WaitForSeconds(duration);

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