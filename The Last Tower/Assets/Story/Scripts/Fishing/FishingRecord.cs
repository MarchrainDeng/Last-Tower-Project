using UnityEngine;

/// <summary>
/// 釣った魚の記録（図鑑）を保存する
/// 種類ごとに釣った数と最大サイズを PlayerPrefs に持つ
/// </summary>
public static class FishingRecord
{
    const string CountPrefix = "Fishing_Count_";
    const string BestPrefix = "Fishing_Best_";

    public static int Count(string id)
    {
        return PlayerPrefs.GetInt(CountPrefix + id, 0);
    }

    public static bool IsCaught(string id)
    {
        return Count(id) > 0;
    }

    public static float BestSize(string id)
    {
        return PlayerPrefs.GetFloat(BestPrefix + id, 0f);
    }

    /// <summary>
    /// 釣った魚を記録する。初めての種類なら isNew、最大記録を更新したら isRecord
    /// </summary>
    public static void Register(string id, float size, out bool isNew, out bool isRecord)
    {
        int count = Count(id);
        float best = BestSize(id);
        isNew = count == 0;
        isRecord = !isNew && size > best;

        PlayerPrefs.SetInt(CountPrefix + id, count + 1);
        if (size > best) PlayerPrefs.SetFloat(BestPrefix + id, size);
        PlayerPrefs.Save();
    }
}
