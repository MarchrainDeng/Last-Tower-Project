using System.Collections;
using UnityEngine;

/// <summary>
/// ステージに入る台座（廃墟になった平らな台座）
/// - 未解放：暗く、錠前を表示。近づくと看板は出るが光らず、入れない
/// - 解放済み：近づくと光り、看板に番号・名前・クリア状況を出す
/// - クリア済み：台座の上に簡易タワーが建つ（初クリアで戻ってきたときは生えてくる演出）
/// </summary>
public class WorldMapPedestal : WorldMapSpot
{
    [Header("── ステージ ────────────────────")]
    public WorldMapStageData stage;

    [Header("── 状態ごとの見た目 ─────────────────")]
    [Tooltip("未解放のとき暗くする絵")]
    public SpriteRenderer[] baseRenderers;
    public Color lockedTint = new Color(0.45f, 0.45f, 0.5f, 1f);
    public GameObject lockIcon;
    [Tooltip("クリア後に台座の上に出る簡易タワー")]
    public Transform tower;
    [Tooltip("クリア済みの印（台座の前のチェック板）")]
    public GameObject clearedMark;
    public float towerRiseDuration = 0.9f;

    Color[] baseColors;
    Vector3 towerScale = Vector3.one;
    bool riseScheduled;

    public override string SpotId => stage != null ? stage.stageId : name;
    public override bool CanEnter => WorldMapProgress.IsUnlocked(stage);

    protected override void Awake()
    {
        base.Awake();
        if (tower != null) towerScale = tower.localScale;
        if (baseRenderers != null)
        {
            baseColors = new Color[baseRenderers.Length];
            for (int i = 0; i < baseRenderers.Length; i++)
                if (baseRenderers[i] != null) baseColors[i] = baseRenderers[i].color;
        }
    }

    public override void Refresh()
    {
        base.Refresh();

        bool unlocked = CanEnter;
        bool cleared = WorldMapProgress.IsCleared(stage);

        if (baseRenderers != null && baseColors != null)
        {
            for (int i = 0; i < baseRenderers.Length; i++)
                if (baseRenderers[i] != null)
                    baseRenderers[i].color = unlocked ? baseColors[i] : baseColors[i] * lockedTint;
        }

        if (lockIcon != null) lockIcon.SetActive(!unlocked);
        if (clearedMark != null) clearedMark.SetActive(cleared);
        if (tower != null && !riseScheduled)
        {
            tower.gameObject.SetActive(cleared);
            tower.localScale = towerScale;
        }
    }

    protected override string BuildSignText()
    {
        if (stage == null) return "";
        if (!CanEnter) return stage.stageNumber + " ？？？\n<size=80%>未解放</size>";

        string status = WorldMapProgress.IsCleared(stage) ? "クリア済み" : "未クリア";
        return stage.stageNumber + " " + stage.stageName + "\n<size=80%>" + status + "\n決定：詳細</size>";
    }

    /// <summary>
    /// 初クリアで戻ってきたとき、タワーが下から生えてくる
    /// </summary>
    public void PlayTowerRise()
    {
        if (tower == null) return;
        riseScheduled = true;
        StartCoroutine(RiseRoutine());
    }

    IEnumerator RiseRoutine()
    {
        tower.gameObject.SetActive(true);
        tower.localScale = new Vector3(towerScale.x, 0f, towerScale.z);
        // 画面が明るくなるのを少し待つ
        yield return new WaitForSeconds(0.6f);

        float t = 0f;
        while (t < towerRiseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / towerRiseDuration);
            // 少し伸びすぎてから戻る
            float y = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
            tower.localScale = new Vector3(towerScale.x, towerScale.y * y, towerScale.z);
            yield return null;
        }
        tower.localScale = towerScale;
        riseScheduled = false;
    }
}
