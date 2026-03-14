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

    // Orijinal renkleri saklamak icin degiskenler
    private Color originalMusicOffColor = Color.white;
    private Color originalSfxOffColor = Color.white;
    private bool colorsCaptured = false;

    private void Start()
    {
        // iOS ve Android icin FPS'i 60'a sabitle
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Butonlarin orijinal renklerini yakala
        CaptureOriginalColors();

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
            // Kullanici en son hangi levelde kaldiysa oradan devam etsin
            int lastPlayed = PlayerPrefs.GetInt("LastPlayedLevel", LevelManager.Instance.GetUnlockedLevel());
            
            if (lastPlayed == -1)
            {
                LevelManager.Instance.StartEndlessMode();
            }
            else
            {
                LevelManager.Instance.LoadLevel(lastPlayed);
            }
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

    private void UpdateSettingsUI()
    {
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
