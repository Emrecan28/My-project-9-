using System;
using UnityEngine;
using UnityEngine.UI;

// Ana menudeki pencereler: ISTATISTIK, AYARLAR, MAGAZA, GOREVLER, BASARIMLAR, GUNLUK ODUL.
// Hepsi UIKit ile kodla kurulur; sahnede bir sey degistirmek gerekmez.
public static class MenuPanels
{
    static void Close(GameObject root, Action onClose)
    {
        PlayerPrefs.Save();
        UnityEngine.Object.Destroy(root);
        onClose?.Invoke();
    }

    // ---------- ISTATISTIK ----------
    public static void ShowStats(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("StatsWindow", "STATS", new Vector2(880f, 1130f), out GameObject root);

        string[] labels = { "BEST SCORE", "GAMES PLAYED", "AVERAGE SCORE", "LINES CLEARED", "BEST COMBO", "ALL CLEARS", "BLOCKS PLACED" };
        string[] values =
        {
            GameStats.BestScore.ToString(), GameStats.GamesPlayed.ToString(), GameStats.AverageScore.ToString(),
            GameStats.LinesCleared.ToString(), GameStats.BestCombo.ToString(), GameStats.AllClears.ToString(),
            GameStats.Placements.ToString()
        };
        Color[] colors = { Theme.Highlight, Theme.Text, Theme.Text, Theme.Text, Theme.Primary, Theme.Positive, Theme.Text };

        float y = 370f;
        for (int i = 0; i < labels.Length; i++)
        {
            Row(w, y, 100f, labels[i], values[i], colors[i]);
            y -= 114f;
        }

        Button close = UIKit.TextButton(w, "CLOSE", Theme.Primary, new Vector2(0f, -470f), new Vector2(420f, 110f), 48);
        close.onClick.AddListener(() => Close(root, onClose));
    }

    static void Row(Transform parent, float y, float height, string label, string value, Color valueColor)
    {
        Image bg = UIKit.Panel(parent, "Row_" + label, new Vector2(760f, height), Theme.SurfaceSoft);
        bg.raycastTarget = false;
        bg.rectTransform.anchoredPosition = new Vector2(0f, y);

        Text l = UIKit.Label(bg.transform, label, 38, Theme.TextMuted, TextAnchor.MiddleLeft);
        l.rectTransform.sizeDelta = new Vector2(700f, height);

        Text v = UIKit.Label(bg.transform, value, 52, valueColor, TextAnchor.MiddleRight);
        v.rectTransform.sizeDelta = new Vector2(700f, height);
    }

    // ---------- AYARLAR ----------
    public static void ShowSettings(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("SettingsWindow", "SETTINGS", new Vector2(880f, 940f), out GameObject root);

        ToggleRow(w, 230f, "MUSIC",
            () => SoundManager.Instance == null || SoundManager.Instance.IsMusicOn,
            on => { if (SoundManager.Instance != null) SoundManager.Instance.SetMusic(on); });

        ToggleRow(w, 85f, "SOUND FX",
            () => SoundManager.Instance == null || SoundManager.Instance.IsSfxOn,
            on => { if (SoundManager.Instance != null) SoundManager.Instance.SetSfx(on); });

        ToggleRow(w, -60f, "VIBRATION",
            () => Haptics.IsOn,
            on => { Haptics.IsOn = on; if (on) Haptics.Medium(); });

        ToggleRow(w, -205f, "EFFECTS",
            () => GameSettings.EffectsOn,
            on => GameSettings.EffectsOn = on);

        Button done = UIKit.TextButton(w, "DONE", Theme.Primary, new Vector2(0f, -370f), new Vector2(420f, 110f), 50);
        done.onClick.AddListener(() => Close(root, onClose));
    }

    static void ToggleRow(Transform parent, float y, string label, Func<bool> getState, Action<bool> setState)
    {
        Image bg = UIKit.Panel(parent, "Row_" + label, new Vector2(760f, 120f), Theme.SurfaceSoft);
        bg.raycastTarget = false;
        bg.rectTransform.anchoredPosition = new Vector2(0f, y);

        Text l = UIKit.Label(bg.transform, label, 44, Theme.Text, TextAnchor.MiddleLeft);
        l.rectTransform.sizeDelta = new Vector2(700f, 110f);

        Button pill = UIKit.TextButton(bg.transform, "ON", Theme.Positive, new Vector2(250f, 0f), new Vector2(190f, 86f), 42);
        Image pillImage = pill.GetComponent<Image>();
        Shadow pillShadow = pill.GetComponent<Shadow>();
        Text pillText = pill.GetComponentInChildren<Text>();

        Action refresh = () =>
        {
            bool on = getState();
            Color c = on ? Theme.Positive : Theme.Danger;
            pillImage.color = c;
            if (pillShadow != null) pillShadow.effectColor = Theme.Darken(c, 0.55f);
            pillText.text = on ? "ON" : "OFF";
        };
        refresh();
        pill.onClick.AddListener(() => { setState(!getState()); refresh(); });
    }

    // ---------- MAGAZA (blok temalari + reklamla para) ----------
    public static void ShowShop(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("ShopWindow", "SHOP", new Vector2(900f, 1400f), out GameObject root);
        UIKit.CloseButton(w, root).onClick.AddListener(() => onClose?.Invoke());
        UIKit.CoinBadge(w, new Vector2(0f, 500f), new Vector2(300f, 84f));

        Text sub = UIKit.Label(w, "BLOCK THEMES", 40, Theme.TextMuted, TextAnchor.MiddleCenter);
        sub.rectTransform.anchoredPosition = new Vector2(0f, 420f);

        for (int i = 0; i < BlockSkin.Themes.Length; i++)
        {
            int col = i % 2, row = i / 2;
            Vector2 pos = new Vector2(col == 0 ? -205f : 205f, 260f - row * 280f);
            ThemeCard(w, i, pos);
        }

        // Reklam izleyip para kazan (gunluk sinirli)
        Button ad = UIKit.TextButton(w, "", Theme.Positive, new Vector2(0f, -590f), new Vector2(720f, 120f), 44);
        Text adText = ad.GetComponentInChildren<Text>();
        Action refreshAd = () =>
        {
            int left = Economy.AdCoinsLeftToday;
            adText.text = left > 0 ? "WATCH AD  +" + Economy.AdCoinReward + " COINS  (" + left + ")" : "COME BACK TOMORROW";
            ad.interactable = left > 0;
        };
        refreshAd();
        ad.onClick.AddListener(() =>
        {
            Economy.WatchAdForCoins(ok =>
            {
                if (ad == null) return;
                if (ok) UIKit.Toast("+" + Economy.AdCoinReward + " COINS", null, Theme.Highlight);
                else UIKit.Toast("AD NOT READY", "TRY AGAIN IN A MOMENT", Theme.Danger);
                refreshAd();
            });
        });
    }

    static void ThemeCard(Transform parent, int index, Vector2 pos)
    {
        Image card = UIKit.Panel(parent, "Theme_" + index, new Vector2(380f, 250f), Theme.SurfaceSoft);
        card.raycastTarget = false;
        card.rectTransform.anchoredPosition = pos;

        Text name = UIKit.Label(card.transform, BlockSkin.ThemeNames[index], 40, Theme.Text, TextAnchor.MiddleCenter);
        name.rectTransform.anchoredPosition = new Vector2(0f, 88f);

        // Renk ornekleri: temanin bloklari
        Color[] pal = BlockSkin.Themes[index];
        float sw = 44f, gap = 6f;
        float start = -((pal.Length - 1) * (sw + gap)) * 0.5f;
        for (int c = 0; c < pal.Length; c++)
        {
            Image swatch = UIKit.Panel(card.transform, "Swatch", new Vector2(sw, sw), pal[c]);
            swatch.raycastTarget = false;
            swatch.pixelsPerUnitMultiplier = 3f;
            swatch.rectTransform.anchoredPosition = new Vector2(start + c * (sw + gap), 22f);
        }

        Button b = UIKit.TextButton(card.transform, "", Theme.Primary, new Vector2(0f, -66f), new Vector2(300f, 84f), 38);
        Text bt = b.GetComponentInChildren<Text>();
        Image bi = b.GetComponent<Image>();
        Shadow bs = b.GetComponent<Shadow>();

        Action refresh = null;
        refresh = () =>
        {
            bool owned = Economy.IsThemeOwned(index);
            bool selected = Economy.SelectedTheme == index;
            Color c = selected ? Theme.Positive : (owned ? Theme.Neutral : Theme.Primary);
            bi.color = c;
            if (bs != null) bs.effectColor = Theme.Darken(c, 0.55f);
            bt.text = selected ? "IN USE" : (owned ? "USE" : Economy.ThemePrice + " COINS");
            b.interactable = !selected;
        };
        refresh();

        b.onClick.AddListener(() =>
        {
            if (!Economy.IsThemeOwned(index))
            {
                if (!Economy.TryBuyTheme(index))
                {
                    UIKit.Toast("NOT ENOUGH COINS", "PLAY, COMPLETE MISSIONS OR WATCH AN AD", Theme.Danger);
                    return;
                }
                UIKit.Toast(BlockSkin.ThemeNames[index] + " UNLOCKED!", null, Theme.Positive);
            }
            Economy.SelectedTheme = index;
            Haptics.Light();
            // Tum kartlari yenile
            foreach (var card2 in parent.GetComponentsInChildren<ThemeCardRefresher>()) card2.Refresh();
        });
        ThemeCardRefresher r = card.gameObject.AddComponent<ThemeCardRefresher>();
        r.refresh = refresh;
    }

    // ---------- GOREVLER + GUNLUK MEYDAN OKUMA ----------
    public static void ShowMissions(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("MissionsWindow", "MISSIONS", new Vector2(900f, 1180f), out GameObject root);
        UIKit.CloseButton(w, root).onClick.AddListener(() => onClose?.Invoke());

        Text sub = UIKit.Label(w, "NEW MISSIONS EVERY DAY", 36, Theme.TextMuted, TextAnchor.MiddleCenter);
        sub.rectTransform.anchoredPosition = new Vector2(0f, 400f);

        Missions.Template[] today = Missions.Today;
        for (int i = 0; i < Missions.Count; i++)
        {
            MissionRow(w, i, today[i], 230f - i * 230f);
        }

        int streak = DailyChallenge.Streak;
        Text info = UIKit.Label(w, "DAILY CHALLENGE STREAK: " + streak, 38, Theme.Highlight, TextAnchor.MiddleCenter);
        info.rectTransform.anchoredPosition = new Vector2(0f, -430f);
    }

    static void MissionRow(Transform parent, int i, Missions.Template t, float y)
    {
        Image bg = UIKit.Panel(parent, "Mission_" + i, new Vector2(800f, 200f), Theme.SurfaceSoft);
        bg.raycastTarget = false;
        bg.rectTransform.anchoredPosition = new Vector2(0f, y);

        Text title = UIKit.Label(bg.transform, t.text, 40, Theme.Text, TextAnchor.MiddleLeft);
        title.rectTransform.sizeDelta = new Vector2(740f, 60f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 52f);

        int prog = Mathf.Min(Missions.GetProgress(i), t.target);
        UIKit.ProgressBar(bg.transform, new Vector2(-120f, -32f), new Vector2(480f, 36f), prog / (float)t.target, Theme.Positive);
        Text p = UIKit.Label(bg.transform, prog + " / " + t.target, 30, Theme.TextMuted, TextAnchor.MiddleCenter);
        p.rectTransform.anchoredPosition = new Vector2(-120f, -32f);

        Button claim = UIKit.TextButton(bg.transform, "", Theme.Primary, new Vector2(270f, -32f), new Vector2(220f, 84f), 34);
        Text ct = claim.GetComponentInChildren<Text>();
        Image ci = claim.GetComponent<Image>();
        Action refresh = () =>
        {
            if (Missions.IsClaimed(i)) { ct.text = "DONE"; claim.interactable = false; ci.color = Theme.Positive; }
            else if (Missions.IsComplete(i)) { ct.text = "CLAIM +" + t.reward; claim.interactable = true; ci.color = Theme.Primary; }
            else { ct.text = "+" + t.reward; claim.interactable = false; ci.color = Theme.Neutral; }
        };
        refresh();
        claim.onClick.AddListener(() =>
        {
            if (Missions.Claim(i))
            {
                Haptics.Medium();
                UIKit.Toast("+" + t.reward + " COINS", t.text, Theme.Highlight);
                refresh();
            }
        });
    }

    // ---------- BASARIMLAR ----------
    public static void ShowAchievements(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("AwardsWindow", "AWARDS", new Vector2(900f, 1400f), out GameObject root);
        UIKit.CloseButton(w, root).onClick.AddListener(() => onClose?.Invoke());

        Text sub = UIKit.Label(w, Achievements.UnlockedCount + " / " + Achievements.All.Length + " UNLOCKED", 38, Theme.TextMuted, TextAnchor.MiddleCenter);
        sub.rectTransform.anchoredPosition = new Vector2(0f, 510f);

        RectTransform content = UIKit.ScrollArea(w, new Vector2(0f, -60f), new Vector2(820f, 1080f));
        const float rowH = 150f, gap = 14f;
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            Achievements.Def d = Achievements.All[i];
            bool unlocked = Achievements.IsUnlocked(i);
            Image row = UIKit.Panel(content, "Ach_" + d.id, new Vector2(800f, rowH), unlocked ? Theme.Darken(Theme.Positive, 0.55f) : Theme.SurfaceSoft);
            RectTransform rr = row.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0f, -i * (rowH + gap));
            row.raycastTarget = false;

            UIKit.Icon(row.transform, UIKit.StarSprite, new Vector2(-330f, 0f), new Vector2(90f, 90f),
                unlocked ? Theme.Highlight : Theme.WithAlpha(Theme.TextMuted, 0.35f));

            Text title = UIKit.Label(row.transform, d.title, 40, unlocked ? Theme.Highlight : Theme.Text, TextAnchor.MiddleLeft);
            title.rectTransform.sizeDelta = new Vector2(500f, 50f);
            title.rectTransform.anchoredPosition = new Vector2(-20f, 36f);

            Text desc = UIKit.Label(row.transform, d.desc, 28, Theme.TextMuted, TextAnchor.MiddleLeft);
            desc.rectTransform.sizeDelta = new Vector2(500f, 40f);
            desc.rectTransform.anchoredPosition = new Vector2(-20f, -4f);

            int v = Mathf.Min(d.value(), d.target);
            UIKit.ProgressBar(row.transform, new Vector2(-20f, -46f), new Vector2(500f, 22f), v / (float)d.target, unlocked ? Theme.Highlight : Theme.Positive);

            Text reward = UIKit.Label(row.transform, unlocked ? "DONE" : "+" + d.reward, 36, unlocked ? Theme.Positive : Theme.Highlight, TextAnchor.MiddleCenter);
            reward.rectTransform.anchoredPosition = new Vector2(320f, 0f);
        }
        content.sizeDelta = new Vector2(content.sizeDelta.x, Achievements.All.Length * (rowH + gap));
    }

    // ---------- NASIL OYNANIR ----------
    public static void ShowHowToPlay(Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("HowToPlayWindow", "HOW TO PLAY", new Vector2(900f, 1400f), out GameObject root);
        UIKit.CloseButton(w, root).onClick.AddListener(() => onClose?.Invoke());

        string[][] sections =
        {
            new[] { "PLACE BLOCKS", "DRAG THE 3 PIECES ONTO THE 8x8 BOARD. FILL A FULL ROW OR COLUMN TO CLEAR IT." },
            new[] { "COMBOS", "CLEAR LINES ON BACK-TO-BACK MOVES TO BUILD A COMBO. A COMBO OF 5 STARTS FEVER: DOUBLE POINTS!" },
            new[] { "GAME OVER", "THE GAME ENDS WHEN NONE OF YOUR PIECES FIT. WATCH A SHORT VIDEO TO CONTINUE." },
            new[] { "UNDO & REFRESH", "UNDO YOUR LAST MOVE OR GET NEW PIECES. ONE FREE USE EACH GAME, MORE WITH A SHORT VIDEO." },
            new[] { "BOMB & ROTATE", "SPEND COINS: BOMB BLASTS A 3x3 AREA. ROTATE, THEN TAP A PIECE TO TURN IT." },
            new[] { "COINS", "EARN COINS FROM GAMES, LEVELS, DAILY BONUS, MISSIONS AND AWARDS. SPEND THEM IN THE SHOP." },
            new[] { "DAILY CHALLENGE", "A NEW BOARD EVERY DAY. REACH THE GOAL SCORE FOR 100 COINS." },
        };

        RectTransform content = UIKit.ScrollArea(w, new Vector2(0f, -70f), new Vector2(820f, 1140f));
        float y = 0f;
        foreach (string[] sec in sections)
        {
            Image card = UIKit.Panel(content, "Section", new Vector2(800f, 168f), Theme.SurfaceSoft);
            card.raycastTarget = false;
            RectTransform cr = card.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.anchoredPosition = new Vector2(0f, -y);

            Text title = UIKit.Label(card.transform, sec[0], 40, Theme.Highlight, TextAnchor.MiddleLeft);
            title.rectTransform.sizeDelta = new Vector2(740f, 50f);
            title.rectTransform.anchoredPosition = new Vector2(0f, 50f);

            Text body = UIKit.Label(card.transform, sec[1], 26, Theme.Text, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.rectTransform.sizeDelta = new Vector2(740f, 100f);
            body.rectTransform.anchoredPosition = new Vector2(0f, -26f);
            y += 182f;
        }
        content.sizeDelta = new Vector2(content.sizeDelta.x, y);
    }

    // ---------- GUNLUK GIRIS ODULU ----------
    public static void ShowDailyBonus(int coins, int streak, bool welcome, Action onClose = null)
    {
        RectTransform w = UIKit.CreateWindow("DailyBonusWindow", welcome ? "WELCOME!" : "DAILY BONUS", new Vector2(820f, 720f), out GameObject root);

        UIKit.Icon(w, UIKit.CoinSprite, new Vector2(0f, 120f), new Vector2(170f, 170f), Color.white);
        Text amount = UIKit.Label(w, "+" + coins, 110, Theme.Highlight, TextAnchor.MiddleCenter);
        amount.rectTransform.anchoredPosition = new Vector2(0f, -40f);

        Text s = UIKit.Label(w, welcome ? "HERE ARE SOME COINS TO START" : "DAY " + streak + " STREAK", 38, Theme.TextMuted, TextAnchor.MiddleCenter);
        s.rectTransform.anchoredPosition = new Vector2(0f, -140f);

        Button collect = UIKit.TextButton(w, "COLLECT", Theme.Positive, new Vector2(0f, -250f), new Vector2(460f, 120f), 54);
        collect.onClick.AddListener(() => { Haptics.Medium(); Close(root, onClose); });
    }
}

// Magazadaki tema kartlarinin durumunu (USE / IN USE / fiyat) yenilemek icin
public class ThemeCardRefresher : MonoBehaviour
{
    public Action refresh;
    public void Refresh() => refresh?.Invoke();
}
