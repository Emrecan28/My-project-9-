using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("--- 1. Panel Ayarlari ---")]
    public GameObject panelPrefab;
    public Color panelColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public float panelPadding = 0.4f;

    [Header("--- 2. Ust Grid Ayarlari ---")]
    public int rows = 8;
    public int columns = 8;
    public float cellSize = 1f;
    public float spacing = 0.1f;
    public GameObject cellPrefab;
    public Color cellColor = new Color(0.25f, 0.25f, 0.25f, 0.4f);

    [Header("--- 3. Kenarlik Ayarlari ---")]
    public bool useOutline = true;
    public Color outlineColor = Color.black;
    [Range(1.01f, 1.2f)] public float outlineSize = 1.06f;

    [Header("--- 4. Alt Yuva (Slot) Ayarlari ---")]
    public GameObject spawnSlotPrefab;
    public GameObject blockBasePrefab;
    public float bottomOffset = 4.0f;
    public float slotSpacing = 5.0f;
    public float slotScale = 2.8f;
    public float blockInSlotScale = 0.45f;

    public Color slotNormalColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color slotHoverColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    
    [Header("--- 5. Blok Renkleri ---")]
    public Color[] blockColors = new Color[] 
    { 
        new Color(1f, 0.3f, 0.3f),    // Kirmizi
        new Color(0.3f, 0.7f, 1f),    // Mavi
        new Color(0.3f, 1f, 0.5f),    // Yesil
        new Color(1f, 0.8f, 0.3f),    // Sari
        new Color(0.8f, 0.3f, 1f),    // Mor
        new Color(1f, 0.5f, 0.3f)     // Turuncu
    };
    
    [Header("--- 6. Patlama Efekti ---")]
    public float explosionDelay = 0.3f;
    public float explosionScale = 1.5f;
    public float explosionDuration = 0.2f;
    
    [Header("--- 7. Oyun Kontrolu ---")]
    public UnityEngine.Events.UnityEvent OnGameOver; // Oyun bittiğinde çağrılacak event
    public GameObject gameOverTextPrefab; // "NO MORE MOVES!" yazisi icin prefab (opsiyonel)
    public float gameOverTextDuration = 2f; // Yazinin ekranda kalma suresi
    
    [Header("--- 8. Görsel Geri Bildirim ---")]
    public Font feedbackFont; // Perfect/Nice yazilari icin font (MUTLAKA ATAYIN)
    public Color perfectNeonColorA = new Color(0.2f, 1f, 0.95f, 1f);
    public Color perfectNeonColorB = new Color(1f, 0.25f, 0.95f, 1f);
    public float perfectNeonDuration = 0.65f;
    [Range(0f, 1f)] public float perfectNeonIntensity = 0.85f;
    public Color niceNeonColorA = new Color(0.35f, 0.8f, 1f, 1f);
    public Color niceNeonColorB = new Color(0.75f, 0.45f, 1f, 1f);
    public float niceNeonDuration = 0.46f;
    [Range(0f, 1f)] public float niceNeonIntensity = 0.58f;
    public Color perfectEdgeFlashColor = new Color(0.1f, 1f, 0.95f, 1f);
    public float perfectEdgeFlashDuration = 0.35f;
    public Color perfectBorderPulseColor = new Color(1f, 0.35f, 0.95f, 1f);
    public float perfectBorderPulseDuration = 0.45f;
    public Color perfectBurstColorA = new Color(1f, 0.9f, 0.25f, 1f);
    public Color perfectBurstColorB = new Color(1f, 0.35f, 0.9f, 1f);
    public float perfectBurstDuration = 0.7f;
    
    [Header("--- Yan Hak UI ---")]
    public UnityEngine.UI.Button refreshButton;
    public UnityEngine.UI.Image refreshButtonBackground;
    public int remainingRefreshes = 1;
    public int maxRefreshes = 1; // Başlangıçtaki hak sayısı
    public TMPro.TextMeshProUGUI refreshCountText;
    public UnityEngine.UI.Button undoButton;
    public UnityEngine.UI.Image undoButtonBackground;
    public int remainingUndos = 1;
    public int maxUndos = 1;
    public TMPro.TextMeshProUGUI undoCountText;
    private Vector3 refreshButtonOriginalScale = Vector3.one;
    private Coroutine refreshButtonPulseCoroutine;
    private string undoGridData = "";
    private string[] undoSlotData = new string[3];
    private int undoScore;
    private bool hasUndoSnapshot;
    
    private Transform[] slotTransforms = new Transform[3];
    private GameObject[,] gridStatus;
    private GameObject[,] gridCells; // Arka plan hucreleri
    private List<Vector2Int> currentlyHighlighted = new List<Vector2Int>();
    public Color highlightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color invalidHighlightColor = new Color(1f, 0.3f, 0.3f, 0.5f); // Kirmizi vurgu rengi (Gecersiz yerlesim)
    private Dictionary<string, Vector2Int> cellNameToGridPos = new Dictionary<string, Vector2Int>();
    private Coroutine perfectNeonCoroutine;
    private Coroutine borderPulseCoroutine;
    private Coroutine edgeFlashCoroutine;
    private Coroutine perfectBurstCoroutine;
    private Coroutine perfectTitleCoroutine;
    
    // Oyun Durumu
    public bool isGameActive = true;
    
    // Slot transform'unu public olarak erisilebilir yap
    public Transform GetSlotTransform(int index)
    {
        if (index >= 0 && index < 3)
        {
            return slotTransforms[index];
        }
        return null;
    }

    private List<Vector2Int[]> easyShapes = new List<Vector2Int[]>()
    {
        // 1. Tek kare (1x1)
        new Vector2Int[] { new Vector2Int(0,0) },
        
        // 2. 1x2 Yatay Ikili
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0) },
        
        // 3. 2x1 Dikey Ikili
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1) },
        
        // 4. Küçük L (2x2 eksi 1)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) },

        // 5. Çapraz İkili (YENI)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,1) },

        // 6. Ters Küçük L (YENI - Basic)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) }
    };

    private List<Vector2Int[]> mediumShapes = new List<Vector2Int[]>()
    {
        // 1. 2x2 Kare
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
        
        // 2. I şekli (3 dikey)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) },
        
        // 3. I şekli (3 yatay)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
        
        // 4. L şekli (sağa dönük)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2) },
        
        // 5. L şekli (ters)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) },
        
        // 6. T şekli
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },
        
        // 7. Z şekli
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }
    };

    private List<Vector2Int[]> hardShapes = new List<Vector2Int[]>()
    {
        // 1. U şekli
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,0) },
        
        // 2. 2x3 Dikdörtgen
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,2), new Vector2Int(1,2) },
        
        // 3. 3x2 Dikdörtgen
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },

        // --- YENI BUYUK SEKILLER ---
        // 13. 4x1 Yatay (I sekli uzun)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },
        
        // 14. 1x4 Dikey (I sekli uzun)
        new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3) },
        
        // 15. 4x2 Blok (Buyuk Dikdortgen) - KALDIRILDI (SPAM ONLEME)
        // new Vector2Int[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), 
        //                    new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1) },
                           
        // 16. Arti (+) Sekli
        new Vector2Int[] { new Vector2Int(1,0), 
                           new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), 
                           new Vector2Int(1,2) }
    };

    void Start()
    {
        gridStatus = new GameObject[rows, columns];
        GenerateLevel();
        
        // Yan Hak Butonu
        if (refreshButton != null)
        {
            refreshButtonOriginalScale = refreshButton.transform.localScale;
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            UpdateRefreshButtonState();
        }

        if (undoButton != null)
        {
            undoButton.onClick.AddListener(OnUndoButtonClicked);
            UpdateUndoButtonState();
        }
    }

    public void GenerateLevel()
    {
        isGameActive = true; // Oyunu aktiflestir

        // Onceki slot referanslarini temizle
        for (int i = 0; i < slotTransforms.Length; i++) slotTransforms[i] = null;

        // Tum cocuk objeleri temizle (Grid, Slotlar, Bloklar)
        // Tersten dongu ile silmek daha guvenlidir
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i) != null)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        CreateBackPanel();
        CreateGrid();

        // Level Data Kontrolu ve Engellerin Yerlestirilmesi
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            PlaceLevelObstacles(LevelManager.Instance.currentLevelData);
        }
        
        // Eger kayitli oyun varsa onu yukle
        if (HasSavedGame())
        {
            LoadSavedGame();
        }

        CreateSlots();
        
        // Slotlari yukle veya yeni olustur
        if (HasSavedSlots())
        {
            LoadSavedSlots();
        }
        else
        {
            SpawnNewBlocks();
        }
        
        // --- KAMERA ORTALAMA VE SIGDIRMA ISLEMI ---
        AdjustCameraToFitGrid();
    }
    
    // Kamerayi Grid'e ve Slotlara gore otomatik ayarlar
    void AdjustCameraToFitGrid()
    {
        if (Camera.main == null) return;

        // 1. Grid'in toplam genisligini hesapla (Sagdan ve soldan biraz bosluk payi (padding) birakarak)
        float totalGridWidth = (rows * cellSize) + ((rows - 1) * spacing);
        
        // Ekranda gridin yanlarinda kalmasini istedigimiz bosluk (margin) - ornegin 1 birim
        float margin = 1.0f; 
        float targetWidth = totalGridWidth + margin;

        // 2. Ekranin en-boy oranini (aspect ratio) bul
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // 3. Kameranin orthographic size'ini (dikey gorus alani) hesapla
        // OrthographicSize = (Ekran Genisligi / 2) / AspectRatio
        float targetOrthographicSize = (targetWidth / 2f) / screenAspect;

        // 4. Eger hesaplanan size mevcut size'dan buyukse (yani ekran dar ise) kamerayi geriye cek (size'i buyut)
        // Kucukse (tablet gibi genis ekran) mevcut size'da kalabilir veya orantisal kuculebilir.
        // Genelde minimum bir deger (ornegin 5) tutmak iyidir.
        if (targetOrthographicSize > Camera.main.orthographicSize)
        {
            Camera.main.orthographicSize = targetOrthographicSize;
        }
    }
    
    // --- OYUN KAYIT SISTEMI (SAVE SYSTEM) ---
    // private const string SAVE_KEY_GRID = "SavedGrid_"; // REMOVED
    // private const string SAVE_KEY_HAS_GAME = "HasSavedGame"; // REMOVED
    
    // Dinamik Kayit Anahtari Olusturucu
    private string GetSaveKey(string suffix)
    {
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            // Level Modu: "Level_5_Grid" gibi
            return $"Level_{LevelManager.Instance.currentLevelData.levelNumber}_{suffix}";
        }
        else
        {
            // Klasik Mod: "Classic_Grid" gibi
            return $"Classic_{suffix}";
        }
    }
    
    public bool HasSavedGame()
    {
        return PlayerPrefs.GetInt(GetSaveKey("HasSavedGame"), 0) == 1;
    }
    
    public bool HasSavedSlots()
    {
        return PlayerPrefs.HasKey(GetSaveKey("Slot_0"));
    }

    public void SaveGame()
    {
        try
        {
            // Kaydetmeden once grid durumunu guncelle (Stale referanslari temizle)
            SyncGridStatus();

            // 1. Grid Durumunu Kaydet
            string gridData = "";
            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    if (gridStatus[x, y] != null)
                    {
                        BlockUnit bu = gridStatus[x, y].GetComponent<BlockUnit>();
                        Color c = Color.white;
                        SpriteRenderer sr = gridStatus[x, y].GetComponent<SpriteRenderer>();
                        if (sr != null) c = sr.color;
                        
                        gridData += $"{x},{y},{ColorUtility.ToHtmlStringRGBA(c)}|";
                    }
                }
            }
            
            PlayerPrefs.SetString(GetSaveKey("Grid"), gridData);
            PlayerPrefs.SetInt(GetSaveKey("HasSavedGame"), 1);
            
            // 2. Slotlardaki Bloklari Kaydet
            SaveSlots();
            
            // 3. Skoru Kaydet
            if (ScoreManager.Instance != null)
            {
                PlayerPrefs.SetInt(GetSaveKey("Score"), ScoreManager.Instance.GetCurrentScore());
            }
            
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError("SaveGame Hatasi: " + e.Message);
        }
    }

    string SerializeGridData()
    {
        SyncGridStatus();
        string gridData = "";
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] != null)
                {
                    Color c = Color.white;
                    SpriteRenderer sr = gridStatus[x, y].GetComponent<SpriteRenderer>();
                    if (sr != null) c = sr.color;
                    gridData += $"{x},{y},{ColorUtility.ToHtmlStringRGBA(c)}|";
                }
            }
        }
        return gridData;
    }

    string SerializeSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return "";
        Transform slot = slotTransforms[slotIndex];
        if (slot == null || slot.childCount == 0) return "";

        DraggableBlock db = slot.GetComponentInChildren<DraggableBlock>();
        if (db == null || db.originalShape == null) return "";

        string shapeStr = "";
        foreach (var vec in db.originalShape)
        {
            shapeStr += $"{vec.x},{vec.y};";
        }

        Color c = Color.white;
        SpriteRenderer sr = db.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) c = sr.color;

        return $"{shapeStr}#{ColorUtility.ToHtmlStringRGBA(c)}";
    }

    public void CaptureUndoSnapshot()
    {
        undoGridData = SerializeGridData();
        for (int i = 0; i < 3; i++)
        {
            undoSlotData[i] = SerializeSlotData(i);
        }
        undoScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
        hasUndoSnapshot = true;
        UpdateUndoButtonState();
    }
    
    void SaveSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform slot = slotTransforms[i];
            if (slot != null && slot.childCount > 0)
            {
                DraggableBlock db = slot.GetComponentInChildren<DraggableBlock>();
                if (db != null && db.originalShape != null)
                {
                    string shapeStr = "";
                    foreach(var vec in db.originalShape)
                    {
                        shapeStr += $"{vec.x},{vec.y};";
                    }
                    
                    Color c = Color.white;
                    SpriteRenderer sr = db.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) c = sr.color;
                    
                    PlayerPrefs.SetString(GetSaveKey($"Slot_{i}"), $"{shapeStr}#{ColorUtility.ToHtmlStringRGBA(c)}");
                }
            }
            else
            {
                PlayerPrefs.DeleteKey(GetSaveKey($"Slot_{i}"));
            }
        }
    }
    
    public void LoadSavedGame()
    {
        string gridData = PlayerPrefs.GetString(GetSaveKey("Grid"), "");
        if (string.IsNullOrEmpty(gridData)) return;
        
        string[] cells = gridData.Split('|');
        foreach (var cellData in cells)
        {
            if (string.IsNullOrEmpty(cellData)) continue;
            
            string[] parts = cellData.Split(',');
            if (parts.Length >= 3)
            {
                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                string colorHex = parts[2];
                
                Color color;
                if (ColorUtility.TryParseHtmlString("#" + colorHex, out color))
                {
                    PlaceBlockAt(x, y, color);
                }
            }
        }
        
        // Skoru yukle
        if (ScoreManager.Instance != null)
        {
            int savedScore = PlayerPrefs.GetInt(GetSaveKey("Score"), 0);
            ScoreManager.Instance.SetScore(savedScore);
        }
    }
    
    void LoadSavedSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            string key = GetSaveKey($"Slot_{i}");
            if (PlayerPrefs.HasKey(key))
            {
                string data = PlayerPrefs.GetString(key);
                string[] mainParts = data.Split('#');
                string shapeData = mainParts[0];
                string colorHex = mainParts.Length > 1 ? mainParts[1] : "FFFFFF";
                
                Color color;
                ColorUtility.TryParseHtmlString("#" + colorHex, out color);
                
                List<Vector2Int> shape = new List<Vector2Int>();
                string[] vecs = shapeData.Split(';');
                foreach(var v in vecs)
                {
                    if (string.IsNullOrEmpty(v)) continue;
                    string[] xy = v.Split(',');
                    if (xy.Length == 2)
                    {
                        shape.Add(new Vector2Int(int.Parse(xy[0]), int.Parse(xy[1])));
                    }
                }
                
                CreateBlockInSlot(i, shape, color);
            }
        }
    }
    
    void PlaceBlockAt(int x, int y, Color color)
    {
        if (x < 0 || x >= rows || y < 0 || y >= columns) return;
        
        // Eger zaten doluysa (engel varsa) elleme
        if (gridStatus[x, y] != null) return;
        
        GameObject block = Instantiate(blockBasePrefab, transform); // Prefab farkli olabilir ama idare eder
        // Sadece tek bir parca (BlockUnit) lazim aslinda
        // blockBasePrefab icinde script var mi? Muhtemelen DraggableBlock var, onu kaldiralim
        Destroy(block.GetComponent<DraggableBlock>());
        Destroy(block.GetComponent<BoxCollider2D>());
        
        // Tek bir kare haline getirelim (Prefab karmasik olabilir, basit cellPrefab kullanalim mi?)
        // CellPrefab kullanmak daha mantikli
        Destroy(block);
        
        GameObject cellBlock = Instantiate(cellPrefab, transform);
        cellBlock.transform.localPosition = gridCells[x, y].transform.localPosition;
        cellBlock.transform.localScale = Vector3.one * cellSize;
        
        SetVisuals(cellBlock, color, 5);
        
        if (cellBlock.GetComponent<BoxCollider2D>() == null)
            cellBlock.AddComponent<BoxCollider2D>();
            
        BlockUnit bu = cellBlock.AddComponent<BlockUnit>();
        // bu.gridLayer = LayerMask.GetMask("Grid"); // KALDIRILDI
        
        gridStatus[x, y] = cellBlock;
    }

    void RestoreGridFromData(string gridData)
    {
        if (string.IsNullOrEmpty(gridData)) return;

        string[] cells = gridData.Split('|');
        foreach (var cellData in cells)
        {
            if (string.IsNullOrEmpty(cellData)) continue;

            string[] parts = cellData.Split(',');
            if (parts.Length < 3) continue;

            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            Color color;
            if (ColorUtility.TryParseHtmlString("#" + parts[2], out color))
            {
                PlaceBlockAt(x, y, color);
            }
        }
    }

    void RestoreSlotFromData(int slotIndex, string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        string[] mainParts = data.Split('#');
        string shapeData = mainParts[0];
        string colorHex = mainParts.Length > 1 ? mainParts[1] : "FFFFFF";

        Color color;
        ColorUtility.TryParseHtmlString("#" + colorHex, out color);

        List<Vector2Int> shape = new List<Vector2Int>();
        string[] vecs = shapeData.Split(';');
        foreach (var v in vecs)
        {
            if (string.IsNullOrEmpty(v)) continue;
            string[] xy = v.Split(',');
            if (xy.Length == 2)
            {
                shape.Add(new Vector2Int(int.Parse(xy[0]), int.Parse(xy[1])));
            }
        }

        CreateBlockInSlot(slotIndex, shape, color);
    }

    void RestoreUndoSnapshot()
    {
        if (!hasUndoSnapshot) return;

        ClearHighlight();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] != null)
                {
                    SafeDestroy(gridStatus[x, y]);
                    gridStatus[x, y] = null;
                }
            }
        }

        for (int i = 0; i < 3; i++)
        {
            if (slotTransforms[i] == null) continue;
            for (int j = slotTransforms[i].childCount - 1; j >= 0; j--)
            {
                Destroy(slotTransforms[i].GetChild(j).gameObject);
            }
        }

        RestoreGridFromData(undoGridData);

        for (int i = 0; i < 3; i++)
        {
            RestoreSlotFromData(i, undoSlotData[i]);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(undoScore);
        }

        hasUndoSnapshot = false;
        SaveGame();
        UpdateUndoButtonState();
    }
    
    void CreateBlockInSlot(int slotIndex, List<Vector2Int> shape, Color color)
    {
        Transform slotTr = slotTransforms[slotIndex];
        GameObject newBlock = Instantiate(blockBasePrefab, slotTr);
        newBlock.transform.localPosition = Vector3.zero;
        
        DraggableBlock db = newBlock.GetComponent<DraggableBlock>();
        if (db == null) db = newBlock.AddComponent<DraggableBlock>();
        
        db.slotScale = blockInSlotScale;
        db.dragScale = 1.0f;
        db.originalShape = shape; // Sekli ata
        
        // Cocuklari temizle ve sekli olustur
        foreach(Transform child in newBlock.transform) Destroy(child.gameObject);
        
        // Merkezi hesapla (Center Offset)
        float minX = 10, maxX = -10, minY = 10, maxY = -10;
        foreach (var c in shape)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }
        Vector3 centerOffset = new Vector3((maxX + minX) * (cellSize + spacing) / 2f, (maxY + minY) * (cellSize + spacing) / 2f, 0);

        foreach(Vector2Int pos in shape)
        {
            GameObject part = Instantiate(cellPrefab, newBlock.transform);
            // DUZELTME: Spacing ve CellSize hesabi eklendi, CenterOffset eklendi
            part.transform.localPosition = new Vector3(pos.x * (cellSize + spacing), pos.y * (cellSize + spacing), 0) - centerOffset;
            part.transform.localScale = Vector3.one * cellSize;
            
            SetVisuals(part, color, 0);
            part.AddComponent<BlockUnit>();
            part.AddComponent<BoxCollider2D>(); // Dokunma icin
        }
        
        // Mouse handler'lari ayarla
        db.SetupChildMouseEvents();
    }
    
    // Start method removed to fix duplication error
    
    // Uygulama durdugunda veya kapandiginda kaydet
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGame();
    }
    
    void OnApplicationQuit()
    {
        SaveGame();
    }
    
    // Level tamamlandiginda kaydi sil (Yeni level temiz baslasin)
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(GetSaveKey("Grid"));
        PlayerPrefs.DeleteKey(GetSaveKey("HasSavedGame"));
        PlayerPrefs.DeleteKey(GetSaveKey("Score"));
        for(int i=0; i<3; i++) PlayerPrefs.DeleteKey(GetSaveKey($"Slot_{i}"));
        PlayerPrefs.Save();
    }
    
    void PlaceLevelObstacles(LevelData data)
    {
        if (data.preFilledBlocks == null) return;
        
        // Islenen bloklari takip et
        HashSet<Vector2Int> processedBlocks = new HashSet<Vector2Int>();
        
        // Helper Action for creation to avoid duplication
        System.Action<Vector2Int, Color> createBlock = (pos, color) => {
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns)
            {
                GameObject obstacle = Instantiate(cellPrefab, transform);
                obstacle.name = "Obstacle_Block";
                obstacle.transform.localPosition = gridCells[pos.x, pos.y].transform.localPosition;
                obstacle.transform.localScale = Vector3.one * cellSize;
                
                SetVisuals(obstacle, color, 5);
                
                if (obstacle.GetComponent<BoxCollider2D>() == null)
                    obstacle.AddComponent<BoxCollider2D>();
                
                BlockUnit bu = obstacle.AddComponent<BlockUnit>();
                // bu.gridLayer = LayerMask.GetMask("Grid"); // KALDIRILDI
                
                gridStatus[pos.x, pos.y] = obstacle;
            }
        };

        // 1. Gruplu engeller (Ayni renk - Kullanici istegi: Buyuk butun blok seklinde)
        if (data.obstacleGroups != null)
        {
            foreach (var group in data.obstacleGroups)
            {
                // Grup rengi sec
                Color baseColor = blockColors[Random.Range(0, blockColors.Length)];
                float h, s, v;
                Color.RGBToHSV(baseColor, out h, out s, out v);
                s = Mathf.Clamp01(s * 1.2f);
                v = Mathf.Clamp01(v * 1.2f);
                Color vividColor = Color.HSVToRGB(h, s, v);
                
                foreach (Vector2Int pos in group)
                {
                    createBlock(pos, vividColor);
                    processedBlocks.Add(pos);
                }
            }
        }
        
        // 2. Kalan tekil engeller
        foreach (Vector2Int pos in data.preFilledBlocks)
        {
            if (processedBlocks.Contains(pos)) continue;
            
            // Rastgele renk
            Color baseColor = blockColors[Random.Range(0, blockColors.Length)];
            float h, s, v;
            Color.RGBToHSV(baseColor, out h, out s, out v);
            s = Mathf.Clamp01(s * 1.2f);
            v = Mathf.Clamp01(v * 1.2f);
            Color vividColor = Color.HSVToRGB(h, s, v);
            
            createBlock(pos, vividColor);
        }
    }
    
    public void ShowLevelComplete()
    {
        // Oyunu durdur (Tiklamalari engelle)
        isGameActive = false;

        // KAZANILAN LEVELI HEMEN KAYDET (Animasyon sirasinda cikilirsa kaybolmasin)
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UnlockNextLevel();
        }
        
        // Level tamamlandi, kaydi temizle ki tekrar girerse ayni seviye eski grid ile baslamasin
        ClearSave();

        // Level Complete Yazisi
        if (gameOverTextPrefab != null)
        {
            GameObject textObj = Instantiate(gameOverTextPrefab, transform.parent); // Grid'in parenti (Canvas/Panel)
            textObj.transform.localPosition = Vector3.zero;
            
            // Text componentini bul ve "LEVEL COMPLETE!" yaz
            // Yaziyi biraz kucult (Kullanici istegi)
            textObj.transform.localScale = Vector3.one * 0.6f; // Daha da kucultuldu (0.8 -> 0.6)

            var textComp = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null) textComp.text = "LEVEL COMPLETE!";
            else 
            {
                var textLegacy = textObj.GetComponentInChildren<Text>();
                if (textLegacy != null) textLegacy.text = "LEVEL COMPLETE!";
            }
            
            // Sonraki levele gecis
            StartCoroutine(WaitAndNextLevel(textObj));
        }
        else
        {
            // Prefab yoksa bile bekle, kullanici "LEVEL COMPLETE" yazisini gormek istiyor.
            // Eger prefab yoksa konsola uyari ver ama yine de bekle.
            Debug.LogWarning("GridManager: gameOverTextPrefab atanmamis! Level Complete yazisi gosterilemiyor.");
            StartCoroutine(WaitAndNextLevel(null));
        }
    }
    
    IEnumerator WaitAndNextLevel(GameObject textObj)
    {
        yield return new WaitForSeconds(2.0f); // 2.0 saniye "LEVEL COMPLETE!" goster (Sure uzatildi)
        
        // Simdi siradaki leveli goster "LEVEL X"
        if (textObj != null)
        {
            int nextLevel = 1;
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
            {
                nextLevel = LevelManager.Instance.currentLevelData.levelNumber + 1;
            }
            
            var textComp = textObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null) textComp.text = "LEVEL " + nextLevel;
            else 
            {
                var textLegacy = textObj.GetComponentInChildren<Text>();
                if (textLegacy != null) textLegacy.text = "LEVEL " + nextLevel;
            }
        }
        
        yield return new WaitForSeconds(2.0f); // 2.0 saniye de Level ismini goster

        if (textObj != null) Destroy(textObj);
        if (LevelManager.Instance != null) LevelManager.Instance.LoadNextLevel();
    }

    void CreateBackPanel()
    {
        float totalWidth = (rows * cellSize) + ((rows - 1) * spacing);
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        GameObject panel = Instantiate(panelPrefab, transform);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = new Vector3(totalWidth + panelPadding, totalHeight + panelPadding, 1);
        SetVisuals(panel, panelColor, -2);
    }

    void CreateGrid()
    {
        gridCells = new GameObject[rows, columns]; // Array'i baslat

        float totalWidth = (rows * cellSize) + ((rows - 1) * spacing);
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        Vector3 startPos = new Vector3(-totalWidth / 2f + cellSize / 2f, -totalHeight / 2f + cellSize / 2f, 0);

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                Vector3 pos = startPos + new Vector3(x * (cellSize + spacing), y * (cellSize + spacing), 0);

                GameObject cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell_{x}_{y}";
                // Layer atamasini kaldirdik - Isim kontrolu yapacagiz
                cell.transform.localPosition = pos;
                cell.transform.localScale = Vector3.one * cellSize;
                SetVisuals(cell, cellColor, 0);
                
                // BoxCollider2D Ekle (Grid algilama icin)
                BoxCollider2D col = cell.GetComponent<BoxCollider2D>();
                if (col == null) col = cell.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                
                // Hucreyi kaydet
                gridCells[x, y] = cell;
                
                // Grid pozisyonunu kaydet
                cellNameToGridPos[cell.name] = new Vector2Int(x, y);

                if (useOutline)
                {
                    GameObject outline = Instantiate(cellPrefab, transform);
                    outline.transform.localPosition = pos;
                    outline.transform.localScale = Vector3.one * (cellSize * outlineSize);
                    SetVisuals(outline, outlineColor, -1);
                }
            }
        }
    }

    void CreateSlots()
    {
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        float gridBottomY = -totalHeight / 2f;

        for (int i = 0; i < 3; i++)
        {
            Vector3 slotPos = new Vector3((i - 1) * slotSpacing, gridBottomY - bottomOffset, 0);
            GameObject slot = Instantiate(spawnSlotPrefab, transform);
            slot.name = $"Slot_{i}";
            slot.transform.localPosition = slotPos;
            
            // Kullanicinin geri bildirimi: "Slot kutulari cok buyuk, yanlara tasiyor"
            // Orjinal boyut: Vector3.one * slotScale. Simdi bunu biraz kucultuyoruz.
            slot.transform.localScale = Vector3.one * (slotScale * 0.85f); // %15 kucultuldu
            
            slotTransforms[i] = slot.transform;

            SetVisuals(slot, slotNormalColor, 1);
            
            // Slot collider'ini trigger yap (bloklari engellemesin)
            BoxCollider2D slotCollider = slot.GetComponent<BoxCollider2D>();
            if (slotCollider == null)
            {
                slotCollider = slot.AddComponent<BoxCollider2D>();
            }
            slotCollider.isTrigger = true; // ONEMLI: Trigger yap ki bloklari engellemesin
            slotCollider.enabled = false; // Veya tamamen kapat (SlotBehavior zaten var)

            SlotBehavior behavior = slot.AddComponent<SlotBehavior>();
            behavior.normalColor = slotNormalColor;
            behavior.hoverColor = slotHoverColor;
        }
    }

    public void SpawnNewBlocks()
    {
        // Tum slotlar bos mu kontrol et
        bool allSlotsEmpty = true;
        for (int i = 0; i < 3; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                allSlotsEmpty = false;
                break;
            }
        }
        
        // Eger tum slotlar bos degilse, spawn etme
        if (!allSlotsEmpty) return;
        
        // Tum slotlar bos, yeni 3 obje spawn et
        for (int i = 0; i < 3; i++)
        {
            // --- AKILLI SPAWN SISTEMI (SMART SPAWN) ---
            // Rastgele bir sekil sec, ama gride sigip sigmadigini kontrol et.
            // Boylece oyunun tikilma ihtimali duser ve daha buyuk bloklar guvenle gelebilir.
            
            Vector2Int[] randomShape = null;
            int attempts = 0;
            int maxAttempts = 50; // Iyi bir sekil bulmak icin kac kere denesin?
            
            // --- LEVEL ZORLUK AYARI ---
            int currentLevelNum = 0;
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
            {
                currentLevelNum = LevelManager.Instance.currentLevelData.levelNumber;
            }
            
            while (attempts < maxAttempts)
            {
                float chance = Random.value;
                List<Vector2Int[]> targetList;
                
                if (currentLevelNum > 0 && currentLevelNum <= 10)
                {
                    // ILK 10 LEVEL: %80 Kolay, %20 Orta (Asla zor gelmez)
                    if (chance < 0.80f) targetList = easyShapes;
                    else targetList = mediumShapes;
                }
                else if (currentLevelNum > 10 && currentLevelNum <= 20)
                {
                    // 11-20 LEVELLER: %60 Kolay, %30 Orta, %10 Zor
                    if (chance < 0.60f) targetList = easyShapes;
                    else if (chance < 0.90f) targetList = mediumShapes;
                    else targetList = hardShapes;
                }
                else
                {
                    // SONSUZ MOD VEYA 20+ LEVELLER: Orijinal zorluk (%25 Kolay, %45 Orta, %30 Zor)
                    if (chance < 0.25f) targetList = easyShapes;
                    else if (chance < 0.70f) targetList = mediumShapes;
                    else targetList = hardShapes;
                }
                
                Vector2Int[] candidate = targetList[Random.Range(0, targetList.Count)];
                
                // Gride sigiyor mu?
                if (CanShapeFit(candidate))
                {
                    randomShape = candidate;
                    break; // Bulduk!
                }
                
                attempts++;
            }
            
            // Eger uygun sekil bulamadiysak (Grid cok dolu), Easy listesinden sigani bulmaya calis (Fallback)
            if (randomShape == null)
            {
                // Listeyi karistirip dene ki hep ayni gelmesin
                List<Vector2Int[]> shuffledEasy = new List<Vector2Int[]>(easyShapes);
                for (int k = 0; k < shuffledEasy.Count; k++) {
                     Vector2Int[] temp = shuffledEasy[k];
                     int r = Random.Range(k, shuffledEasy.Count);
                     shuffledEasy[k] = shuffledEasy[r];
                     shuffledEasy[r] = temp;
                }

                foreach (var shape in shuffledEasy)
                {
                    if (CanShapeFit(shape))
                    {
                        randomShape = shape;
                        break;
                    }
                }
            }
            
            // Hala yoksa (Grid tikabasa), en kucuk parcayi (1x1) ver
            if (randomShape == null)
            {
                randomShape = easyShapes[0]; 
            }

            // Blogu olustur
            CreateBlockInSlot(i, randomShape);
        }
    }

    // World pozisyonundan grid koordinatına çevir
    public Vector2Int? WorldPosToGrid(Vector3 worldPos)
    {
        float totalWidth = (rows * cellSize) + ((rows - 1) * spacing);
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        Vector3 startPos = new Vector3(-totalWidth / 2f + cellSize / 2f, -totalHeight / 2f + cellSize / 2f, 0);
        
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 relativePos = localPos - startPos;
        
        int x = Mathf.RoundToInt(relativePos.x / (cellSize + spacing));
        int y = Mathf.RoundToInt(relativePos.y / (cellSize + spacing));
        
        if (x >= 0 && x < rows && y >= 0 && y < columns)
            return new Vector2Int(x, y);
        return null;
    }
    
    // Grid koordinatından world pozisyonuna çevir
    public Vector3 GridToWorldPos(Vector2Int gridPos)
    {
        float totalWidth = (rows * cellSize) + ((rows - 1) * spacing);
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        Vector3 startPos = new Vector3(-totalWidth / 2f + cellSize / 2f, -totalHeight / 2f + cellSize / 2f, 0);
        
        Vector3 localPos = startPos + new Vector3(gridPos.x * (cellSize + spacing), gridPos.y * (cellSize + spacing), 0);
        return transform.TransformPoint(localPos);
    }
    
    // Grid pozisyonu dolu mu kontrol et
    public bool IsGridOccupied(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.x < rows && gridPos.y >= 0 && gridPos.y < columns)
        {
            return gridStatus[gridPos.x, gridPos.y] != null;
        }
        return false;
    }
    
    // Blok yerleştirme - gridStatus'e kaydet
    public void PlaceBlockAtGrid(Vector2Int gridPos, GameObject blockUnit)
    {
        if (gridPos.x >= 0 && gridPos.x < rows && gridPos.y >= 0 && gridPos.y < columns)
        {
            gridStatus[gridPos.x, gridPos.y] = blockUnit;
        }
    }

    // Grid vurgulama (Preview)
    public void HighlightPlacement(List<Vector2Int> positions, bool isValid)
    {
        ClearHighlight(); // Oncekileri temizle
        
        Color targetColor = isValid ? highlightColor : invalidHighlightColor;

        // 1) Normal yerlesecek hucreleri vurgula
        foreach (var pos in positions)
        {
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns)
            {
                if (gridCells[pos.x, pos.y] != null)
                {
                    SetVisuals(gridCells[pos.x, pos.y], targetColor, 0);
                    currentlyHighlighted.Add(pos);
                }
            }
        }

        // 2) EGER yerlesim gecerli ise, bu yerlesim SONRASI patlayacak tum satir/sutunlari da onceden gri goster
        if (isValid)
        {
            // Simule icin: Mevcut grid durumunun kopyasini olustur
            GameObject[,] simGrid = new GameObject[rows, columns];
            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    simGrid[x, y] = gridStatus[x, y];
                }
            }

            // Blok yerlestikten sonra dolu olacak hucreleri isaretle
            foreach (var pos in positions)
            {
                if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns)
                {
                    simGrid[pos.x, pos.y] = gridCells[pos.x, pos.y];
                }
            }

            // Simule edilen grid uzerinde dolu satir/sutunlari bul
            List<int> fullRows = new List<int>();
            List<int> fullColumns = new List<int>();

            for (int y = 0; y < columns; y++)
            {
                bool isFull = true;
                for (int x = 0; x < rows; x++)
                {
                    if (simGrid[x, y] == null)
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull) fullRows.Add(y);
            }

            for (int x = 0; x < rows; x++)
            {
                bool isFull = true;
                for (int y = 0; y < columns; y++)
                {
                    if (simGrid[x, y] == null)
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull) fullColumns.Add(x);
            }

            // Bu satir/sutunlardaki TUM hucreleri gri vurgula
            foreach (int row in fullRows)
            {
                for (int x = 0; x < rows; x++)
                {
                    Vector2Int pos = new Vector2Int(x, row);
                    if (gridCells[x, row] != null)
                    {
                        SetVisuals(gridCells[x, row], highlightColor, 0);
                        if (!currentlyHighlighted.Contains(pos))
                            currentlyHighlighted.Add(pos);
                    }
                }
            }

            foreach (int col in fullColumns)
            {
                for (int y = 0; y < columns; y++)
                {
                    Vector2Int pos = new Vector2Int(col, y);
                    if (gridCells[col, y] != null)
                    {
                        SetVisuals(gridCells[col, y], highlightColor, 0);
                        if (!currentlyHighlighted.Contains(pos))
                            currentlyHighlighted.Add(pos);
                    }
                }
            }
        }
    }
    
    // Vurguyu temizle
    public void ClearHighlight()
    {
        foreach (var pos in currentlyHighlighted)
        {
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns)
            {
                if (gridCells[pos.x, pos.y] != null)
                {
                    SetVisuals(gridCells[pos.x, pos.y], cellColor, 0);
                }
            }
        }
        currentlyHighlighted.Clear();
    }

    public void CheckForLines()
    {
        SyncGridStatus();
        CheckForLinesRecursive(0);
    }

    // Guvenli yok etme fonksiyonu: Unity Destroy gecikmeli calistigi icin
    // objeyi hemen pasif hale getirip, "yok edilmis" gibi davranilmasini saglar.
    private void SafeDestroy(GameObject obj)
    {
        if (obj == null) return;
        
        // 1. GridStatus senkronizasyonu icin "Olu" olarak isaretle (Active = false)
        obj.SetActive(false);
        
        // 2. Unity'nin Destroy kuyruguna ekle
        Destroy(obj);
    }

    public void SyncGridStatus()
    {
        // 1. Tabloyu temizle (Stale referansları temizle)
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                gridStatus[x, y] = null;
            }
        }
        
        // 2. Sahnedeki blokları tara ve tabloya işle
        // Sadece GridManager'ın altındaki PLACED (yerleşmiş) bloklar BlockUnit içerir
        foreach (Transform child in transform)
        {
            // Guvenlik: Pasif veya yok edilmek uzere olan objeleri atla
            if (!child.gameObject.activeInHierarchy) continue;

            BlockUnit bu = child.GetComponent<BlockUnit>();
            if (bu != null)
            {
                // Eger blok patlama surecindeyse (isDying), gridde yer kaplamamali
                // (Cunku gorsel olarak var olsa bile, mantiksal olarak yok sayilmali)
                if (bu.isDying) continue;

                Vector2Int? gridPos = WorldPosToGrid(child.position);
                if (gridPos.HasValue)
                {
                    // Ayni hucrede birden fazla blok olmamali, varsa hata/cakisma vardir
                    // Ama biz sonuncuyu kabul edelim veya loglayalim
                    gridStatus[gridPos.Value.x, gridPos.Value.y] = child.gameObject;
                }
            }
        }
    }
    
    private void CheckForLinesRecursive(int depth)
    {
        // Sonsuz döngüyü önle
        if (depth > 10) return;
        
        List<int> fullRows = new List<int>();
        List<int> fullColumns = new List<int>();
        
        // Dolu satırları bul
        for (int y = 0; y < columns; y++)
        {
            bool isFull = true;
            for (int x = 0; x < rows; x++)
            {
                if (gridStatus[x, y] == null)
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullRows.Add(y);
        }
        
        // Dolu sütunları bul
        for (int x = 0; x < rows; x++)
        {
            bool isFull = true;
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] == null)
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) fullColumns.Add(x);
        }
        
        // Dolu satır ve sütunları temizle
        if (fullRows.Count > 0 || fullColumns.Count > 0)
        {
            HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
            
            // Dolu satırları işaretle
            foreach (int row in fullRows)
            {
                for (int x = 0; x < rows; x++)
                {
                    cellsToClear.Add(new Vector2Int(x, row));
                }
            }
            
            // Dolu sütunları işaretle
            foreach (int col in fullColumns)
            {
                for (int y = 0; y < columns; y++)
                {
                    cellsToClear.Add(new Vector2Int(col, y));
                }
            }
            
            // Patlama efekti ile temizle (sadece ilk seferde coroutine baslat)
            if (depth == 0)
            {
                StartCoroutine(ExplodeAndClear(cellsToClear, fullRows, fullColumns));
                return; // Coroutine devam edecek
            }
            else
            {
                // Cascade effect icin direkt temizle (efekt yok)
                foreach (Vector2Int pos in cellsToClear)
                {
                    if (gridStatus[pos.x, pos.y] != null)
                    {
                        SafeDestroy(gridStatus[pos.x, pos.y]);
                        gridStatus[pos.x, pos.y] = null;
                    }
                }
                DropBlocksAfterClear(fullRows, fullColumns);
                CheckForLinesRecursive(depth + 1);
            }
            
            // Yeni bloklar spawn et (sadece ilk seferde)
            if (depth == 0)
            {
                SpawnNewBlocks();
            }
            
            // Tekrar kontrol et (cascade effect için)
            CheckForLinesRecursive(depth + 1);
        }
    }
    
    void DropBlocksAfterClear(List<int> clearedRows, List<int> clearedColumns)
    {
        // Satır temizlendiyse, üstteki blokları aşağı kaydır
        if (clearedRows.Count > 0)
        {
            clearedRows.Sort();
            for (int i = clearedRows.Count - 1; i >= 0; i--)
            {
                int clearedRow = clearedRows[i];
                int dropAmount = 1;
                
                // Aynı anda birden fazla satır temizlendiyse drop miktarını artır
                for (int j = i - 1; j >= 0; j--)
                {
                    if (clearedRows[j] == clearedRow - (i - j))
                        dropAmount++;
                }
                
                // Bu satırın üstündeki tüm blokları aşağı kaydır
                for (int y = clearedRow + 1; y < columns; y++)
                {
                    for (int x = 0; x < rows; x++)
                    {
                        if (gridStatus[x, y] != null)
                        {
                            int newY = y - dropAmount;
                            if (newY >= 0)
                            {
                                gridStatus[x, newY] = gridStatus[x, y];
                                gridStatus[x, y] = null;
                                
                                // Görsel pozisyonu güncelle
                                Vector3 newPos = GridToWorldPos(new Vector2Int(x, newY));
                                gridStatus[x, newY].transform.position = newPos;
                            }
                            else
                            {
                                // Grid dışına çıktı, sil
                                SafeDestroy(gridStatus[x, y]);
                                gridStatus[x, y] = null;
                            }
                        }
                    }
                }
            }
        }
        
        // Sütun temizlendiyse, o sütundaki üstteki blokları aşağı kaydır
        if (clearedColumns.Count > 0)
        {
            foreach (int clearedCol in clearedColumns)
            {
                // Bu sütundaki tüm blokları kontrol et ve aşağı kaydır
                for (int y = columns - 1; y >= 0; y--)
                {
                    if (gridStatus[clearedCol, y] != null)
                    {
                        // Bu bloktan aşağıya doğru boş yer bul
                        int targetY = y;
                        for (int checkY = y - 1; checkY >= 0; checkY--)
                        {
                            if (gridStatus[clearedCol, checkY] == null)
                            {
                                targetY = checkY;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        // Eğer aşağı kaydırılabilirse kaydır
                        if (targetY < y)
                        {
                            gridStatus[clearedCol, targetY] = gridStatus[clearedCol, y];
                            gridStatus[clearedCol, y] = null;
                            
                            // Görsel pozisyonu güncelle
                            Vector3 newPos = GridToWorldPos(new Vector2Int(clearedCol, targetY));
                            gridStatus[clearedCol, targetY].transform.position = newPos;
                        }
                    }
                }
            }
        }
    }

    IEnumerator ExplodeAndClear(HashSet<Vector2Int> cellsToClear, List<int> fullRows, List<int> fullColumns)
    {
        // PATLAMA ANALIZI: Kombinasyon var mi?
        bool isCrossExplosion = (fullRows.Count > 0 && fullColumns.Count > 0);
        bool isMultiLineExplosion = (fullRows.Count + fullColumns.Count) > 1;
        bool isBigExplosion = isCrossExplosion || isMultiLineExplosion;

        // SKOR EFEKTI: Skor ekle
        if (ScoreManager.Instance != null)
        {
            // Temel puan: Temizlenen hücre sayısı * 10
            int scoreToAdd = cellsToClear.Count * 10;
            
            // Kombo bonusu: Eğer birden fazla satır/sütun aynı anda temizlenirse
            int totalLines = fullRows.Count + fullColumns.Count;
            if (totalLines > 1)
            {
                scoreToAdd *= totalLines; // Çarpan etkisi
            }
            
            ScoreManager.Instance.AddScore(scoreToAdd);
            
            // LEVEL WIN CHECK
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
            {
                if (ScoreManager.Instance.GetCurrentScore() >= LevelManager.Instance.currentLevelData.targetScore)
                {
                    ShowLevelComplete();
                }
            }
        }

        // KAMERA SALLAMA EFEKTI
        if (CameraShake.Instance != null)
        {
            // Buyuk patlamada daha fazla salla
            float shakeDuration = isBigExplosion ? 0.4f : 0.2f;
            float shakeMagnitude = isBigExplosion ? 0.2f : 0.1f;
            StartCoroutine(CameraShake.Instance.Shake(shakeDuration, shakeMagnitude));
        }

        // SES EFEKTI: Satir/Sutun patlama sesi
        if (SoundManager.Instance != null)
        {
            if (isBigExplosion)
                SoundManager.Instance.PlayComboExplosion();
            else
                SoundManager.Instance.PlayRowClear();
        }
        
        if (isBigExplosion)
        {
            StartCoroutine(ShowFloatingText("PERFECT!", new Color(1f, 0.9f, 0.2f, 1f)));
            TriggerPerfectComboVfx();
        }
        else
        {
            StartCoroutine(ShowFloatingText("NICE", Color.white));
            TriggerNiceComboVfx();
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.PlayComboScorePulse(isBigExplosion);
        }

        // Patlama animasyonu icin bloklari topla
        List<GameObject> blocksToExplode = new List<GameObject>();
        foreach (Vector2Int pos in cellsToClear)
        {
            if (gridStatus[pos.x, pos.y] != null)
            {
                GameObject obj = gridStatus[pos.x, pos.y];
                blocksToExplode.Add(obj);
                
                // ISARETLE: Bu blok artik "isDying". 
                // Boylece animasyon sirasinda SyncGridStatus cagrilirsa, bu bloklari grid doluymus gibi saymayacak.
                BlockUnit bu = obj.GetComponent<BlockUnit>();
                if (bu != null) bu.isDying = true;
            }
        }
        
        // Patlama animasyonu
        float elapsed = 0f;
        Vector3[] originalScales = new Vector3[blocksToExplode.Count];
        Color[] originalColors = new Color[blocksToExplode.Count];
        
        // Hedef buyukluk: Kombo varsa daha buyuk patlasin
        float targetScale = isBigExplosion ? explosionScale * 1.3f : explosionScale;
        
        for (int i = 0; i < blocksToExplode.Count; i++)
        {
            if (blocksToExplode[i] != null)
            {
                originalScales[i] = blocksToExplode[i].transform.localScale;
                SpriteRenderer sr = blocksToExplode[i].GetComponent<SpriteRenderer>();
                if (sr != null) originalColors[i] = sr.color;
            }
        }
        
        while (elapsed < explosionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / explosionDuration;
            float scale = Mathf.Lerp(1f, targetScale, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            for (int i = 0; i < blocksToExplode.Count; i++)
            {
                if (blocksToExplode[i] != null)
                {
                    blocksToExplode[i].transform.localScale = originalScales[i] * scale;
                    SpriteRenderer sr = blocksToExplode[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = originalColors[i];
                        c.a = alpha;
                        sr.color = c;
                    }
                }
            }
            yield return null;
        }
        
        // Bloklari sil
        foreach (Vector2Int pos in cellsToClear)
        {
            if (gridStatus[pos.x, pos.y] != null)
            {
                SafeDestroy(gridStatus[pos.x, pos.y]);
                gridStatus[pos.x, pos.y] = null;
            }
        }
        
        // Kisa bekleme
        yield return new WaitForSeconds(0.1f);
        
        // Üstteki blokları aşağı kaydır
        DropBlocksAfterClear(fullRows, fullColumns);
        
        // Yeni bloklar spawn et (tum slotlar bos oldugunda)
        CheckAndSpawnIfAllEmpty();
        
        // Tekrar kontrol et (cascade effect için)
        CheckForLinesRecursive(1);
    }
    
    IEnumerator ShowFloatingText(string message, Color color)
    {
        GameObject canvasObj = new GameObject("FloatingTextCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 300);
        
        text.text = message;
        text.color = color;
        
        // Font atamasi (Onemli! Font yoksa yazi gozukmez)
        if (feedbackFont != null)
        {
            text.font = feedbackFont;
        }
        else
        {
            // Fallback: Eger font atanmamissa Unity'nin varsayilan fontunu bulmaya calis
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", 50);
        }
        
        text.fontSize = 120;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);
        
        float inDuration = 0.4f;
        float outDuration = 0.6f;
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.deltaTime;
            float p = t / inDuration;
            float s = Mathf.Lerp(0.7f, 1.2f, p);
            textObj.transform.localScale = Vector3.one * s;
            yield return null;
        }
        t = 0f;
        while (t < outDuration)
        {
            t += Time.deltaTime;
            float p = t / outDuration;
            Color cText = text.color;
            cText.a = Mathf.Lerp(1f, 0f, p);
            text.color = cText;
            yield return null;
        }
        Destroy(canvasObj);
    }

    void TriggerPerfectComboVfx()
    {
        if (perfectNeonCoroutine != null)
        {
            StopCoroutine(perfectNeonCoroutine);
        }
        perfectNeonCoroutine = StartCoroutine(PlayGridSweep(perfectNeonColorA, perfectNeonColorB, perfectNeonDuration, perfectNeonIntensity));

        if (borderPulseCoroutine != null)
        {
            StopCoroutine(borderPulseCoroutine);
        }
        borderPulseCoroutine = StartCoroutine(PlayBorderPulse(perfectBorderPulseColor, perfectBorderPulseDuration));

        if (edgeFlashCoroutine != null)
        {
            StopCoroutine(edgeFlashCoroutine);
        }
        edgeFlashCoroutine = StartCoroutine(PlayEdgeFlash(perfectEdgeFlashColor, perfectEdgeFlashDuration));

    }

    void TriggerNiceComboVfx()
    {
        if (perfectNeonCoroutine != null)
        {
            StopCoroutine(perfectNeonCoroutine);
        }
        perfectNeonCoroutine = StartCoroutine(PlayGridSweep(niceNeonColorA, niceNeonColorB, niceNeonDuration, niceNeonIntensity));
    }

    IEnumerator PlayGridSweep(Color colorA, Color colorB, float duration, float intensity)
    {
        if (gridCells == null) yield break;

        SpriteRenderer[,] renderers = new SpriteRenderer[rows, columns];
        Color[,] originalColors = new Color[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridCells[x, y] == null) continue;
                SpriteRenderer sr = gridCells[x, y].GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                renderers[x, y] = sr;
                originalColors[x, y] = sr.color;
            }
        }

        float elapsed = 0f;
        float bandWidth = 0.24f;
        float widthDivisor = Mathf.Max(1f, rows - 1f);
        float heightDivisor = Mathf.Max(1f, columns - 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float sweep = Mathf.Lerp(-0.25f, 1.25f, progress);

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    SpriteRenderer sr = renderers[x, y];
                    if (sr == null) continue;

                    float normalizedX = x / widthDivisor;
                    float normalizedY = y / heightDivisor;
                    float diagonal = (normalizedX + normalizedY) * 0.5f;
                    float band = Mathf.Clamp01(1f - Mathf.Abs(diagonal - sweep) / bandWidth);
                    float strength = band * band * intensity;
                    Color neon = Color.Lerp(colorA, colorB, Mathf.PingPong(progress * 2.4f + diagonal * 1.5f, 1f));
                    Color target = Color.Lerp(originalColors[x, y], neon, strength);
                    target.a = originalColors[x, y].a;
                    sr.color = target;
                }
            }

            yield return null;
        }

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                SpriteRenderer sr = renderers[x, y];
                if (sr == null) continue;
                sr.color = originalColors[x, y];
            }
        }

        perfectNeonCoroutine = null;
    }

    IEnumerator PlayBorderPulse(Color pulseColor, float duration)
    {
        if (gridCells == null) yield break;

        List<SpriteRenderer> borderRenderers = new List<SpriteRenderer>();
        List<Color> baseColors = new List<Color>();

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                bool isBorder = x == 0 || x == rows - 1 || y == 0 || y == columns - 1;
                if (!isBorder || gridCells[x, y] == null) continue;

                SpriteRenderer sr = gridCells[x, y].GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                borderRenderers.Add(sr);
                baseColors.Add(sr.color);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);

            for (int i = 0; i < borderRenderers.Count; i++)
            {
                if (borderRenderers[i] == null) continue;
                Color c = Color.Lerp(baseColors[i], pulseColor, wave * 0.9f);
                c.a = baseColors[i].a;
                borderRenderers[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < borderRenderers.Count; i++)
        {
            if (borderRenderers[i] == null) continue;
            borderRenderers[i].color = baseColors[i];
        }

        borderPulseCoroutine = null;
    }

    IEnumerator PlayEdgeFlash(Color flashColor, float duration)
    {
        GameObject canvasObj = new GameObject("PerfectEdgeFlash");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 240;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Image[] edgeImages = new Image[4];
        Vector2[] anchorsMin =
        {
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f)
        };
        Vector2[] anchorsMax =
        {
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        Vector2[] sizeDeltas =
        {
            new Vector2(0f, 160f),
            new Vector2(0f, 160f),
            new Vector2(160f, 0f),
            new Vector2(160f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject edge = new GameObject("Edge_" + i);
            edge.transform.SetParent(canvasObj.transform, false);
            Image image = edge.AddComponent<Image>();
            image.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorsMin[i];
            rect.anchorMax = anchorsMax[i];
            rect.sizeDelta = sizeDeltas[i];
            edgeImages[i] = image;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Sin(t * Mathf.PI) * 0.35f;

            for (int i = 0; i < edgeImages.Length; i++)
            {
                if (edgeImages[i] == null) continue;
                Color c = flashColor;
                c.a = alpha;
                edgeImages[i].color = c;
            }

            yield return null;
        }

        Destroy(canvasObj);
        edgeFlashCoroutine = null;
    }

    IEnumerator ShowPerfectText()
    {
        GameObject canvasObj = new GameObject("PerfectTextCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject glowObj = new GameObject("PerfectGlow");
        glowObj.transform.SetParent(canvasObj.transform, false);
        Image glow = glowObj.AddComponent<Image>();
        glow.color = new Color(perfectBurstColorA.r, perfectBurstColorA.g, perfectBurstColorA.b, 0f);
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = new Vector2(0f, 70f);
        glowRect.sizeDelta = new Vector2(760f, 300f);

        GameObject textBackObj = new GameObject("PerfectTextBack");
        textBackObj.transform.SetParent(canvasObj.transform, false);
        Text textBack = textBackObj.AddComponent<Text>();
        RectTransform backRect = textBack.rectTransform;
        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = new Vector2(0f, 70f);
        backRect.sizeDelta = new Vector2(900f, 280f);

        GameObject textFrontObj = new GameObject("PerfectTextFront");
        textFrontObj.transform.SetParent(canvasObj.transform, false);
        Text textFront = textFrontObj.AddComponent<Text>();
        RectTransform frontRect = textFront.rectTransform;
        frontRect.anchorMin = new Vector2(0.5f, 0.5f);
        frontRect.anchorMax = new Vector2(0.5f, 0.5f);
        frontRect.anchoredPosition = new Vector2(0f, 70f);
        frontRect.sizeDelta = new Vector2(900f, 280f);

        Font fontToUse = feedbackFont != null
            ? feedbackFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fontToUse == null) fontToUse = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (fontToUse == null) fontToUse = Font.CreateDynamicFontFromOSFont("Arial", 50);

        textBack.font = fontToUse;
        textFront.font = fontToUse;
        textBack.text = "PERFECT!";
        textFront.text = "PERFECT!";
        textBack.fontSize = 132;
        textFront.fontSize = 132;
        textBack.alignment = TextAnchor.MiddleCenter;
        textFront.alignment = TextAnchor.MiddleCenter;
        textBack.horizontalOverflow = HorizontalWrapMode.Overflow;
        textFront.horizontalOverflow = HorizontalWrapMode.Overflow;
        textBack.verticalOverflow = VerticalWrapMode.Overflow;
        textFront.verticalOverflow = VerticalWrapMode.Overflow;
        textBack.color = new Color(0f, 0f, 0f, 0f);
        textFront.color = new Color(perfectBurstColorA.r, perfectBurstColorA.g, perfectBurstColorA.b, 0f);

        Outline outline = textFrontObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.5f);
        outline.effectDistance = new Vector2(4f, -4f);
        Shadow shadow = textBackObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(perfectBurstColorB.r, perfectBurstColorB.g, perfectBurstColorB.b, 0.75f);
        shadow.effectDistance = new Vector2(10f, -10f);

        float duration = 0.82f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.55f;
        Vector3 endScale = Vector3.one * 1.22f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            float settle = 1f - Mathf.Clamp01((t - 0.55f) / 0.45f);
            float scale = Mathf.Lerp(startScale.x, endScale.x, punch) * Mathf.Lerp(1f, 0.92f, 1f - settle);
            float yOffset = Mathf.Lerp(0f, 110f, t);
            float alpha = t < 0.18f ? Mathf.InverseLerp(0f, 0.18f, t) : Mathf.InverseLerp(1f, 0.62f, t);
            Color frontColor = Color.Lerp(perfectBurstColorA, perfectBurstColorB, Mathf.PingPong(t * 2.5f, 1f));
            frontColor.a = alpha;
            Color backColor = new Color(0f, 0f, 0f, alpha * 0.45f);
            Color glowColor = Color.Lerp(perfectBurstColorA, perfectBurstColorB, Mathf.PingPong(t * 2f + 0.2f, 1f));
            glowColor.a = alpha * 0.26f;

            textFront.color = frontColor;
            textBack.color = backColor;
            glow.color = glowColor;

            textFrontObj.transform.localScale = Vector3.one * scale;
            textBackObj.transform.localScale = Vector3.one * (scale * 1.04f);
            glowObj.transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.35f, punch);

            frontRect.anchoredPosition = new Vector2(0f, 70f + yOffset);
            backRect.anchoredPosition = new Vector2(0f, 70f + yOffset);
            glowRect.anchoredPosition = new Vector2(0f, 70f + yOffset);

            yield return null;
        }

        Destroy(canvasObj);
        perfectTitleCoroutine = null;
    }

    IEnumerator PlayPerfectBurstOverlay()
    {
        GameObject canvasObj = new GameObject("PerfectBurstCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 245;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Image centerGlow = CreateBurstImage(canvasObj.transform, "CenterGlow", perfectBurstColorA, new Vector2(420f, 420f));
        Image ringA = CreateBurstImage(canvasObj.transform, "RingA", perfectBurstColorA, new Vector2(220f, 220f));
        Image ringB = CreateBurstImage(canvasObj.transform, "RingB", perfectBurstColorB, new Vector2(220f, 220f));

        float elapsed = 0f;
        while (elapsed < perfectBurstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / perfectBurstDuration);
            float wave = Mathf.Sin(t * Mathf.PI);

            centerGlow.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(260f, 260f), new Vector2(920f, 920f), t);
            ringA.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(180f, 180f), new Vector2(980f, 980f), t);
            ringB.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(240f, 240f), new Vector2(1180f, 1180f), Mathf.Clamp01(t * 0.92f));

            Color cGlow = Color.Lerp(perfectBurstColorA, perfectBurstColorB, Mathf.PingPong(t * 1.8f, 1f));
            cGlow.a = wave * 0.18f;
            centerGlow.color = cGlow;

            Color cA = perfectBurstColorA;
            cA.a = wave * 0.22f;
            ringA.color = cA;

            Color cB = perfectBurstColorB;
            cB.a = wave * 0.14f;
            ringB.color = cB;

            yield return null;
        }

        Destroy(canvasObj);
        perfectBurstCoroutine = null;
    }

    Image CreateBurstImage(Transform parent, string name, Color color, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = size;
        image.color = new Color(color.r, color.g, color.b, 0f);
        return image;
    }
    
    // Tum slotlar bos oldugunda spawn et
    public void CheckAndSpawnIfAllEmpty()
    {
        StartCoroutine(CheckAndSpawnCoroutine());
    }
    
    IEnumerator CheckAndSpawnCoroutine()
    {
        // Bir frame bekle (Destroy'un calismasi icin)
        yield return null;
        
        bool allSlotsEmpty = true;
        for (int i = 0; i < 3; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                allSlotsEmpty = false;
                break;
            }
        }
        
        if (allSlotsEmpty)
        {
            // Oyun bitis kontrolu yap (sadece grid dolu mu kontrol et)
            if (IsGridFull())
            {
                // Grid tamamen dolu, oyun bitti
                ShowGameOverAndRestart();
                yield break;
            }
            
            // Oyun devam ediyor, yeni bloklar spawn et
            SpawnNewBlocks();
            
            // Yeni bloklar spawn edildikten sonra kontrol et (biraz bekle)
            yield return new WaitForSeconds(0.2f);
            
            // HER HAMLEDEN SONRA OTOMATIK KAYDET (Kullanici oyundan cikarsa kaybolmasin)
            SaveGame();
            
            if (CheckGameOver())
            {
                // Elimizdeki bloklar yerlesemiyor, oyun bitti
                ShowGameOverAndRestart();
            }
        }
        else
        {
            // HER HAMLEDEN SONRA OTOMATIK KAYDET
            SaveGame();
            
            // Slotlarda bloklar var; yine de mevcut durumda hareket kalmis mi kontrol et
            if (CheckGameOver())
            {
                ShowGameOverAndRestart();
            }
        }
    }
    
    // Grid tamamen dolu mu kontrol et
    bool IsGridFull()
    {
        // Once tabloyu guncelle
        SyncGridStatus();
        
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] == null)
                {
                    return false; // Bos yer var
                }
            }
        }
        return true; // Grid tamamen dolu
    }
    
    // Oyun bitis kontrolu (elimizdeki bloklar yerlesebilir mi)
    public bool CheckGameOver()
    {
        // Kontrol oncesi grid durumunu guncelle (Onemli!)
        SyncGridStatus();

        // Elimizdeki objeler yerlesebilir mi kontrol et
        bool canPlaceAnyBlock = false;
        for (int i = 0; i < 3; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                DraggableBlock block = slotTransforms[i].GetComponentInChildren<DraggableBlock>();
                
                // Block null degilse ve yerlesebiliyorsa
                if (block != null)
                {
                    if (CanBlockBePlaced(block))
                    {
                        canPlaceAnyBlock = true;
                        break;
                    }
                }
            }
        }
        
        // Eger elimizde blok var ama hicbiri yerlesemiyorsa oyun bitti
        if (!canPlaceAnyBlock && HasAnyBlocksInSlots())
        {
            Debug.Log("Game Over: Elimizdeki bloklar gride sigmiyor.");
            
            // --- DEBUG LOGGING: Neden bitti? ---
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Grid Durumu (Game Over Aninda):");
            for (int y = columns - 1; y >= 0; y--)
            {
                for (int x = 0; x < rows; x++)
                {
                    sb.Append(gridStatus[x, y] != null ? "[X]" : "[_]");
                }
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());
            // -----------------------------------

            return true;
        }
        
        return false;
    }
    
    // Bir blok yerlesebilir mi kontrol et
    bool CanBlockBePlaced(DraggableBlock block)
    {
        if (block == null) return false;
        
        // Eger originalShape kaydedilmemisse, child unitlerden olusturmayi dene (fallback)
        List<Vector2Int> shapeToCheck = block.originalShape;
        
        if (shapeToCheck == null || shapeToCheck.Count == 0)
        {
            // Fallback: Child unitlerden sekli cikar
            shapeToCheck = new List<Vector2Int>();
            BlockUnit[] units = block.GetComponentsInChildren<BlockUnit>();
            if (units.Length == 0) return false;

            // Ilk unit'i referans al
            Vector3 firstPos = units[0].transform.localPosition;
            
            foreach (var unit in units)
            {
                Vector3 localPos = unit.transform.localPosition;
                // Grid birimlerine cevir (yaklasik)
                int x = Mathf.RoundToInt((localPos.x - firstPos.x) / (cellSize + spacing));
                int y = Mathf.RoundToInt((localPos.y - firstPos.y) / (cellSize + spacing));
                shapeToCheck.Add(new Vector2Int(x, y));
            }
        }
        
        // Tum grid pozisyonlarini dene
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                bool canPlaceAtThisPos = true;
                
                // Blok parcalarini bu grid pozisyonuna (x,y) gore test et
                foreach (Vector2Int part in shapeToCheck)
                {
                    int targetX = x + part.x;
                    int targetY = y + part.y;
                    
                    // Grid disina tasiyor mu?
                    if (targetX < 0 || targetX >= rows || targetY < 0 || targetY >= columns)
                    {
                        canPlaceAtThisPos = false;
                        break;
                    }
                    
                    // Grid dolu mu?
                    if (gridStatus[targetX, targetY] != null)
                    {
                        canPlaceAtThisPos = false;
                        break;
                    }
                }
                
                // Eger bu (x,y) pozisyonuna blok sigiyorsa, true don
                if (canPlaceAtThisPos) return true;
            }
        }
        
        return false;
    }
    
    // Slotlarda blok var mi kontrol et
    bool HasAnyBlocksInSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                // Sadece gercek DraggableBlock'lari say, cop objeleri sayma
                if (slotTransforms[i].GetComponentInChildren<DraggableBlock>() != null)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    bool IsShapePlaceable(Vector2Int[] shape)
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                bool ok = true;
                foreach (var part in shape)
                {
                    int tx = x + part.x;
                    int ty = y + part.y;
                    if (tx < 0 || tx >= rows || ty < 0 || ty >= columns) { ok = false; break; }
                    if (gridStatus[tx, ty] != null) { ok = false; break; }
                }
                if (ok) return true;
            }
        }
        return false;
    }
    
    void CreateBlockInSlot(int slotIndex, Vector2Int[] shape)
    {
        GameObject newBlock = Instantiate(blockBasePrefab, slotTransforms[slotIndex]);
        newBlock.name = "ActiveBlock";
        newBlock.transform.localPosition = Vector3.zero;
        DraggableBlock db = newBlock.GetComponent<DraggableBlock>();
        if (db == null) db = newBlock.AddComponent<DraggableBlock>();
        db.slotScale = blockInSlotScale;
        db.dragScale = 1.0f;
        float minX = 10, maxX = -10, minY = 10, maxY = -10;
        foreach (var c in shape)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }
        Vector3 centerOffset = new Vector3((maxX + minX) * (cellSize + spacing) / 2f, (maxY + minY) * (cellSize + spacing) / 2f, 0);
        foreach (var coord in shape)
        {
            GameObject piece = Instantiate(cellPrefab, newBlock.transform);
            piece.transform.localPosition = new Vector3(coord.x * (cellSize + spacing), coord.y * (cellSize + spacing), 0) - centerOffset;
            piece.transform.localScale = Vector3.one * cellSize;

            if (piece.GetComponent<BlockUnit>() == null)
            {
                BlockUnit bu = piece.AddComponent<BlockUnit>();
                // bu.gridLayer = LayerMask.GetMask("Grid"); // KALDIRILDI
            }
            BoxCollider2D pieceCollider = piece.GetComponent<BoxCollider2D>();
            if (pieceCollider == null) pieceCollider = piece.AddComponent<BoxCollider2D>();
            pieceCollider.size = new Vector2(2f, 2f);
            pieceCollider.isTrigger = false;
            pieceCollider.enabled = true;
            Color blockColor = blockColors[Random.Range(0, blockColors.Length)];
            SetVisuals(piece, blockColor, 10);
        }
        if (db != null)
        {
            db.originalShape = new List<Vector2Int>();
            foreach (var v in shape) db.originalShape.Add(v);
            db.SetupChildMouseEvents();
        }
    }
    
    Vector2Int[] SelectGuaranteedPlaceableShape()
    {
        List<Vector2Int[]> pools = new List<Vector2Int[]>();
        foreach (var s in easyShapes) pools.Add(s);
        foreach (var s in mediumShapes) pools.Add(s);
        foreach (var s in hardShapes) pools.Add(s);
        for (int i = 0; i < pools.Count; i++)
        {
            int j = Random.Range(i, pools.Count);
            var tmp = pools[i]; pools[i] = pools[j]; pools[j] = tmp;
        }
        foreach (var shape in pools)
        {
            if (IsShapePlaceable(shape)) return shape;
        }
        return easyShapes[Random.Range(0, easyShapes.Count)];
    }
    
    // LevelCompleteRoutine was deleted because it's unused.

    public void OnRefreshButtonClicked()
    {
        if (remainingRefreshes > 0)
        {
            remainingRefreshes--;
            UpdateRefreshButtonState();
            StartCoroutine(ShowRefreshBanner());
            RefreshBlocksWithGuarantee();
        }
        else
        {
            if (AdsManager.Instance != null)
            {
                // UI tiklamalarini engelle ki reklam yuklenirken baska seye basilmasin
                isGameActive = false;
                
                AdsManager.Instance.ShowExtraLifeRewarded(success =>
                {
                    // MainThreadDispatcher kullanildigi icin isGameActive'i burada guvenle true yapabiliriz
                    isGameActive = true;
                    
                    if (success)
                    {
                        GrantRefreshFromAd();
                        OnRefreshButtonClicked();
                    }
                    else
                    {
                        // Eger reklam gosterilemezse (internet yok vb.) kullaniciya kucuk bir bildirim yapilabilir
                        StartCoroutine(ShowFloatingText("AD NOT READY", Color.red));
                    }
                });
            }
        }
    }

    public void OnUndoButtonClicked()
    {
        if (!hasUndoSnapshot)
        {
            StartCoroutine(ShowFloatingText("MAKE A MOVE FIRST", Color.white));
            return;
        }

        if (remainingUndos > 0)
        {
            remainingUndos--;
            PerformUndo();
        }
        else if (AdsManager.Instance != null)
        {
            isGameActive = false;
            UpdateUndoButtonState();
            AdsManager.Instance.ShowUndoRewarded(success =>
            {
                isGameActive = true;
                if (success)
                {
                    PerformUndo();
                }
                else
                {
                    StartCoroutine(ShowFloatingText("AD NOT READY", Color.red));
                    UpdateUndoButtonState();
                }
            });
        }
    }

    void PerformUndo()
    {
        if (!hasUndoSnapshot) return;
        RestoreUndoSnapshot();
        UpdateUndoButtonState();
    }
    
    public void UpdateRefreshButtonState()
    {
        if (refreshButton != null)
        {
            refreshButton.interactable = isGameActive;

            if (refreshButtonBackground != null)
            {
                // Rengini hep ayni (aktif) birak. Sonucta artik 0 da olsa basilip reklam izlenecek.
                refreshButtonBackground.color = Color.white;
            }

            // Text varsa guncelle (Eger 0 olursa "AD" veya reklam ikonu konulabilir ama simdilik sayi)
            if (refreshCountText != null)
            {
                if (remainingRefreshes > 0)
                {
                    refreshCountText.text = remainingRefreshes.ToString();
                }
                else
                {
                    refreshCountText.text = "+"; // veya "AD", "Oynat"
                }
            }
            else
            {
                // TextMeshProUGUI degilse cocuklarda ara
                var textComp = refreshButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textComp != null) 
                {
                    textComp.text = remainingRefreshes > 0 ? remainingRefreshes.ToString() : "+";
                }
                else
                {
                    var textLegacy = refreshButton.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (textLegacy != null) 
                    {
                        textLegacy.text = remainingRefreshes > 0 ? remainingRefreshes.ToString() : "+";
                    }
                }
            }

            if (!isGameActive || remainingRefreshes > 0)
            {
                StopRefreshButtonPulse();
            }
            else
            {
                StartRefreshButtonPulse();
            }
        }
    }
    
    public void GrantRefreshFromAd()
    {
        remainingRefreshes++;
        UpdateRefreshButtonState();
    }

    public void UpdateUndoButtonState()
    {
        if (undoButton == null) return;

        undoButton.interactable = isGameActive;

        if (undoButtonBackground != null)
        {
            undoButtonBackground.color = isGameActive ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        if (undoCountText != null)
        {
            if (!hasUndoSnapshot)
            {
                undoCountText.text = "1";
            }
            else if (remainingUndos > 0)
            {
                undoCountText.text = remainingUndos.ToString();
            }
            else
            {
                undoCountText.text = "+";
            }
        }
        else
        {
            var textComp = undoButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null)
            {
                if (!hasUndoSnapshot) textComp.text = "1";
                else textComp.text = remainingUndos > 0 ? remainingUndos.ToString() : "+";
            }
            else
            {
                var textLegacy = undoButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (textLegacy != null)
                {
                    if (!hasUndoSnapshot) textLegacy.text = "1";
                    else textLegacy.text = remainingUndos > 0 ? remainingUndos.ToString() : "+";
                }
            }
        }
    }

    public void GrantUndoFromAd()
    {
        remainingUndos++;
        UpdateUndoButtonState();
    }
    
    void StartRefreshButtonPulse()
    {
        if (refreshButton == null) return;
        if (refreshButtonPulseCoroutine != null) return;
        refreshButtonPulseCoroutine = StartCoroutine(RefreshButtonPulseRoutine());
    }
    
    void StopRefreshButtonPulse()
    {
        if (refreshButtonPulseCoroutine != null)
        {
            StopCoroutine(refreshButtonPulseCoroutine);
            refreshButtonPulseCoroutine = null;
        }
        if (refreshButton != null)
        {
            refreshButton.transform.localScale = refreshButtonOriginalScale;
        }
    }
    
    IEnumerator RefreshButtonPulseRoutine()
    {
        // Orijinal rotasyonu kaydet
        Quaternion originalRotation = refreshButton.transform.localRotation;

        while (remainingRefreshes <= 0 && isGameActive && refreshButton != null)
        {
            // 1. BEKLEME
            yield return new WaitForSeconds(6f);

            // 2. ZIPLAMA (YUKARI ASAGI SCALE)
            float jumpDuration = 0.6f; 
            float t = 0f;
            
            while (t < jumpDuration)
            {
                t += Time.deltaTime;
                float progress = t / jumpDuration;
                
                // Sinus dalgasi ile ziplama efekti (1.0 -> 1.15 -> 1.0)
                float scaleMultiplier = 1f + Mathf.Sin(progress * Mathf.PI) * 0.15f;
                refreshButton.transform.localScale = refreshButtonOriginalScale * scaleMultiplier;
                
                yield return null;
            }
            // Scale'i garanti sifirla
            refreshButton.transform.localScale = refreshButtonOriginalScale;

            // 3. BEKLEME
            yield return new WaitForSeconds(6f);

            // 4. SALLANMA (SAG SOL ROTASYON)
            float shakeDuration = 0.5f;
            t = 0f;
            
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float progress = t / shakeDuration; // 0..1
                
                // Sinus dalgasi ile sag sol sallama (+15 derece, -15 derece...)
                // Hizli titreme icin frekansi artiriyoruz (3 tam tur)
                float angle = Mathf.Sin(progress * Mathf.PI * 6f) * 15f;
                refreshButton.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
                
                yield return null;
            }
            // Rotasyonu garanti sifirla
            refreshButton.transform.localRotation = originalRotation;
        }

        // Coroutine biterse her seyi sifirla
        if (refreshButton != null)
        {
            refreshButton.transform.localScale = refreshButtonOriginalScale;
            refreshButton.transform.localRotation = originalRotation;
        }
        refreshButtonPulseCoroutine = null;
    }
    
    void RefreshBlocksWithGuarantee()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = slotTransforms[i].childCount - 1; j >= 0; j--)
            {
                Destroy(slotTransforms[i].GetChild(j).gameObject);
            }
        }
        int guaranteedIndex = Random.Range(0, 3);
        for (int i = 0; i < 3; i++)
        {
            Vector2Int[] shape;
            if (i == guaranteedIndex)
            {
                shape = SelectGuaranteedPlaceableShape();
            }
            else
            {
                float chance = Random.value;
                if (chance < 0.35f)
                    shape = easyShapes[Random.Range(0, easyShapes.Count)];
                else if (chance < 0.85f)
                    shape = mediumShapes[Random.Range(0, mediumShapes.Count)];
                else
                    shape = hardShapes[Random.Range(0, hardShapes.Count)];
            }
            CreateBlockInSlot(i, shape);
        }
    }
    
    IEnumerator ShowRefreshBanner()
    {
        GameObject canvasObj = new GameObject("RefreshBannerCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        GameObject textObj = new GameObject("RefreshText");
        textObj.transform.SetParent(canvasObj.transform, false);
        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        var rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(700, 200);
        text.text = "REFRESH!";
        text.color = Color.white;
        if (feedbackFont != null) text.font = feedbackFont;
        else
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", 50);
        }
        text.fontSize = 100;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);
        
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);
        float gridBottomY = -totalHeight / 2f;
        Vector3 world = transform.TransformPoint(new Vector3(0, gridBottomY - bottomOffset, 0));
        float yTarget = (Camera.main != null) ? Camera.main.WorldToScreenPoint(world).y : Screen.height * 0.15f;
        Vector3 startPos = new Vector3(Screen.width * 0.5f, 0f, 0);
        Vector3 endPos = new Vector3(Screen.width * 0.5f, Mathf.Clamp(yTarget, 80f, Screen.height * 0.35f), 0);
        
        float dur = 0.6f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            rect.position = Vector3.Lerp(startPos, endPos, p);
            float s = Mathf.Lerp(0.85f, 1.15f, p);
            textObj.transform.localScale = Vector3.one * s;
            yield return null;
        }
        float fade = 0.25f;
        t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fade);
            var c = text.color; c.a = Mathf.Lerp(1f, 0f, p); text.color = c;
            yield return null;
        }
        Destroy(canvasObj);
    }
    
    // Oyun bitis mesaji goster ve oyunu basla
    void ShowGameOverAndRestart()
    {
        StartCoroutine(GameOverSequence());
    }
    
    // Disaridan tetiklenebilen basit kontrol
    public void TryGameOverCheck()
    {
        if (CheckGameOver())
        {
            ShowGameOverAndRestart();
        }
    }
    
    IEnumerator GameOverSequence()
    {
        // Event'i cagir
        if (OnGameOver != null) OnGameOver.Invoke();
        
        // Kayitli oyunu sil (Boylece restart atinca ayni yerden baslamaz)
        ClearSave();
        
        Debug.Log("OYUN BİTTİ! No More Left!");

        GameObject canvasObj = null;

        // Prefab varsa onu kullan
        if (gameOverTextPrefab != null)
        {
            canvasObj = Instantiate(gameOverTextPrefab);
        }
        else
        {
            // Prefab yoksa kodla olustur
            canvasObj = new GameObject("GameOverCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // Scaler ayarlarini yap (COK ONEMLI)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Dikey mobil oyun standardi
            scaler.matchWidthOrHeight = 0.5f; // Hem genislik hem yukseklik ortalamasi
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            GameObject textObj = new GameObject("GameOverText");
            textObj.transform.SetParent(canvasObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            
            // Text objesinin ekrani kaplamasini sagla
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.text = "No More Left !";
            
            // Font yukleme (Unity surumune gore degisebilir, guvenli yontem)
            Font defaultFont = feedbackFont;
            
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            // Projedeki tum fontlari tara ve birini bul
            if (defaultFont == null)
            {
                // Fallback: OS Font
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 50);

                if (defaultFont == null)
                {
                    Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                    if (allFonts != null && allFonts.Length > 0)
                    {
                        // Isminde Arial geceni onceliklendir
                        defaultFont = allFonts.FirstOrDefault(f => f.name.Contains("Arial"));
                        // Yoksa herhangi birini al
                        if (defaultFont == null) defaultFont = allFonts[0];
                    }
                }
            }
            
            if (defaultFont != null)
            {
                text.font = defaultFont;
                Debug.Log($"Font yuklendi: {defaultFont.name}");
            }
            else
            {
                Debug.LogError("HICBIR FONT BULUNAMADI!");
            }

            text.fontSize = 80; // Fontu buyut
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Golge/Outline ekle
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);
            
            // Arka plan paneli
            GameObject bgObj = new GameObject("BackgroundPanel");
            bgObj.transform.SetParent(canvasObj.transform, false);
            bgObj.transform.SetAsFirstSibling(); // Text'in arkasina at
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }
        
        yield return new WaitForSeconds(3.0f);
        
        if (canvasObj != null) Destroy(canvasObj);
        
        bool waitForAd = false;
        if (AdsManager.Instance != null)
        {
            waitForAd = true;
            AdsManager.Instance.ShowGameOverInterstitial(success =>
            {
                waitForAd = false;
            });
        }
        
        while (waitForAd)
        {
            yield return null;
        }
        
        RestartGame();
    }
    
    // Oyunu basla
    public void RestartGame()
    {
        // Skoru sifirla
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        // Grid'i temizle
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] != null)
                {
                    SafeDestroy(gridStatus[x, y]);
                    gridStatus[x, y] = null;
                }
            }
        }
        
        // Slotlardaki bloklari temizle
        for (int i = 0; i < 3; i++)
        {
            for (int j = slotTransforms[i].childCount - 1; j >= 0; j--)
            {
                Destroy(slotTransforms[i].GetChild(j).gameObject);
            }
        }
        
        // Yenileme hakkini sifirla (Oyuncu tekrar 1 hakka sahip olsun)
        remainingRefreshes = maxRefreshes;
        UpdateRefreshButtonState();
        remainingUndos = maxUndos;
        hasUndoSnapshot = false;
        UpdateUndoButtonState();
        
        // Level'i yeniden olustur ve hemen kaydet
        GenerateLevel();
        SaveGame();
    }

    // Menuye Don
    public void ReturnToMenu()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMenu();
        }
        else
        {
            SaveGame();
            SceneManager.LoadScene("MainMenu");
        }
    }

    void SetVisuals(GameObject obj, Color col, int order)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) 
        { 
            // Rengi ata ama Alpha'yi kesinlikle 1 yap (Tam opak)
            col.a = 1f;
            sr.color = col; 
            sr.sortingOrder = order; 
        }
    }

    // --- SMART SPAWN YARDIMCILARI ---
    bool CanShapeFit(Vector2Int[] shape)
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (DoesShapeFitAt(shape, x, y))
                    return true;
            }
        }
        return false;
    }

    bool DoesShapeFitAt(Vector2Int[] shape, int gridX, int gridY)
    {
        foreach (var offset in shape)
        {
            int targetX = gridX + offset.x;
            int targetY = gridY + offset.y;
            
            if (targetX < 0 || targetX >= rows || targetY < 0 || targetY >= columns)
                return false;
                
            if (gridStatus[targetX, targetY] != null)
                return false;
        }
        return true;
    }
} // GridManager Sinifi Burada Bitiyor

// --- SlotBehavior Sinifi ---
public class SlotBehavior : MonoBehaviour
{
    public Color normalColor;
    public Color hoverColor;
    private SpriteRenderer sr;
    void Start() => sr = GetComponent<SpriteRenderer>();
    void OnMouseEnter() => sr.color = hoverColor;
    void OnMouseExit() => sr.color = normalColor;
    void OnMouseDown() => sr.color = hoverColor;
    void OnMouseUp() => sr.color = normalColor;
}
