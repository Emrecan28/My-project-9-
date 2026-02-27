using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel; // Inspector'dan atanacak yeni panel (Resume ve Main Menu butonlarinin oldugu panel)

    private void Start()
    {
        // Baslangicta paneli gizle (Eger sahnede acik biraktiysaniz oyun baslayinca kapansin)
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // Sag ustteki "Pause" veya "Menu" butonuna bu fonksiyonu atayin
    public void OpenPauseMenu()
    {
        if (pausePanel != null)
        {
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
            pausePanel.SetActive(false);
            // Time.timeScale = 1f; // Zamani tekrar baslat
        }
    }

    // Paneldeki X (Kapatma) butonuna bu fonksiyonu atayin
    public void ClosePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f; 
        }
    }

    // Paneldeki "Main Menu" butonuna bu fonksiyonu atayin
    public void GoToMainMenu()
    {
        // Menuye donmeden once zamani duzelt (Eger durdurduysaniz)
        Time.timeScale = 1f; 
        
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
}
