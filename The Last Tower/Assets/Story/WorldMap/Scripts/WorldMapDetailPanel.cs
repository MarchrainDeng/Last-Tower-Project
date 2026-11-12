using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 台座で決定したときに出すステージの詳細
/// ステージ番号・名前／クリア状況／主な報酬／クリアタイム／星評価（最大3個）
/// </summary>
public class WorldMapDetailPanel : MonoBehaviour
{
    public TMP_Text titleLabel;
    public TMP_Text statusLabel;
    public TMP_Text rewardLabel;
    public TMP_Text timeLabel;
    public Image[] stars;
    public TMP_Text hintLabel;

    public Color starOnColor = new Color(1f, 0.82f, 0.25f, 1f);
    public Color starOffColor = new Color(1f, 1f, 1f, 0.18f);
    public Color clearedColor = new Color(0.55f, 0.95f, 0.6f, 1f);
    public Color notClearedColor = new Color(0.85f, 0.85f, 0.9f, 1f);

    public bool IsOpen => gameObject.activeSelf;

    WorldMapStageData current;

    public void Show(WorldMapStageData stage)
    {
        current = stage;
        var record = WorldMapProgress.GetRecord(stage.stageId);
        bool cleared = record != null && record.cleared;

        if (titleLabel != null) titleLabel.text = stage.stageNumber + "  " + stage.stageName;

        if (statusLabel != null)
        {
            statusLabel.text = cleared ? "クリア済み" : "未クリア";
            statusLabel.color = cleared ? clearedColor : notClearedColor;
        }

        if (rewardLabel != null)
            rewardLabel.text = stage.rewards != null && stage.rewards.Length > 0 ? string.Join("\n", stage.rewards) : "なし";

        if (timeLabel != null)
            timeLabel.text = record != null && record.bestTime >= 0f ? FormatTime(record.bestTime) : "--:--.--";

        int starCount = record != null ? record.stars : 0;
        if (stars != null)
            for (int i = 0; i < stars.Length; i++)
                if (stars[i] != null) stars[i].color = i < starCount ? starOnColor : starOffColor;

        SetHint(false);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// ステージのシーンがまだ無いときは「準備中」を出す
    /// </summary>
    public void SetHint(bool notReady)
    {
        if (hintLabel == null) return;
        bool playable = current != null && !string.IsNullOrEmpty(current.sceneName);
        hintLabel.text = playable
            ? "決定：ステージ開始　　戻る：閉じる"
            : (notReady ? "<color=#FF9A7A>このステージは準備中です</color>　　戻る：閉じる" : "準備中　　戻る：閉じる");
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60;
        return m.ToString("00") + ":" + s.ToString("00.00");
    }
}

