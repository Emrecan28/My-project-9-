using System;
using UnityEngine;

// GUNLUK MEYDAN OKUMA: her gun herkes icin ayni engel dizilimi ve hedef puan.
// Gunde bir kez odul verir; ust uste gunler seri (streak) olusturur.
public static class DailyChallenge
{
    const string DoneKey = "Daily_Done_";
    const string CountKey = "Daily_Count";
    const string StreakKey = "Daily_Streak";
    const string LastKey = "Daily_Last";

    public static string TodayKey => DateTime.Now.ToString("yyyyMMdd");
    public static int Seed => int.Parse(TodayKey);

    public static bool IsCompletedToday => PlayerPrefs.GetInt(DoneKey + TodayKey, 0) == 1;
    public static int CompletedCount => PlayerPrefs.GetInt(CountKey, 0);

    public static int Streak
    {
        get
        {
            // Dun veya bugun tamamlanmadiysa seri bozulmustur
            string last = PlayerPrefs.GetString(LastKey, "");
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            if (last != TodayKey && last != yesterday) return 0;
            return PlayerPrefs.GetInt(StreakKey, 0);
        }
    }

    // Bugunun hedefi: 1500-2500 arasi, gune gore degisir
    public static int TargetScore => 1500 + (Seed % 5) * 250;

    public static LevelData BuildLevel()
    {
        LevelData data = new LevelData(0, TargetScore);
        data.isDaily = true;
        return data;
    }

    // Tamamlaninca cagrilir; odulu verir. Bugun zaten tamamlandiysa 0 dondurur.
    public static int MarkCompleted()
    {
        if (IsCompletedToday) return 0;
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        int streak = PlayerPrefs.GetString(LastKey, "") == yesterday ? PlayerPrefs.GetInt(StreakKey, 0) + 1 : 1;
        PlayerPrefs.SetInt(DoneKey + TodayKey, 1);
        PlayerPrefs.SetInt(CountKey, CompletedCount + 1);
        PlayerPrefs.SetInt(StreakKey, streak);
        PlayerPrefs.SetString(LastKey, TodayKey);
        Economy.Add(Economy.DailyChallengeReward);
        PlayerPrefs.Save();
        return Economy.DailyChallengeReward;
    }
}
