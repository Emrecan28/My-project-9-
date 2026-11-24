using System;
using UnityEngine;

// Oyun icindeki olaylari tek yerden toplar: istatistik, gorevler ve basarimlar buradan beslenir.
public static class Progress
{
    public static void OnPlacement()
    {
        GameStats.AddPlacement();
        Missions.Add(MissionType.Place, 1);
    }

    public static void OnLinesCleared(int lines)
    {
        GameStats.AddLines(lines);
        Missions.Add(MissionType.Lines, lines);
        Achievements.Check();
    }

    public static void OnCombo(int combo)
    {
        GameStats.ReportCombo(combo);
        Missions.ReportMax(MissionType.Combo, combo);
        Achievements.Check();
    }

    public static void OnScore(int score)
    {
        Missions.ReportMax(MissionType.Score, score);
    }

    public static void OnAllClear()
    {
        GameStats.AddAllClear();
        Achievements.Check();
    }

    public static void OnClassicGameEnd(int score)
    {
        GameStats.RecordGame(score);
        Missions.Add(MissionType.Games, 1);
        Missions.ReportMax(MissionType.Score, score);
        Achievements.Check();
    }

    public static void OnLevelComplete()
    {
        Missions.Add(MissionType.Games, 1);
        Achievements.Check();
    }

    public static void OnDailyComplete()
    {
        Missions.Add(MissionType.Games, 1);
        Achievements.Check();
    }

    public static void OnPowerUpUsed(bool bomb)
    {
        if (bomb) GameStats.AddBomb(); else GameStats.AddRotate();
        Missions.Add(MissionType.PowerUp, 1);
        Achievements.Check();
    }
}

// ======================= GUNLUK GOREVLER =======================
public enum MissionType { Lines, Games, Combo, Place, Score, PowerUp }

public static class Missions
{
    public struct Template
    {
        public MissionType type;
        public int target;
        public int reward;
        public string text;
        public Template(MissionType type, int target, int reward, string text)
        {
            this.type = type; this.target = target; this.reward = reward; this.text = text;
        }
    }

    static readonly Template[] Pool =
    {
        new Template(MissionType.Lines, 20, 30, "CLEAR 20 LINES"),
        new Template(MissionType.Lines, 40, 50, "CLEAR 40 LINES"),
        new Template(MissionType.Games, 3, 30, "PLAY 3 GAMES"),
        new Template(MissionType.Games, 5, 50, "PLAY 5 GAMES"),
        new Template(MissionType.Combo, 3, 30, "REACH COMBO 3"),
        new Template(MissionType.Combo, 5, 60, "REACH COMBO 5"),
        new Template(MissionType.Place, 60, 30, "PLACE 60 BLOCKS"),
        new Template(MissionType.Place, 120, 50, "PLACE 120 BLOCKS"),
        new Template(MissionType.Score, 1500, 40, "SCORE 1500 IN A GAME"),
        new Template(MissionType.Score, 3000, 60, "SCORE 3000 IN A GAME"),
        new Template(MissionType.PowerUp, 1, 25, "USE A POWER-UP"),
    };

    public const int Count = 3;

    // Bugunun 3 gorevi (tarihten tohumlanir, ayni tipte iki gorev gelmez)
    public static Template[] Today
    {
        get
        {
            System.Random rng = new System.Random(DailyChallenge.Seed * 31 + 7);
            Template[] result = new Template[Count];
            bool[] usedType = new bool[Enum.GetValues(typeof(MissionType)).Length];
            int filled = 0, guard = 0;
            while (filled < Count && guard++ < 200)
            {
                Template t = Pool[rng.Next(Pool.Length)];
                if (usedType[(int)t.type]) continue;
                usedType[(int)t.type] = true;
                result[filled++] = t;
            }
            return result;
        }
    }

    static string Key(int i, string suffix) => "Mis_" + DailyChallenge.TodayKey + "_" + i + "_" + suffix;

    public static int GetProgress(int i) => PlayerPrefs.GetInt(Key(i, "p"), 0);
    public static bool IsClaimed(int i) => PlayerPrefs.GetInt(Key(i, "c"), 0) == 1;
    public static bool IsComplete(int i) => GetProgress(i) >= Today[i].target;

    public static int UnclaimedCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Count; i++) if (IsComplete(i) && !IsClaimed(i)) n++;
            return n;
        }
    }

    public static void Add(MissionType type, int amount)
    {
        Template[] today = Today;
        for (int i = 0; i < Count; i++)
        {
            if (today[i].type != type) continue;
            int before = GetProgress(i);
            int after = before + amount;
            PlayerPrefs.SetInt(Key(i, "p"), after);
            if (before < today[i].target && after >= today[i].target) AnnounceComplete(today[i]);
        }
    }

    public static void ReportMax(MissionType type, int value)
    {
        Template[] today = Today;
        for (int i = 0; i < Count; i++)
        {
            if (today[i].type != type) continue;
            int before = GetProgress(i);
            if (value <= before) continue;
            PlayerPrefs.SetInt(Key(i, "p"), value);
            if (before < today[i].target && value >= today[i].target) AnnounceComplete(today[i]);
        }
    }

    static void AnnounceComplete(Template t)
    {
        UIKit.Toast("MISSION COMPLETE!", t.text, Theme.Positive);
    }

    public static bool Claim(int i)
    {
        if (!IsComplete(i) || IsClaimed(i)) return false;
        PlayerPrefs.SetInt(Key(i, "c"), 1);
        Economy.Add(Today[i].reward);
        return true;
    }
}

// ======================= BASARIMLAR =======================
public static class Achievements
{
    public struct Def
    {
        public string id, title, desc;
        public int target, reward;
        public Func<int> value;
        public Def(string id, string title, string desc, int target, int reward, Func<int> value)
        {
            this.id = id; this.title = title; this.desc = desc; this.target = target; this.reward = reward; this.value = value;
        }
    }

    public static readonly Def[] All =
    {
        new Def("first_game", "FIRST STEPS", "PLAY YOUR FIRST GAME", 1, 20, () => GameStats.GamesPlayed),
        new Def("games_25", "REGULAR", "PLAY 25 GAMES", 25, 80, () => GameStats.GamesPlayed),
        new Def("lines_50", "LINE ROOKIE", "CLEAR 50 LINES", 50, 40, () => GameStats.LinesCleared),
        new Def("lines_500", "LINE MASTER", "CLEAR 500 LINES", 500, 150, () => GameStats.LinesCleared),
        new Def("combo_3", "COMBO STARTER", "REACH COMBO 3", 3, 30, () => GameStats.BestCombo),
        new Def("combo_6", "COMBO KING", "REACH COMBO 6", 6, 100, () => GameStats.BestCombo),
        new Def("score_1k", "1K CLUB", "SCORE 1000", 1000, 30, () => GameStats.BestScore),
        new Def("score_5k", "HIGH ROLLER", "SCORE 5000", 5000, 100, () => GameStats.BestScore),
        new Def("score_10k", "LEGEND", "SCORE 10000", 10000, 250, () => GameStats.BestScore),
        new Def("allclear", "SPOTLESS", "GET AN ALL CLEAR", 1, 80, () => GameStats.AllClears),
        new Def("level_10", "ADVENTURER", "REACH LEVEL 10", 10, 60, () => PlayerPrefs.GetInt("UnlockedLevel", 1)),
        new Def("level_30", "EXPLORER", "REACH LEVEL 30", 30, 150, () => PlayerPrefs.GetInt("UnlockedLevel", 1)),
        new Def("daily_1", "DAILY PLAYER", "COMPLETE A DAILY CHALLENGE", 1, 40, () => DailyChallenge.CompletedCount),
        new Def("daily_7", "DEDICATED", "COMPLETE 7 DAILY CHALLENGES", 7, 150, () => DailyChallenge.CompletedCount),
        new Def("placed_1000", "BUILDER", "PLACE 1000 BLOCKS", 1000, 100, () => GameStats.Placements),
        new Def("bomber", "DEMOLITION", "USE 5 BOMBS", 5, 50, () => GameStats.BombsUsed),
    };

    public static bool IsUnlocked(int i) => PlayerPrefs.GetInt("Ach_" + All[i].id, 0) == 1;

    public static int UnlockedCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (IsUnlocked(i)) n++;
            return n;
        }
    }

    // Hedefe ulasilan basarimlari acar, parayi verir ve bildirim gosterir.
    // Ayni anda cok sayida acilirsa (ornegin guncellemeden once oynamis oyuncu) tek bildirimde toplar.
    public static void Check()
    {
        int count = 0, coins = 0;
        string lastTitle = null, lastSub = null;
        for (int i = 0; i < All.Length; i++)
        {
            if (IsUnlocked(i)) continue;
            int v = All[i].value();
            if (v < All[i].target) continue;
            PlayerPrefs.SetInt("Ach_" + All[i].id, 1);
            Economy.Add(All[i].reward);
            count++;
            coins += All[i].reward;
            lastTitle = All[i].title;
            lastSub = "ACHIEVEMENT  +" + All[i].reward + " COINS";
        }
        if (count == 1) UIKit.Toast(lastTitle, lastSub, Theme.Highlight);
        else if (count > 1) UIKit.Toast(count + " ACHIEVEMENTS!", "+" + coins + " COINS", Theme.Highlight);
    }
}
