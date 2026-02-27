using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    
    [Header("UI Panels")]
    public GameObject menuPanel; // Level secim ekrani
    public GameObject gamePanel; // Oyun ekrani (GridManager vs burada olmali)
    
    [Header("Game References")]
    public GridManager gridManager;
    
    public List<LevelData> levels = new List<LevelData>();
    public LevelData currentLevelData; // Null ise Sonsuz Mod, dolu ise Level Modu
    
    private const string UNLOCKED_LEVEL_KEY = "UnlockedLevel";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahneler arasi geciste yok olmasin
            GenerateLevels();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenu sahnesi yuklendiginde menuyu hazirla
        if (scene.name == "MainMenu")
        {
            // GridManager referansini temizle (cunku oyun sahnesinden ciktik)
            gridManager = null;
            LoadMenu();
        }
    }
    
    void Start()
    {
        // LevelManager artik sadece veri tasiyici, UI kontrolu yapmasina gerek yok
        // LoadMenu(); 
    }
    
    void GenerateLevels()
    {
        // 99 Seviye oluştur
        for (int i = 1; i <= 99; i++)
        {
            // Hedef puan her seviyede artar
            int target = 500 + ((i - 1) * 250);
            LevelData data = new LevelData(i, target);
            GeneratePatterns(data, i);
            levels.Add(data);
        }
    }
    
    void GeneratePatterns(LevelData data, int levelIndex)
    {
        // Kullanici istegi: Level 2'den itibaren baslasin (Eskiden 5 idi)
        if (levelIndex < 2) return;

        int obstacleCount = 0;
        
        // Zorluk seviyesine gore engel sayisini belirle
        // Kullanici istegi: 5. bolumden itibaren gridde yerlesmis olan bloklari arttir (Agresif artis)
        if (levelIndex < 5) 
        {
            obstacleCount = 3; // Level 2, 3, 4 -> 3 Engel (Eskiden 2)
        }
        else if (levelIndex < 10) 
        {
            obstacleCount = 6; // Level 5-9 -> 6 Engel (Eskiden 4)
        }
        else if (levelIndex < 20) 
        {
            obstacleCount = 8; // Level 10-19 -> 8 Engel (Eskiden 5)
        }
        else if (levelIndex < 40) 
        {
            obstacleCount = 10; // Level 20-39 -> 10 Engel (Eskiden 6)
        }
        else 
        {
            obstacleCount = 12; // Ileri seviyelerde 12 engel
        }
        
        // Sekil Tanimlari (Kullanici istegi: Buyuk butun blok seklinde sekiller)
        List<Vector2Int[]> shapes = new List<Vector2Int[]> {
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(1,1) }, // Kare 2x2
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0) }, // Yatay 2
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // Yatay 3
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1) }, // Dikey 2
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) }, // Dikey 3
            new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0) } // L Kucuk
        };

        // Rastgele engel sekilleri yerlestir
        for (int k = 0; k < obstacleCount; k++)
        {
            // Rastgele bir sekil sec
            Vector2Int[] shape = shapes[Random.Range(0, shapes.Count)];
            
            // Yerlestirmeyi dene (Max 10 deneme)
            for(int attempt=0; attempt<10; attempt++) 
            {
                int rX = Random.Range(0, 8);
                int rY = Random.Range(0, 8);
                
                // Merkezden uzak tutmaya calis (oyun kilitlenmesin diye)
                if (rX > 2 && rX < 5 && rY > 2 && rY < 5 && Random.value > 0.3f) continue;
                
                // Seklin sigma kontrolu
                bool canFit = true;
                List<Vector2Int> currentShapePositions = new List<Vector2Int>();
                
                foreach(var offset in shape) 
                {
                    int x = rX + offset.x;
                    int y = rY + offset.y;
                    
                    // Grid disina cikti mi?
                    if (x >= 8 || y >= 8) { canFit = false; break; }
                    
                    Vector2Int pos = new Vector2Int(x, y);
                    // Zaten dolu mu?
                    if (data.preFilledBlocks.Contains(pos)) { canFit = false; break; }
                    
                    currentShapePositions.Add(pos);
                }
                
                if (canFit) 
                {
                    // Yerlestir
                    data.obstacleGroups.Add(currentShapePositions);
                    data.preFilledBlocks.AddRange(currentShapePositions);
                    break; // Basarili, sonraki engele gec
                }
            }
        }
    }
    
    public int GetUnlockedLevel()
    {
        return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
    }
    
    public void UnlockNextLevel()
    {
        int currentUnlocked = GetUnlockedLevel();
        // Eger su an oynanan level, en son acik olan level ise bir sonrakini ac
        if (currentLevelData != null && currentLevelData.levelNumber == currentUnlocked)
        {
            if (currentUnlocked < 99)
            {
                PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, currentUnlocked + 1);
                PlayerPrefs.Save();
            }
        }
    }
    
    public void LoadNextLevel()
    {
        if (currentLevelData != null)
        {
            int nextLvl = currentLevelData.levelNumber + 1;
            if (nextLvl <= 99)
            {
                UnlockNextLevel(); // Bir sonrakini ac
                LoadLevel(nextLvl);
            }
            else
            {
                // Oyun bitti veya ana menuye don
                LoadMenu();
            }
        }
    }
    
    public void LoadLevel(int levelNumber)
    {
        if (levelNumber > 0 && levelNumber <= levels.Count)
        {
            currentLevelData = levels[levelNumber - 1];
            SceneManager.LoadScene("GameScene");
        }
    }
    
    public void StartEndlessMode()
    {
        currentLevelData = null;
        SceneManager.LoadScene("GameScene");
    }
    
    private void StartGame()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
        
        // GridManager'i baslat
        if (gridManager != null)
        {
            gridManager.GenerateLevel();
        }
        else
        {
            // Belki sahnede arar buluruz (yeni API)
            gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null) gridManager.GenerateLevel();
        }
    }
    
    public void LoadMenu()
    {
        // 1. Oyunu Kaydet (Eger oyun sahnesindeysek)
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        
        // Guvenli Kayit
        if (gridManager != null) 
        {
            try 
            {
                gridManager.SaveGame();
            }
            catch(System.Exception e)
            {
                Debug.LogWarning("LoadMenu sirasinda kayit hatasi (Onemsiz): " + e.Message);
            }
        }

        // 2. Eger Ana Menu sahnesinde degilsek, MainMenu sahnesini yukle
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // 3. Ana Menu sahnesindeyiz, panelleri ayarla
        if (menuPanel == null)
        {
            // Referans kaybolmussa (sahne yenilendigi icin) tekrar bul
            var ui = FindFirstObjectByType<LevelSelectUI>();
            if (ui != null) menuPanel = ui.gameObject;
        }

        if (gamePanel != null) gamePanel.SetActive(false);
        if (menuPanel != null) 
        {
            menuPanel.SetActive(true);
            // Menudeki UI'yi guncelle
            LevelSelectUI ui = menuPanel.GetComponent<LevelSelectUI>();
            if (ui != null) ui.RefreshUI();
            else 
            {
                ui = menuPanel.GetComponentInChildren<LevelSelectUI>();
                if (ui != null) ui.RefreshUI();
            }
        }
    }
}
