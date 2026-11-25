using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ワールドで決定したときに出す詳細
/// ワールド番号・名前／クリア状況／説明／ステージの進み具合（未解放なら解放条件）
/// </summary>
public class WorldSelectDetailPanel : MonoBehaviour
{
    public TMP_Text numberLabel;
    public TMP_Text titleLabel;
    public TMP_Text statusLabel;
    public TMP_Text descriptionLabel;
    public TMP_Text progressLabel;
    public TMP_Text hintLabel;
    [Tooltip("この中をクリックすると決定、外をクリックすると閉じる")]
    public RectTransform clickArea;

    public Color clearedColor = new Color(0.20f, 0.40f, 0.22f, 1f);
    public Color notClearedColor = new Color(0.30f, 0.29f, 0.28f, 1f);
    public Color lockedColor = new Color(0.55f, 0.25f, 0.20f, 1f);

    public bool IsOpen => gameObject.activeSelf;

    WorldData current;

    public void Show(WorldData world)
    {
        current = world;
        bool unlocked = WorldMapProgress.IsWorldUnlocked(world);
        bool cleared = WorldMapProgress.IsWorldCleared(world);

        if (numberLabel != null) numberLabel.text = world.NumberLabel;
        if (titleLabel != null) titleLabel.text = world.worldName;

        if (statusLabel != null)
        {
            statusLabel.text = !unlocked ? "未解放" : cleared ? "クリア済み" : "未クリア";
            statusLabel.color = !unlocked ? lockedColor : cleared ? clearedColor : notClearedColor;
        }

        if (descriptionLabel != null) descriptionLabel.text = world.description;

        if (progressLabel != null)
        {
            if (!unlocked) progressLabel.text = UnlockCondition(world);
            else if (world.stages != null && world.stages.Length > 0)
                progressLabel.text = "ステージ　" + WorldMapProgress.ClearedStageCount(world) + " / " + world.stages.Length + "　クリア";
            else progressLabel.text = "ステージ　準備中";
        }

        SetHint(false);
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 入れないとき（未解放・準備中）に決定したら、理由を目立たせる
    /// </summary>
    public void SetHint(bool denied)
    {
        if (hintLabel == null || current == null) return;
        bool unlocked = WorldMapProgress.IsWorldUnlocked(current);
        bool ready = !string.IsNullOrEmpty(current.stageMapScene);

        if (unlocked && ready)
        {
            hintLabel.text = "決定：出発　　戻る：閉じる";
            return;
        }
        string reason = !unlocked ? "まだ解放されていません" : "このワールドは準備中です";
        hintLabel.text = (denied ? "<color=#A0402F>" + reason + "</color>" : reason) + "　　戻る：閉じる";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public bool IsPointerOverFrame()
    {
        return MenuInput.IsPointerOver(clickArea);
    }

    static string UnlockCondition(WorldData world)
    {
        if (world.unlockAfter == null || world.unlockAfter.Length == 0) return "解放条件：未設定";
        var names = new List<string>();
        foreach (var w in world.unlockAfter)
            if (w != null) names.Add(w.worldNumber.ToString());
        string list = "WORLD " + string.Join("・", names);
        return names.Count > 1 ? list + " をすべてクリアすると解放" : list + " をクリアすると解放";
    }
}
