using UnityEngine;

public class TestVibration : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GamepadVibrationManager.Instance?.PlayVibration(0.3f, 0.8f, 0.15f);
    }

    // Update is called once per frame
    void Update()
    {
        GamepadVibrationManager.Instance?.PlayVibration(0.5f, 0.9f, 0.15f);
    }
}
