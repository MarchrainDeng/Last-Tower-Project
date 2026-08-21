using UnityEngine;

/// <summary>
/// タワーHPが低下した際に画面の端を黒く（ビネット）する演出スクリプト
/// HPが0になったら画面全体を真っ黒にする
/// </summary>
public class HPVignetteController : MonoBehaviour
{
    [SerializeField] private TowerHP towerHP;
    [SerializeField] private CanvasGroup vignetteCanvasGroup; // 画面端黒影のCanvasGroup
    [SerializeField] private float dangerThresholdRatio = 0.3f; // HPが何%以下で発動するか（例: 0.3 = 30%）

    private void Update()
    {
        if (towerHP == null || vignetteCanvasGroup == null) return;

        // HP割合の算出
        float hpRatio = (float)towerHP.currentHP / towerHP.maxHP;

        if (hpRatio <= 0f)
        {
            // HP0：画面全体を真っ黒にする
            vignetteCanvasGroup.alpha = Mathf.Lerp(vignetteCanvasGroup.alpha, 1f, Time.deltaTime * 3f);
        }
        else if (hpRatio <= dangerThresholdRatio)
        {
            // 体力が赤（危険域）になったら画面端を徐々に黒くする
            // HPが減るほど黒みが強くなる計算
            float targetAlpha = Mathf.Lerp(0.8f, 0.2f, hpRatio / dangerThresholdRatio);
            vignetteCanvasGroup.alpha = Mathf.Lerp(vignetteCanvasGroup.alpha, targetAlpha, Time.deltaTime * 3f);
        }
        else
        {
            vignetteCanvasGroup.alpha = Mathf.Lerp(vignetteCanvasGroup.alpha, 0f, Time.deltaTime * 5f);
        }
    }
}