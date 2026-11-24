using System;
using UnityEngine;

// NEON COIN ekonomisi: kazanma, harcama, gunluk giris odulu, blok temalari.
// Her sey cihazda (PlayerPrefs) tutulur. Gercek para ile satin alma YOK (yasal yuk yok).
public static class Economy
{
    const string CoinsKey = "Eco_Coins";
    const string WelcomeKey = "Eco_Welcome";
    const string LastLoginKey = "Eco_LastLogin";
    const string LoginStreakKey = "Eco_LoginStreak";
    const string AdCoinsDayKey = "Eco_AdCoinsDay";
    const string AdCoinsCountKey = "Eco_AdCoinsCount";

    // Fiyatlar / oduller (tek yerden ayarlanir)
    public const int WelcomeBonus = 100;
    public const int BombPrice = 60;
    public const int RotatePrice = 30;
    public const int ThemePrice = 250;
    public const int DailyChallengeReward = 100;
    public const int AdCoinReward = 40;
    public const int AdCoinsPerDay = 5;

    public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(CoinsKey, Coins + amount);
        PlayerPrefs.Save();
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        PlayerPrefs.SetInt(CoinsKey, Coins - amount);
        PlayerPrefs.Save();
        return true;
    }

    // Klasik oyun sonu: her 40 puana 1 para (en az 1)
    public static int CoinsForScore(int score) => score <= 0 ? 0 : Mathf.Max(1, score / 40);

    // Level bitince: 15 + level numarasi
    public static int CoinsForLevel(int level) => 15 + Mathf.Max(0, level);

    // ---------------- GUNLUK GIRIS ----------------
    public static string TodayKey => DateTime.Now.ToString("yyyyMMdd");

    // Ana menu acilinca cagrilir. Bugun ilk girisse odul verir ve miktari dondurur (yoksa 0).
    public static int ClaimDailyLogin(out int streak, out bool welcome)
    {
        welcome = false;
        int gained = 0;
        if (PlayerPrefs.GetInt(WelcomeKey, 0) == 0)
        {
            PlayerPrefs.SetInt(WelcomeKey, 1);
            gained += WelcomeBonus;
            welcome = true;
        }

        string today = TodayKey;
        string last = PlayerPrefs.GetString(LastLoginKey, "");
        streak = PlayerPrefs.GetInt(LoginStreakKey, 0);
        if (last != today)
        {
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            streak = last == yesterday ? streak + 1 : 1;
            PlayerPrefs.SetInt(LoginStreakKey, streak);
            PlayerPrefs.SetString(LastLoginKey, today);
            // 1. gun 20, her ardisik gun +5, en fazla 50
            gained += Mathf.Min(50, 20 + (streak - 1) * 5);
        }

        if (gained > 0) Add(gained);
        return gained;
    }

    // ---------------- REKLAMLA PARA ----------------
    public static int AdCoinsLeftToday
    {
        get
        {
            if (PlayerPrefs.GetString(AdCoinsDayKey, "") != TodayKey) return AdCoinsPerDay;
            return Mathf.Max(0, AdCoinsPerDay - PlayerPrefs.GetInt(AdCoinsCountKey, 0));
        }
    }

    public static void WatchAdForCoins(Action<bool> done)
    {
        if (AdCoinsLeftToday <= 0 || AdsManager.Instance == null || !AdsManager.Instance.IsRewardedReady())
        {
            done?.Invoke(false);
            return;
        }
        AdsManager.Instance.ShowCoinsRewarded(success =>
        {
            if (success)
            {
                if (PlayerPrefs.GetString(AdCoinsDayKey, "") != TodayKey)
                {
                    PlayerPrefs.SetString(AdCoinsDayKey, TodayKey);
                    PlayerPrefs.SetInt(AdCoinsCountKey, 0);
                }
                PlayerPrefs.SetInt(AdCoinsCountKey, PlayerPrefs.GetInt(AdCoinsCountKey, 0) + 1);
                Add(AdCoinReward);
            }
            done?.Invoke(success);
        });
    }

    // ---------------- BLOK TEMALARI ----------------
    const string ThemeSelectedKey = "Eco_Theme";

    public static int SelectedTheme
    {
        get
        {
            int t = PlayerPrefs.GetInt(ThemeSelectedKey, 0);
            return (t >= 0 && t < BlockSkin.Themes.Length && IsThemeOwned(t)) ? t : 0;
        }
        set { PlayerPrefs.SetInt(ThemeSelectedKey, value); PlayerPrefs.Save(); }
    }

    public static bool IsThemeOwned(int index) => index == 0 || PlayerPrefs.GetInt("Eco_ThemeOwned_" + index, 0) == 1;

    public static bool TryBuyTheme(int index)
    {
        if (IsThemeOwned(index)) return true;
        if (!TrySpend(ThemePrice)) return false;
        PlayerPrefs.SetInt("Eco_ThemeOwned_" + index, 1);
        PlayerPrefs.Save();
        return true;
    }
}

// Oyuncu ayarlari (ses/titresim disindakiler)
public static class GameSettings
{
    const string EffectsKey = "Set_Effects";

    // Kapaliysa parcacik sayisi azalir, kamera sallanmaz (eski telefonlar / sade oynamak isteyenler)
    public static bool EffectsOn
    {
        get => PlayerPrefs.GetInt(EffectsKey, 1) == 1;
        set { PlayerPrefs.SetInt(EffectsKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }
}
