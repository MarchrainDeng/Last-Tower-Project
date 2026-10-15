using UnityEngine;

/// <summary>
/// マイタウンの場所の解放状況を保存する
/// ストーリー進行側から MyTownProgress.Unlock("factory") のように呼ぶ
/// </summary>
public static class MyTownProgress
{
    const string KeyPrefix = "MyTown_Unlocked_";

    public static bool IsUnlocked(string locationId, bool unlockedByDefault)
    {
        return PlayerPrefs.GetInt(KeyPrefix + locationId, unlockedByDefault ? 1 : 0) == 1;
    }

    public static void Unlock(string locationId)
    {
        PlayerPrefs.SetInt(KeyPrefix + locationId, 1);
        PlayerPrefs.Save();
    }
}
