using UnityEngine;

// Kalici oyuncu istatistikleri (menudeki STATS ekrani ve Game Over sonuc ekrani icin).
// Sadece cihazda (PlayerPrefs) tutulur; hicbir kisisel veri toplanmaz/gonderilmez.
public static class GameStats
{
    const string GamesKey = "Stats_GamesPlayed";
    const string TotalScoreKey = "Stats_TotalScore";
    const string LinesKey = "Stats_LinesCleared";
    const string BestComboKey = "Stats_BestCombo";
    const string AllClearsKey = "Stats_AllClears";

    public static int GamesPlayed => PlayerPrefs.GetInt(GamesKey, 0);
    public static int LinesCleared => PlayerPrefs.GetInt(LinesKey, 0);
    public static int BestCombo => PlayerPrefs.GetInt(BestComboKey, 0);
    public static int AllClears => PlayerPrefs.GetInt(AllClearsKey, 0);
    public static int BestScore => PlayerPrefs.GetInt("HighScore", 0); // ScoreManager ile ayni anahtar

    public static int AverageScore
    {
        get
        {
            int games = GamesPlayed;
            if (games <= 0) return 0;
            // Toplam skor string olarak tutulur (int tasmasin)
            long total = 0;
            long.TryParse(PlayerPrefs.GetString(TotalScoreKey, "0"), out total);
            return (int)(total / games);
        }
    }

    public static int Placements => PlayerPrefs.GetInt("Stats_Placements", 0);
    public static int BombsUsed => PlayerPrefs.GetInt("Stats_Bombs", 0);
    public static int RotatesUsed => PlayerPrefs.GetInt("Stats_Rotates", 0);

    public static void AddPlacement() => PlayerPrefs.SetInt("Stats_Placements", Placements + 1);
    public static void AddBomb() => PlayerPrefs.SetInt("Stats_Bombs", BombsUsed + 1);
    public static void AddRotate() => PlayerPrefs.SetInt("Stats_Rotates", RotatesUsed + 1);

    public static void AddLines(int lines)
    {
        if (lines <= 0) return;
        PlayerPrefs.SetInt(LinesKey, LinesCleared + lines);
    }

    public static void ReportCombo(int combo)
    {
        if (combo > BestCombo) PlayerPrefs.SetInt(BestComboKey, combo);
    }

    public static void AddAllClear()
    {
        PlayerPrefs.SetInt(AllClearsKey, AllClears + 1);
    }

    // Klasik (sonsuz) oyun bitince cagrilir
    public static void RecordGame(int finalScore)
    {
        PlayerPrefs.SetInt(GamesKey, GamesPlayed + 1);
        long total = 0;
        long.TryParse(PlayerPrefs.GetString(TotalScoreKey, "0"), out total);
        total += Mathf.Max(0, finalScore);
        PlayerPrefs.SetString(TotalScoreKey, total.ToString());
        PlayerPrefs.Save();
    }
}
