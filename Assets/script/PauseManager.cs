using UnityEngine;
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
            pausePanel.SetActive(false);
        }
        IsPauseMenuOpen = false;
    }

    // Sag ustteki "Pause" veya "Menu" butonuna bu fonksiyonu atayin
    public void OpenPauseMenu()
    {
        if (pausePanel != null)
        {
            IsPauseMenuOpen = true;

            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                previousGameActiveState = gridManager.isGameActive;
                gridManager.isGameActive = false; // Arka plan etkileşimini kapat
                gridManager.UpdateRefreshButtonState();
                gridManager.UpdateUndoButtonState();
            }

            pausePanel.SetActive(true);
            // Animasyonlari durdurmak isterseniz bu satiri acabilirsiniz:
            // Time.timeScale = 0f; 
        }
        else
        {
            Debug.LogError("PauseManager: Pause Panel atanmamis! Lutfen Inspector'dan atayin.");
        }
    }

    // Paneldeki "Resume" butonuna bu fonksiyonu atayin
    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            IsPauseMenuOpen = false;
            pausePanel.SetActive(false);
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
        if (pausePanel != null)
        {
            IsPauseMenuOpen = false;
            pausePanel.SetActive(false);
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
