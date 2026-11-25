using UnityEngine;

// iOS Game Center skor tablosu (leaderboard).
// App Store Connect > NEOBLOCK > Game Center'da "neoblock_best" kimlikli bir skor tablosu olmali
// ve Xcode'da Game Center yetenegi (capability) acik olmali.
// Oyuncu Game Center'a girmemisse hicbir sey bozulmaz: sadece bilgilendirme gosterilir.
public static class GameCenter
{
    public const string LeaderboardId = "neoblock_best";
#pragma warning disable 414
    static bool authenticating;
#pragma warning restore 414

#pragma warning disable 618, 612 // Unity'nin Social API'si "eski" olarak isaretli ama Unity 6'da calisiyor

    public static bool IsSignedIn
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return Social.localUser != null && Social.localUser.authenticated;
#else
            return false;
#endif
        }
    }

    public static void SignIn(System.Action<bool> done = null)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (IsSignedIn) { done?.Invoke(true); return; }
        if (authenticating) { done?.Invoke(false); return; }
        authenticating = true;
        Social.localUser.Authenticate(success =>
        {
            authenticating = false;
            if (success) ReportBestScore(GameStats.BestScore);
            done?.Invoke(success);
        });
#else
        done?.Invoke(false);
#endif
    }

    public static void ReportBestScore(int score)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!IsSignedIn || score <= 0) return;
        Social.ReportScore(score, LeaderboardId, _ => { });
#endif
    }

    public static void ShowLeaderboard()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (IsSignedIn)
        {
            ReportBestScore(GameStats.BestScore);
            Social.ShowLeaderboardUI();
            return;
        }
        SignIn(ok =>
        {
            if (ok) Social.ShowLeaderboardUI();
            else UIKit.Toast("GAME CENTER", "SIGN IN TO GAME CENTER IN SETTINGS", Theme.Neutral);
        });
#else
        UIKit.Toast("LEADERBOARD", "AVAILABLE ON IPHONE (GAME CENTER)", Theme.Neutral);
#endif
    }

#pragma warning restore 618, 612
}
