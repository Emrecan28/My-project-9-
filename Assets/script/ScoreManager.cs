using UnityEngine;
using TMPro; // TextMeshPro kullanmak i�in gerekli

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI highScoreText;

    private int score = 0;
    private int highScore = 0;
    
    // Level Modu icin Hedef Puan
    private int targetScore = 0;
    private bool isLevelMode = false;
    private bool isLevelCompleted = false; // Level tamamlandi mi kontrolu

    void Awake()
    {
        if (Instance == null) Instance = this;
        highScore = PlayerPrefs.GetInt("HighScore", 0); // Kayitli rekoru yukle
    }

    void Start()
    {
        ResetLevelStatus();
        
        // LevelManager'dan veri al
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            isLevelMode = true;
            targetScore = LevelManager.Instance.currentLevelData.targetScore;
        }
        else
        {
            isLevelMode = false;
        }
        
        UpdateUI();
    }
    
    // Scene basladiginda ScoreManager'in LevelManager verisini tekrar okumasini sagla
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Yeni bir sahne (ozellikle GameScene) yuklendiginde LevelManager verilerini tekrar kontrol et
        if (LevelManager.Instance != null)
        {
            if (LevelManager.Instance.currentLevelData != null)
            {
                isLevelMode = true;
                targetScore = LevelManager.Instance.currentLevelData.targetScore;
            }
            else
            {
                isLevelMode = false;
            }
        }
        UpdateUI();
    }
    
    void ResetLevelStatus()
    {
        isLevelCompleted = false;
    }

    public void AddScore(int amount)
    {
        score += amount;
        
        // Endless modda rekor takibi
        if (!isLevelMode)
        {
            if (score > highScore)
            {
                highScore = score;
                PlayerPrefs.SetInt("HighScore", highScore); // Yeni rekoru kaydet
            }
        }
        
        UpdateUI();
        
        // Level Modu Kontrolu
        if (isLevelMode && score >= targetScore && !isLevelCompleted)
        {
            CompleteLevel();
        }
    }
    
    void CompleteLevel()
    {
        isLevelCompleted = true; // Tekrar tetiklenmesini onle
        
        // Level Tamamlandi!
        // GridManager uzerinden tebrik mesaji gosterilebilir veya burada basitce gecis yapilabilir.
        // Kullanici "LEVEL COMPLETE!" yazisi istiyor.
        
        // GridManager'a ulasip oyunu durdurmasini veya efekti baslatmasini isteyelim
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gm.ShowLevelComplete();
        }
    }

    public int GetCurrentScore()
    {
        return score;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
        UpdateUI();
    }
    
    public void ResetScore()
    {
        score = 0;
        ResetLevelStatus();
        
        // Level modundaysak target'i guncelle (Restart durumunda)
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            isLevelMode = true;
            targetScore = LevelManager.Instance.currentLevelData.targetScore;
        }
        else
        {
            isLevelMode = false;
        }
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (isLevelMode)
        {
            // Level Modu: Score: 150 / 200 formatinda
            currentScoreText.text = "Score: " + score + " / " + targetScore;
            
            // Goal gostergesi
            if (highScoreText != null) 
            {
                highScoreText.text = "Goal: " + targetScore;
            }
        }
        else
        {
            // Sonsuz Mod: Klasik gorunum
            currentScoreText.text = "Score: " + score;
            
            // Endless modda BEST SCORE (Rekor) gosterilir
            if (highScoreText != null) 
            {
                highScoreText.text = "Best: " + highScore;
            }
        }
    }
}