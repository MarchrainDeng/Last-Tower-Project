using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    private void Awake()
    {
        // 禁用垂直同步
        // 垂直同期（VSync）を無効にする
        QualitySettings.vSyncCount = 0;

        // 将目标帧率设置为 60 FPS
        // 目標フレームレートを60FPSに設定する
        Application.targetFrameRate = 60;
    }
}
