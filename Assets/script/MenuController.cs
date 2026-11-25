using UnityEngine;
using UnityEngine.SceneManagement; // Sahneler arası geçiş için gerekli
using UnityEngine.UI; // Button ve Image kontrolü için gerekli

public class MenuController : MonoBehaviour
{
    [Header("UI Panelleri")]
    // Hierarchy'deki SettingsPanel objesini buraya surkleyeceksin
    public GameObject settingsPanel;
    public GameObject helpPanel; // Soru isareti butonu icin panel
    public GameObject gameplayPanel;

    [Header("Ayarlar Butonları")]
    public Button musicOnButton; // Muzik Acik Butonu (Hoparlor)
    public Button musicOffButton; // Muzik Kapali Butonu (Carpili Hoparlor)
    
    public Button sfxOnButton; // Ses Acik Butonu
    public Button sfxOffButton; // Ses Kapali Butonu

    [Header("Tek Butonlu Ses Ayarı")]
    public Button soundButton; // Tek GameSound butonu
    public GameObject soundOffIcon; // Ses kapaliyken gozukecek carpi isareti

    [Header("Gameplay")]
    public Button gameplayButton;
    
    [Header("Görsel Ayarlar")]
    public Color activeColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Secili olanin rengi (Daha az parlak)
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Secili olmayanin rengi (Gri)

    [Header("Buton Içi Icon Anim")]
    [SerializeField] private RectTransform startPlayIcon;
    [SerializeField] private RectTransform levelsButtonIcon;
    [SerializeField] private float startPlayIconWiggleAmplitude = 10f;
    [SerializeField] private float startPlayIconWiggleSpeed = 2f;
    [SerializeField] private float levelsIconWiggleAmplitude = 8f;
    [SerializeField] private float levelsIconWiggleSpeed = 2f;

    // Orijinal renkleri saklamak icin degiskenler
    private Color originalMusicOffColor = Color.white;
    private Color originalSfxOffColor = Color.white;
    private bool colorsCaptured = false;

    private Vector2 startPlayIconBasePos;
    private Vector2 levelsIconBasePos;
    private bool startPlayIconReady;
    private bool levelsIconReady;

    private void Start()
    {
        // iOS ve Android icin FPS'i 60'a sabitle
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Butonlarin orijinal renklerini yakala
        CaptureOriginalColors();

        // Ayarlar: ses butonunun yanina titresim butonu
        SetupHapticsButton();

        // Oyun ekraniyla ayni Block Blast tarzi gorunum
        ApplyModernMenuStyle();

        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(false);
        }

        if (gameplayButton != null)
        {
            gameplayButton.onClick.RemoveAllListeners();
            gameplayButton.onClick.AddListener(OpenGameplay);
        }

        if (soundButton != null)
        {
            soundButton.onClick.RemoveAllListeners();
            soundButton.onClick.AddListener(ToggleSound);
        }

        // --- GUVENLIK ONLEMI ---
        // Eger carpi ikonu (Image) tiklamalari engelliyorsa, onu devre disi birak
        if (soundOffIcon != null)
        {
            Image iconImage = soundOffIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.raycastTarget = false;
            }
        }
        // -----------------------

        // Baslangicta buton renklerini guncel duruma gore ayarla
        UpdateSettingsUI();

        SetupIconWigglesIfNeeded();
        ShowDailyBonusIfAny();
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        if (startPlayIconReady && startPlayIcon != null)
        {
            float x = Mathf.Sin(t * (startPlayIconWiggleSpeed * Mathf.PI * 2f)) * startPlayIconWiggleAmplitude;
            startPlayIcon.anchoredPosition = startPlayIconBasePos + new Vector2(x, 0f);
        }

        if (levelsIconReady && levelsButtonIcon != null)
        {
            float y = Mathf.Sin(t * (levelsIconWiggleSpeed * Mathf.PI * 2f)) * levelsIconWiggleAmplitude;
            levelsButtonIcon.anchoredPosition = levelsIconBasePos + new Vector2(0f, y);
        }
    }

    private void OnDisable()
    {
        if (startPlayIconReady && startPlayIcon != null) startPlayIcon.anchoredPosition = startPlayIconBasePos;
        if (levelsIconReady && levelsButtonIcon != null) levelsButtonIcon.anchoredPosition = levelsIconBasePos;
    }

    private void SetupIconWigglesIfNeeded()
    {
        if (startPlayIcon == null)
        {
            GameObject go = GameObject.Find("playbuton");
            if (go != null) startPlayIcon = go.GetComponent<RectTransform>();
        }

        if (levelsButtonIcon == null)
        {
            GameObject levelsButtonGo = GameObject.Find("LevelsButtons");
            if (levelsButtonGo != null)
            {
                Transform imageChild = levelsButtonGo.transform.Find("Image");
                if (imageChild != null) levelsButtonIcon = imageChild.GetComponent<RectTransform>();
            }
        }

        if (startPlayIcon != null)
        {
            startPlayIconBasePos = startPlayIcon.anchoredPosition;
            startPlayIconReady = true;
        }

        if (levelsButtonIcon != null)
        {
            levelsIconBasePos = levelsButtonIcon.anchoredPosition;
            levelsIconReady = true;
        }
    }


    private void CaptureOriginalColors()
    {
        if (colorsCaptured) return;

        if (musicOffButton != null)
        {
            Image img = musicOffButton.GetComponent<Image>();
            if (img != null) originalMusicOffColor = img.color;
        }

        if (sfxOffButton != null)
        {
            Image img = sfxOffButton.GetComponent<Image>();
            if (img != null) originalSfxOffColor = img.color;
        }
        
        colorsCaptured = true;
    }

    /// <summary>
    /// START butonuna basildiginda oyunu baslatir.
    /// </summary>
    public void StartGame()
    {
        if (LevelManager.Instance != null)
        {
            // Eger oyuncu daha once bir levelden ciktiysa, o levelin kaydini sil ki
            // sonsuz moda (klasik moda) girince levelin eski gridleri gelmesin.
            PlayerPrefs.SetInt("LastPlayedLevel", -1);
            PlayerPrefs.Save();
            
            // START butonu HER ZAMAN sonsuz modu (klasik modu) baslatmali
            LevelManager.Instance.StartEndlessMode();
        }
        else
        {
            // LevelManager yoksa direkt GameScene'i yukle
            SceneManager.LoadScene("GameScene");
        }
    }

    /// <summary>
    /// LEVELS butonuna basildiginda Level secim sahnesini yukler.
    /// </summary>
    public void OpenLevels()
    {
        SceneManager.LoadScene("LevelScene");
    }

    /// <summary>
    /// SETTINGS butonuna basildiginda paneli acar.
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true); // Paneli gorunur yapar
            UpdateSettingsUI(); // Paneli acinca butonlarin durumunu guncelle
        }
        else
        {
            Debug.LogError("Settings Panel atanmamis! Lutfen MenuManager uzerinden surukleyin.");
        }
    }

    /// <summary>
    /// Panel icindeki kapatma (X) butonuna basildiginda paneli gizler.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false); // Paneli gizler
        }
    }

    public void OpenGameplay()
    {
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(true);
        }
    }

    public void CloseGameplay()
    {
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Oyundan cikis yapar (Sadece Build alinmis surumde calisir).
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Oyundan cikildi.");
    }

    /// <summary>
    /// Oyun icindeyken Menuye donmek icin kullanilir.
    /// Oyunu kaydeder ve Menuye doner.
    /// </summary>
    public void GoToMenu()
    {
        // Oyunu kaydet
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gm.SaveGame();
        }

        // LevelManager uzerinden menuye don
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMenu();
        }
        else
        {
            // LevelManager yoksa MainMenu sahnesine don
            SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// HELP (Soru Isareti) butonuna basildiginda paneli acar.
    /// </summary>
    public void OpenHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Help Panel atanmamis! Lutfen MenuManager uzerinden surukleyin.");
        }
    }

    /// <summary>
    /// Help paneli kapatma.
    /// </summary>
    public void CloseHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }
    }

    // --- SES AYARLARI ---

    public void ToggleSound()
    {
        if (SoundManager.Instance != null)
        {
            // Müzik açıksa kapat, kapalıysa aç (hem müzik hem sfx)
            bool newState = !SoundManager.Instance.IsMusicOn;
            
            SoundManager.Instance.SetMusic(newState);
            SoundManager.Instance.SetSfx(newState);
            
            UpdateSettingsUI();
        }
    }

    public void EnableMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusic(true);
            UpdateSettingsUI();
        }
    }

    public void DisableMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusic(false);
            UpdateSettingsUI();
        }
    }

    public void EnableSfx()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSfx(true);
            UpdateSettingsUI();
        }
    }

    public void DisableSfx()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSfx(false);
            UpdateSettingsUI();
        }
    }

    // Eski Toggle fonksiyonlari (Eger tek buton kullanmak istersen diye birakildi ama yukaridakileri kullanman daha iyi)
    public void ToggleMusic()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ToggleMusic();
            UpdateSettingsUI();
        }
    }

    public void ToggleSfx()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ToggleSfx();
            UpdateSettingsUI();
        }
    }

    // --- TITRESIM AYARI ---
    [Header("Titresim")]
    public GameObject hapticsOffIcon; // Titresim kapaliyken gorunecek carpi (opsiyonel)

    [Tooltip("Bos birakilirsa ses butonunun bir kopyasi titresim butonu olarak otomatik olusturulur")]
    public Button hapticsButton;
    private Image hapticsIconImage;

    // Ses butonunu kopyalayip yanina titresim butonu koyar (ayni stil, sahne duzenlemesi gerekmez)
    [Header("Modern Menu Stili")]
    public bool useModernStyle = true;

    private void ApplyModernMenuStyle()
    {
        if (!useModernStyle) return;
        Canvas canvas = FindMenuCanvas();
        if (canvas == null) return;

        Transform bg = canvas.transform.Find("Background");
        if (bg != null) NeoUI.ApplyGradientBackground(bg.GetComponent<Image>());

        Transform menuButtons = canvas.transform.Find("MenuButtons");
        if (menuButtons == null) menuButtons = canvas.transform;

        // Eski piksel butonlari (ok ve kilit ikonlariyla birlikte) gizle; yerine duzgun hizali yeni menu
        Transform oldStart = menuButtons.Find("StartButtons");
        Transform oldLevels = menuButtons.Find("LevelsButtons");
        if (oldStart != null) oldStart.gameObject.SetActive(false);
        if (oldLevels != null) oldLevels.gameObject.SetActive(false);

        BuildMenu(canvas, menuButtons);
    }

    // Ana menu canvas'ini guvenle bul. FindFirstObjectByType<Canvas> cihazda baska bir canvas'i
    // (reklam/bildirim/alt panel) dondurebiliyordu: o zaman eski menu kaliyordu.
    private Canvas FindMenuCanvas()
    {
        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in all)
        {
            if (c != null && c.isRootCanvas && c.transform.Find("MenuButtons") != null) return c;
        }
        if (soundButton != null)
        {
            Canvas c = soundButton.GetComponentInParent<Canvas>(true);
            if (c != null) return c.rootCanvas;
        }
        foreach (Canvas c in all)
        {
            if (c != null && c.isRootCanvas && c.gameObject.scene == gameObject.scene) return c;
        }
        return null;
    }

    // ---------------- YENI ANA MENU ----------------
    private Text classicSub, levelsSub, dailySub;
    private GameObject missionsBadge;
    private Button dailyButton;

    private void BuildMenu(Canvas canvas, Transform parent)
    {
        GameObject stack = new GameObject("NeoMenu", typeof(RectTransform));
        stack.transform.SetParent(parent, false);
        RectTransform sr = stack.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
        sr.sizeDelta = new Vector2(1000f, 1000f);
        sr.anchoredPosition = new Vector2(-parent.GetComponent<RectTransform>().anchoredPosition.x, 0f);

        // Kisa ekranlarda (iPad) logo ile alt butonlar arasina sigsin
        float h = UIKit.CanvasHeight;
        float fit = Mathf.Clamp(h / 1920f, 0.72f, 1f);
        sr.localScale = Vector3.one * fit;
        sr.anchoredPosition += new Vector2(0f, -40f * fit);

        FitLogo(canvas, fit);

        const float bigW = 700f, bigH = 165f;

        Button classic = BigButton(stack.transform, "CLASSIC", Theme.Primary, new Vector2(0f, 300f), new Vector2(bigW, bigH), out classicSub);
        classic.onClick.AddListener(StartGame);

        Button levels = BigButton(stack.transform, "LEVELS", Theme.Positive, new Vector2(0f, 100f), new Vector2(bigW, bigH), out levelsSub);
        levels.onClick.AddListener(OpenLevels);

        dailyButton = BigButton(stack.transform, "DAILY CHALLENGE", Theme.Highlight, new Vector2(0f, -100f), new Vector2(bigW, bigH), out dailySub);
        dailyButton.onClick.AddListener(StartDaily);

        // Kucuk butonlar: 2 satir x 3, ayni genislik, tam ortali
        const float smallW = 222f, smallH = 120f, gapX = 17f;
        float x0 = -(smallW + gapX);
        Button missions = UIKit.TextButton(stack.transform, "MISSIONS", Theme.SurfaceSoft, new Vector2(x0, -285f), new Vector2(smallW, smallH), 36);
        missions.onClick.AddListener(() => MenuPanels.ShowMissions(RefreshMenu));
        Button shop = UIKit.TextButton(stack.transform, "SHOP", Theme.SurfaceSoft, new Vector2(0f, -285f), new Vector2(smallW, smallH), 36);
        shop.onClick.AddListener(() => MenuPanels.ShowShop(RefreshMenu));
        Button awards = UIKit.TextButton(stack.transform, "AWARDS", Theme.SurfaceSoft, new Vector2(-x0, -285f), new Vector2(smallW, smallH), 36);
        awards.onClick.AddListener(() => MenuPanels.ShowAchievements(RefreshMenu));

        Button stats = UIKit.TextButton(stack.transform, "STATS", Theme.SurfaceSoft, new Vector2(x0, -425f), new Vector2(smallW, smallH), 36);
        stats.onClick.AddListener(() => MenuPanels.ShowStats(RefreshMenu));
        Button settings = UIKit.TextButton(stack.transform, "SETTINGS", Theme.SurfaceSoft, new Vector2(0f, -425f), new Vector2(smallW, smallH), 36);
        settings.onClick.AddListener(() => MenuPanels.ShowSettings(UpdateSettingsUI));
        Button ranks = UIKit.TextButton(stack.transform, "RANKS", Theme.SurfaceSoft, new Vector2(-x0, -425f), new Vector2(smallW, smallH), 36);
        ranks.onClick.AddListener(GameCenter.ShowLeaderboard);

        // Gorevlerde alinmamis odul varsa kirmizi nokta
        Image dot = UIKit.Icon(missions.transform, UIKit.CircleSprite, new Vector2(smallW * 0.5f - 14f, smallH * 0.5f - 14f), new Vector2(40f, 40f), Theme.Danger);
        missionsBadge = dot.gameObject;

        // Sol ust: para rozeti (centik payi kadar asagida)
        GameObject top = new GameObject("TopBar", typeof(RectTransform));
        top.transform.SetParent(canvas.transform, false);
        RectTransform tr = top.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0f, 1f);
        tr.pivot = new Vector2(0f, 1f);
        tr.sizeDelta = new Vector2(300f, 90f);
        tr.anchoredPosition = new Vector2(30f, -30f - UIKit.TopInset);
        Text coins = UIKit.CoinBadge(top.transform, Vector2.zero, new Vector2(280f, 84f));
        coins.raycastTarget = false;

        PlaceBelowPanels(top.transform, canvas);
        BuildBottomRow(canvas);
        RefreshMenu();
    }

    // Kodla eklenen menu parcalari, sahnedeki acilir panellerin (Nasil oynanir, Yardim) ALTINDA cizilsin
    private static void PlaceBelowPanels(Transform t, Canvas canvas)
    {
        int index = t.GetSiblingIndex();
        foreach (string name in new[] { "gameplayp", "HelpPanel", "SettingPanel" })
        {
            Transform panel = canvas.transform.Find(name);
            if (panel != null && panel != t) index = Mathf.Min(index, panel.GetSiblingIndex());
        }
        t.SetSiblingIndex(index);
    }

    // ---------------- ALT SIRA: NASIL OYNANIR / TITRESIM / SES ----------------
    // Eski kose butonlari (farkli boy ve hizada) gizlenir; yerine ortali, esit boyda, etiketli uc yuvarlak buton.
    private Image bottomSoundIcon, bottomHapticIcon;
    private GameObject bottomSoundSlash, bottomHapticSlash;

    private void BuildBottomRow(Canvas canvas)
    {
        Sprite helpSprite = ChildSprite(gameplayButton);
        Sprite soundSprite = ChildSprite(soundButton);
        Sprite vibSprite = NeoUI.LoadSprite("Icons/vibration");

        if (gameplayButton != null) gameplayButton.gameObject.SetActive(false);
        if (soundButton != null) soundButton.gameObject.SetActive(false);
        if (hapticsButton != null) hapticsButton.gameObject.SetActive(false);

        GameObject row = new GameObject("BottomRow", typeof(RectTransform));
        row.transform.SetParent(canvas.transform, false);
        RectTransform rr = row.GetComponent<RectTransform>();
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0f);
        rr.sizeDelta = new Vector2(800f, 200f);
        float fit = Mathf.Clamp(UIKit.CanvasHeight / 1920f, 0.8f, 1f);
        rr.localScale = Vector3.one * fit;
        rr.anchoredPosition = new Vector2(0f, UIKit.BottomInset + 120f * fit);

        PlaceBelowPanels(row.transform, canvas);

        RoundIcon(row.transform, new Vector2(-240f, 0f), helpSprite, "HOW TO PLAY", () => MenuPanels.ShowHowToPlay(), out _, out _);
        RoundIcon(row.transform, new Vector2(0f, 0f), vibSprite, "VIBRATION", () => { ToggleHaptics(); UpdateSettingsUI(); }, out bottomHapticIcon, out bottomHapticSlash);
        RoundIcon(row.transform, new Vector2(240f, 0f), soundSprite, "SOUND", () => { ToggleSound(); UpdateSettingsUI(); }, out bottomSoundIcon, out bottomSoundSlash);
    }

    private static Sprite ChildSprite(Button b)
    {
        if (b == null) return null;
        foreach (Image img in b.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject == b.gameObject || img.sprite == null) continue;
            return img.sprite; // butonun icindeki ilk ikon
        }
        Image own = b.GetComponent<Image>();
        return own != null ? own.sprite : null;
    }

    private void RoundIcon(Transform parent, Vector2 pos, Sprite icon, string label, UnityEngine.Events.UnityAction onClick, out Image iconImage, out GameObject slash)
    {
        Button b = UIKit.BaseButton(parent, "Round_" + label, Theme.SurfaceSoft, pos + new Vector2(0f, 24f), new Vector2(140f, 140f));
        Image bg = b.GetComponent<Image>();
        bg.sprite = UIKit.CircleSprite;
        bg.type = Image.Type.Simple;
        Transform gloss = b.transform.Find("Gloss");
        if (gloss != null) gloss.gameObject.SetActive(false);
        b.onClick.AddListener(onClick);

        iconImage = UIKit.Icon(b.transform, icon, Vector2.zero, new Vector2(82f, 82f), Color.white);
        if (icon == null) iconImage.enabled = false;

        // Kapaliyken ikonun ustunde kirmizi capraz cizgi
        Image s = UIKit.Panel(b.transform, "Off", new Vector2(14f, 112f), Theme.Danger);
        s.raycastTarget = false;
        s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        s.gameObject.SetActive(false);
        slash = s.gameObject;

        Text t = UIKit.Label(parent, label, 28, Theme.TextMuted, TextAnchor.MiddleCenter);
        t.rectTransform.anchoredPosition = pos + new Vector2(0f, -72f);
    }

    private void RefreshBottomRow()
    {
        bool soundOn = SoundManager.Instance == null || SoundManager.Instance.IsMusicOn || SoundManager.Instance.IsSfxOn;
        if (bottomSoundIcon != null) bottomSoundIcon.color = new Color(1f, 1f, 1f, soundOn ? 1f : 0.45f);
        if (bottomSoundSlash != null) bottomSoundSlash.SetActive(!soundOn);
        if (bottomHapticIcon != null) bottomHapticIcon.color = new Color(1f, 1f, 1f, Haptics.IsOn ? 1f : 0.45f);
        if (bottomHapticSlash != null) bottomHapticSlash.SetActive(!Haptics.IsOn);
    }

    // iPad gibi kisa/genis ekranlarda logo ekranin ustunden tasmasin ve butonlarin ustune binmesin
    private void FitLogo(Canvas canvas, float fit)
    {
        Transform logoT = canvas.transform.Find("Logo");
        if (logoT == null) logoT = canvas.transform.Find("logo");
        RectTransform lr = logoT as RectTransform;
        if (lr == null) return;

        float half = UIKit.CanvasHeight * 0.5f;
        float top = half - UIKit.TopInset - 40f;                // logonun cikabilecegi en ust nokta
        float buttonsTop = (300f + 82f) * fit - 40f * fit + 40f; // CLASSIC butonunun ustu + bosluk
        float logoH = lr.rect.height * Mathf.Abs(lr.localScale.y);
        float logoTop = lr.localPosition.y + logoH * (1f - lr.pivot.y);
        float logoBottom = lr.localPosition.y - logoH * lr.pivot.y;
        if (logoTop <= top && logoBottom >= buttonsTop) return; // zaten sigiyor (telefonlar)

        float available = Mathf.Max(80f, top - buttonsTop);
        if (logoH > available)
        {
            float k = available / logoH;
            lr.localScale = new Vector3(lr.localScale.x * k, lr.localScale.y * k, lr.localScale.z);
            logoH = available;
        }
        float centerY = (top + buttonsTop) * 0.5f;
        Vector3 p = lr.localPosition;
        p.y = centerY + logoH * (lr.pivot.y - 0.5f);
        lr.localPosition = p;

        // Ziplama animasyonu yeni konumu temel alsin
        FloatingLogo fl = lr.GetComponent<FloatingLogo>();
        if (fl != null) fl.SetBase(p);
    }

    // Ana buton: buyuk baslik + altinda kucuk bilgi satiri (rekor, level, gunluk hedef)
    private Button BigButton(Transform parent, string title, Color color, Vector2 pos, Vector2 size, out Text subtitle)
    {
        Button b = UIKit.BaseButton(parent, "Button_" + title, color, pos, size);
        Text t = UIKit.Label(b.transform, title, 62, Theme.Text, TextAnchor.MiddleCenter);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 20;
        t.resizeTextMaxSize = 62;
        t.rectTransform.sizeDelta = new Vector2(size.x - 60f, 80f);
        t.rectTransform.anchoredPosition = new Vector2(0f, 20f);

        subtitle = UIKit.Label(b.transform, "", 32, Theme.WithAlpha(Color.white, 0.85f), TextAnchor.MiddleCenter);
        subtitle.rectTransform.sizeDelta = new Vector2(size.x - 60f, 44f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        return b;
    }

    private void RefreshMenu()
    {
        if (classicSub != null) classicSub.text = "BEST " + GameStats.BestScore;
        if (levelsSub != null)
        {
            int unlocked = LevelManager.Instance != null ? LevelManager.Instance.GetUnlockedLevel() : PlayerPrefs.GetInt("UnlockedLevel", 1);
            levelsSub.text = "LEVEL " + unlocked;
        }
        if (dailySub != null)
        {
            dailySub.text = DailyChallenge.IsCompletedToday
                ? "COMPLETED - NEW ONE TOMORROW"
                : "GOAL " + DailyChallenge.TargetScore + "   +" + Economy.DailyChallengeReward + " COINS";
        }
        if (missionsBadge != null) missionsBadge.SetActive(Missions.UnclaimedCount > 0);
        UpdateSettingsUI();
    }

    public void StartDaily()
    {
        if (DailyChallenge.IsCompletedToday)
        {
            UIKit.Toast("DAILY DONE!", "COME BACK TOMORROW FOR A NEW ONE", Theme.Positive);
            return;
        }
        if (LevelManager.Instance == null)
        {
            new GameObject("LevelManager").AddComponent<LevelManager>();
        }
        LevelManager.Instance.StartDailyChallenge();
    }

    // Menu acilinca: gunluk giris odulu + Game Center girisi
    private void ShowDailyBonusIfAny()
    {
        int gained = Economy.ClaimDailyLogin(out int streak, out bool welcome);
        if (gained > 0) MenuPanels.ShowDailyBonus(gained, streak, welcome, RefreshMenu);
        Achievements.Check(); // Guncellemeden once kazanilmis basarimlar da acilsin
        GameCenter.SignIn();  // Game Center girisi (iOS kendi kucuk penceresini gosterir)
    }

    private void SetupHapticsButton()
    {
        if (hapticsButton == null && soundButton != null)
        {
            RectTransform srcRect = soundButton.GetComponent<RectTransform>();
            Vector2 visualSize = new Vector2(srcRect.rect.width * Mathf.Abs(srcRect.localScale.x),
                                             srcRect.rect.height * Mathf.Abs(srcRect.localScale.y));

            GameObject go = new GameObject("HapticsButton", typeof(RectTransform));
            go.transform.SetParent(soundButton.transform.parent, false);
            go.transform.SetSiblingIndex(soundButton.transform.GetSiblingIndex());
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = srcRect.anchorMin;
            rect.anchorMax = srcRect.anchorMax;
            rect.pivot = srcRect.pivot;
            rect.sizeDelta = visualSize;
            rect.anchoredPosition = srcRect.anchoredPosition - new Vector2(visualSize.x + 16f, 0f);

            // Arka plan: ses butonuyla ayni yuvarlak zemin
            Image bg = go.AddComponent<Image>();
            Image srcBg = soundButton.GetComponent<Image>();
            if (srcBg != null)
            {
                bg.sprite = srcBg.sprite;
                bg.type = srcBg.type;
                bg.color = srcBg.color;
            }

            hapticsButton = go.AddComponent<Button>();
            hapticsButton.targetGraphic = bg;

            Sprite vibrationIcon = NeoUI.LoadSprite("Icons/vibration");
            if (vibrationIcon != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(go.transform, false);
                hapticsIconImage = iconObj.AddComponent<Image>();
                hapticsIconImage.sprite = vibrationIcon;
                hapticsIconImage.preserveAspect = true;
                hapticsIconImage.raycastTarget = false;
                RectTransform iconRect = hapticsIconImage.rectTransform;
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = visualSize * 0.1f;
                iconRect.offsetMax = -visualSize * 0.1f;
            }
        }

        if (hapticsButton != null)
        {
            hapticsButton.onClick.AddListener(ToggleHaptics);
        }
    }

    public void ToggleHaptics()
    {
        Haptics.Toggle();
        if (Haptics.IsOn) Haptics.Medium(); // Acilinca hissettir
        UpdateSettingsUI();
    }

    private void UpdateSettingsUI()
    {
        RefreshBottomRow();
        if (hapticsOffIcon != null) hapticsOffIcon.SetActive(!Haptics.IsOn);
        if (hapticsIconImage != null)
        {
            Color c = hapticsIconImage.color;
            c.a = Haptics.IsOn ? 1f : 0.45f;
            hapticsIconImage.color = c;
        }

        if (SoundManager.Instance == null) return;
        
        // Garanti olsun diye renkleri yakalamaya calis (Start calismadiysa vs)
        CaptureOriginalColors();

        bool isMusicOn = SoundManager.Instance.IsMusicOn;
        bool isSfxOn = SoundManager.Instance.IsSfxOn;

        // --- TEK BUTONLU SISTEM ---
        // Eger muzik kapaliysa (ses kapali demektir) carpi ikonunu goster
        if (soundOffIcon != null)
        {
            soundOffIcon.SetActive(!isMusicOn);
        }
        // --------------------------

        // --- ESKI AYRI AYRI BUTON SISTEMI (Hala kullaniliyorsa diye birakildi) ---
        // Muzik kapaliysa (OFF butonu aktifse) rengini gri yap
        if (musicOffButton != null)
        {
            Image img = musicOffButton.GetComponent<Image>();
            if (img != null)
            {
                // Eger muzik KAPALIYSA (yani carpili buton seciliyse) hafif gri olsun
                // Eger muzik ACIKSA (yani carpili buton pasifse) orjinal rengine donsun
                img.color = !isMusicOn ? new Color(0.7f, 0.7f, 0.7f, 1f) : originalMusicOffColor;
            }
        }
        
        // Ayni mantik SFX icin de gecerli
        if (sfxOffButton != null)
        {
            Image img = sfxOffButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = !isSfxOn ? new Color(0.7f, 0.7f, 0.7f, 1f) : originalSfxOffColor;
            }
        }
    }
}
