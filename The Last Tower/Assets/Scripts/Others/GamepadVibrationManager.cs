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
    /// ﾊﾖｱ晥ｯ
    /// ･ｲｩ`･爭ﾑ･ﾃ･ﾉ､ﾓ､ｵ､ｻ､・
    /// </summary>
    /// <param name="lowFrequency">ｵﾍﾆｵﾂ昻・0~1)</param>
    /// <param name="highFrequency">ｸﾟﾆｵﾂ昻・0~1)</param>
    /// <param name="duration">ｳﾖﾐｱｼ・ﾃ・</param>
    public void PlayVibration(float lowFrequency, float highFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        if (vibrationCoroutine != null)
        {
            StopCoroutine(vibrationCoroutine);
        }

        Debug.Log("vibration start");

        vibrationCoroutine = StartCoroutine(VibrationCoroutine(lowFrequency, highFrequency, duration));
    }

    /// <summary>
    /// ﾍ｣ﾖｹﾕｯ
    /// ﾕﾓ､｣ﾖｹ､ｹ､・
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