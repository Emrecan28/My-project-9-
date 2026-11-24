using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static bool IsPauseMenuOpen { get; private set; }

    [Header("UI References")]
    public GameObject pausePanel; // Inspector'dan atanacak yeni panel (Resume ve Main Menu butonlarinin oldugu panel)
    private GridManager gridManager;
    private bool previousGameActiveState = true;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();

        // Baslangicta paneli gizle (Eger sahnede acik biraktiysaniz oyun baslayinca kapansin)
        if (pausePanel != null)
        {
            ApplyNeonTheme();
            pausePanel.SetActive(false);
        }
        IsPauseMenuOpen = false;
    }

    // Pause panelini oyunun neon temasina boyar (sahne duzenlemeden, oyun basinda bir kez)
    private void ApplyNeonTheme()
    {
        var panelImage = pausePanel.GetComponent<UnityEngine.UI.Image>();
        if (panelImage != null)
        {
            panelImage.sprite = NeoUI.RoundedSprite;
            panelImage.type = UnityEngine.UI.Image.Type.Sliced;
            panelImage.color = NeoUI.PanelDark;
            var border = pausePanel.GetComponent<UnityEngine.UI.Outline>();
            if (border == null) border = pausePanel.AddComponent<UnityEngine.UI.Outline>();
            border.effectColor = Theme.Neutral;
            border.effectDistance = new Vector2(6f, -6f);
        }

        foreach (var button in pausePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            var img = button.GetComponent<UnityEngine.UI.Image>();
            if (img == null) continue;

            string n = button.name.ToLowerInvariant();
            if (n.Contains("close")) continue; // Carpi butonu oldugu gibi kalsin

            Color c = Theme.Neutral;
            if (n.Contains("resume")) c = Theme.Positive;       // devam et: yesil
            else if (n.Contains("restart")) c = Theme.Primary;  // yeniden basla: turuncu
            // Levels ve Menu: notr acik mavi
            img.sprite = NeoUI.RoundedSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = c;

            // Buton yazilarini beyaz yap
            foreach (var tmp in button.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)) tmp.color = Color.white;
        }
    }

    // Sag ustteki "Pause" veya "Menu" butonuna bu fonksiyonu atayin
    public void OpenPauseMenu()
    {
        if (IsPauseMenuOpen) return;
        IsPauseMenuOpen = true;

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            previousGameActiveState = gridManager.isGameActive;
            gridManager.isGameActive = false; // Arka plan etkilesimini kapat
            gridManager.DisarmBomb();
            gridManager.DisarmRotate();
            gridManager.UpdateRefreshButtonState();
            gridManager.UpdateUndoButtonState();
        }

        // Eski sahne paneli yerine, diger pencerelerle ayni stilde kodla kurulan pencere
        if (pausePanel != null) pausePanel.SetActive(false);
        BuildPauseWindow();
    }

    // ---------------- YENI DURAKLAT PENCERESI ----------------
    private GameObject pauseWindow;

    private void BuildPauseWindow()
    {
        if (pauseWindow != null) Destroy(pauseWindow);
        bool levelMode = LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null
                         && !LevelManager.Instance.IsDailyMode;

        RectTransform w = UIKit.CreateWindow("PauseWindow", "PAUSED", new Vector2(820f, levelMode ? 960f : 820f), out pauseWindow);
        UIKit.CloseButton(w, pauseWindow).onClick.AddListener(ResumeAfterWindowClosed);

        float y = levelMode ? 270f : 200f;
        const float step = 140f;
        Button resume = UIKit.TextButton(w, "RESUME", Theme.Positive, new Vector2(0f, y), new Vector2(560f, 120f), 56);
        resume.onClick.AddListener(ResumeGame);
        y -= step;

        Button restart = UIKit.TextButton(w, "RESTART", Theme.Primary, new Vector2(0f, y), new Vector2(560f, 120f), 56);
        restart.onClick.AddListener(RestartGame);
        y -= step;

        if (levelMode)
        {
            Button levels = UIKit.TextButton(w, "LEVELS", Theme.Neutral, new Vector2(0f, y), new Vector2(560f, 120f), 56);
            levels.onClick.AddListener(GoToLevelSelect);
            y -= step;
        }

        Button menu = UIKit.TextButton(w, "MAIN MENU", Theme.Neutral, new Vector2(0f, y), new Vector2(560f, 120f), 56);
        menu.onClick.AddListener(GoToMainMenu);
        y -= step + 20f;

        // Hizli ayarlar: muzik / efekt / titresim
        QuickToggle(w, new Vector2(-235f, y), "MUSIC",
            () => SoundManager.Instance == null || SoundManager.Instance.IsMusicOn,
            on => { if (SoundManager.Instance != null) SoundManager.Instance.SetMusic(on); });
        QuickToggle(w, new Vector2(0f, y), "SOUND FX",
            () => SoundManager.Instance == null || SoundManager.Instance.IsSfxOn,
            on => { if (SoundManager.Instance != null) SoundManager.Instance.SetSfx(on); });
        QuickToggle(w, new Vector2(235f, y), "VIBRATION",
            () => Haptics.IsOn,
            on => { Haptics.IsOn = on; if (on) Haptics.Medium(); });
    }

    private void QuickToggle(Transform parent, Vector2 pos, string label, System.Func<bool> get, System.Action<bool> set)
    {
        UnityEngine.UI.Button b = UIKit.BaseButton(parent, "Toggle_" + label, Theme.SurfaceSoft, pos, new Vector2(215f, 110f));
        UnityEngine.UI.Text title = UIKit.Label(b.transform, label, 28, Theme.TextMuted, TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = new Vector2(0f, 22f);
        UnityEngine.UI.Text state = UIKit.Label(b.transform, "", 36, Theme.Positive, TextAnchor.MiddleCenter);
        state.rectTransform.anchoredPosition = new Vector2(0f, -22f);
        System.Action refresh = () =>
        {
            bool on = get();
            state.text = on ? "ON" : "OFF";
            state.color = on ? Theme.Positive : Theme.Danger;
        };
        refresh();
        b.onClick.AddListener(() => { set(!get()); PlayerPrefs.Save(); refresh(); });
    }

    // X ile kapatildi: pencere zaten yok edildi, sadece oyunu devam ettir
    private void ResumeAfterWindowClosed()
    {
        pauseWindow = null;
        ResumeGame();
    }

    private void ClosePauseWindow()
    {
        if (pauseWindow != null) Destroy(pauseWindow);
        pauseWindow = null;
    }

    // Paneldeki "Resume" butonuna bu fonksiyonu atayin
    public void ResumeGame()
    {
        ClosePauseWindow();
        {
            IsPauseMenuOpen = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                gridManager.isGameActive = previousGameActiveState; // Pause oncesi duruma geri don
                gridManager.UpdateRefreshButtonState();
                gridManager.UpdateUndoButtonState();
            }
            // Time.timeScale = 1f; // Zamani tekrar baslat
        }
    }

    // Paneldeki X (Kapatma) butonuna bu fonksiyonu atayin
    public void ClosePanel()
    {
        ClosePauseWindow();
        {
            IsPauseMenuOpen = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                gridManager.isGameActive = previousGameActiveState;
                gridManager.UpdateRefreshButtonState();
                gridManager.UpdateUndoButtonState();
            }
            Time.timeScale = 1f; 
        }
    }

    // Paneldeki "Main Menu" butonuna bu fonksiyonu atayin
    public void GoToMainMenu()
    {
        ClosePauseWindow();
        // Menuye donmeden once zamani duzelt (Eger durdurduysaniz)
        Time.timeScale = 1f; 
        IsPauseMenuOpen = false;
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.isGameActive = previousGameActiveState;
        
        // LevelManager uzerinden guvenli donus (Kayit islemi LevelManager.LoadMenu icinde yapiliyor)
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMenu();
        }
        else
        {
            // LevelManager yoksa direkt MainMenu sahnesine don (Yedek cozum)
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Paneldeki "Levels" butonuna bu fonksiyonu atayin
    public void GoToLevelSelect()
    {
        ClosePauseWindow();
        Time.timeScale = 1f;
        IsPauseMenuOpen = false;
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.isGameActive = previousGameActiveState;

        // Oyunu kaydet
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gm.SaveGame();
        }

        // Level secim sahnesini yukle
        SceneManager.LoadScene("LevelScene");
    }

    public void RestartGame()
    {
        ClosePauseWindow();
        Time.timeScale = 1f;
        IsPauseMenuOpen = false;
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.isGameActive = previousGameActiveState;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gm.ClearSave();
            gm.RestartGame();
        }
    }

    private void OnDisable()
    {
        IsPauseMenuOpen = false;
    }
}
