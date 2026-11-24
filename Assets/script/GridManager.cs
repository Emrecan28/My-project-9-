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
    public float blockInSlotScale = 0.52f;

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
    public Font feedbackFont; // Perfect/Nice yazilari icin font (bos ise NeoUI fontu kullanilir)
    [Tooltip("Neon tema: oyun arka plani, mor grid paneli ve neon cerceve (eski stil)")]
    public bool useNeonTheme = false;
    [Tooltip("Block Blast tarzi: parlak bloklar, gomulu hucreler, lacivert tahta, mavi arka plan")]
    public bool useJewelStyle = true;

    // Son yerlestirilen parcanin rengi ve merkezi (patlama dalgasi ve parcaciklar icin)
    private Color lastPlacedColor = Color.white;
    private int lastScoreGained;

    // Bu oyunun istatistikleri (sonuc ekrani icin)
    private int linesThisGame;
    private int bestComboThisGame;
    private int bestAtGameStart;
    [Tooltip("Tahta tamamen temizlenince verilen bonus puan")]
    public int allClearBonus = 300;
    [Tooltip("Tahta bu oranda doluysa kurtarici parca sistemi devreye girer")]
    [Range(0.4f, 0.95f)] public float mercyFillThreshold = 0.7f;
    private Vector2 lastPlacedCenter = new Vector2(3.5f, 3.5f);
    private readonly Dictionary<SpriteRenderer, Sprite> previewRecolored = new Dictionary<SpriteRenderer, Sprite>();

    Color CurrentCellColor => useJewelStyle ? BlockSkin.EmptyCellColor : cellColor;
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
    public bool HasUndo => hasUndoSnapshot;

    private Transform[] slotTransforms = new Transform[3];
    private GameObject[,] gridStatus;
    private GameObject[,] gridCells; // Arka plan hucreleri
    private List<Vector2Int> currentlyHighlighted = new List<Vector2Int>();
    public Color highlightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color invalidHighlightColor = new Color(1f, 0.3f, 0.3f, 0.5f); // Kirmizi vurgu rengi (Gecersiz yerlesim)
    private Coroutine perfectNeonCoroutine;
    private Coroutine borderPulseCoroutine;
    private Coroutine edgeFlashCoroutine;
    private bool levelCompleteSequenceRunning;

    // Oyun Durumu
    public bool isGameActive = true;

    [Header("--- Puan ve Combo ---")]
    [Tooltip("Blok konunca her kare icin verilen puan (satir patlamasa da)")]
    public int placementPointsPerCell = 1;
    [Tooltip("Combo serisi: bu kadar hamle ust uste satir patlatilmazsa seri sifirlanir")]
    public int comboMoveWindow = 3;
    private int comboStreak;      // Ust uste patlatma serisi
    private int movesSinceClear;  // Son patlamadan beri yapilan hamle

    [Header("--- Revive (Odullu Reklamla Devam) ---")]
    [Tooltip("Bir oyunda en fazla kac kez reklamla devam edilebilir")]
    public int maxRevivesPerGame = 2;
    [Tooltip("Revive'da temizlenecek en dolu satir sayisi")]
    public int reviveClearRows = 3;
    [Tooltip("Devam penceresinin geri sayimi (saniye)")]
    public float reviveCountdownSeconds = 5f;
    private int revivesUsed;

    private int undoComboStreak;
    private int undoMovesSinceClear;

    // Patlama animasyonu devam ederken > 0 olur (game over kontrolu bu sirada ertelenir)
    private int activeClears;
    // Game over sekansi calisiyor mu? (Ust uste tetiklenmesin)
    private bool isGameOverRunning;
    // Tahta her yeniden olusturuldugunda artar; eski coroutine'ler bunu gorup durur
    private int boardVersion;
    private Font cachedFeedbackFont;

    public bool IsBusy => activeClears > 0 || isGameOverRunning || levelCompleteSequenceRunning;

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

    void Update()
    {
#if UNITY_EDITOR
        // Sadece Unity Editor'de: F9 ile game over / revive penceresini test et (build'e girmez)
        if (UnityEngine.Input.GetKeyDown(KeyCode.F9)) ShowGameOverAndRestart();
#endif
        if (IsBombArmed) HandleBombInput();
    }

    // ======================= GUCLENDIRMELER (BOMBA / DONDUR) =======================
    public bool IsBombArmed { get; private set; }
    public event System.Action OnPowerUpStateChanged;

    public bool CanUsePowerUp => isGameActive && !IsBusy && !PauseManager.IsPauseMenuOpen && !DraggableBlock.IsDragging;

    public void ToggleBomb()
    {
        if (IsBombArmed) { DisarmBomb(); return; }
        if (!CanUsePowerUp) return;
        if (IsRotateArmed) DisarmRotate();
        if (Economy.Coins < Economy.BombPrice)
        {
            UIKit.Toast("NOT ENOUGH COINS", "BOMB COSTS " + Economy.BombPrice, Theme.Danger);
            return;
        }
        IsBombArmed = true;
        Haptics.Light();
        UIKit.Toast("TAP THE BOARD", "BLASTS A 3x3 AREA", Theme.Primary);
        OnPowerUpStateChanged?.Invoke();
    }

    public void DisarmBomb()
    {
        if (!IsBombArmed) return;
        IsBombArmed = false;
        OnPowerUpStateChanged?.Invoke();
    }

    void HandleBombInput()
    {
        if (!UnityEngine.Input.GetMouseButtonDown(0)) return;
        if (!CanUsePowerUp) { DisarmBomb(); return; }

        // UI'ye (guclendirme butonuna) dokunulduysa tahtaya patlatma yapma
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null)
        {
            if (es.IsPointerOverGameObject()) return;
            if (UnityEngine.Input.touchCount > 0 && es.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId)) return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 world = cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
        world.z = 0f;
        Vector2Int? cell = WorldPosToGrid(world);
        if (!cell.HasValue) { DisarmBomb(); return; } // tahta disina dokunmak iptal eder
        StartCoroutine(DetonateBomb(cell.Value));
    }

    IEnumerator DetonateBomb(Vector2Int center)
    {
        IsBombArmed = false;
        if (!Economy.TrySpend(Economy.BombPrice))
        {
            OnPowerUpStateChanged?.Invoke();
            yield break;
        }

        activeClears++;
        try
        {
            SyncGridStatus();
            Vector3 centerWorld = GridToWorldPos(center);
            BlockFx.I.Burst(centerWorld, Theme.Primary, 26, 1.7f);
            BlockFx.I.Burst(centerWorld, Theme.Highlight, 14, 1.2f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.3f, 0.18f);
            Haptics.Heavy();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayComboExplosion();

            int removed = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = center.x + dx, y = center.y + dy;
                    if (x < 0 || x >= rows || y < 0 || y >= columns) continue;
                    GameObject b = gridStatus[x, y];
                    if (b == null) continue;
                    BlockFx.I.Burst(b.transform.position, BlockUnit.GetColor(b), 8, 1.2f);
                    SafeDestroy(b);
                    gridStatus[x, y] = null;
                    removed++;
                }
            }
            if (removed > 0 && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(removed * 5);
                BlockFx.I.ScorePopup(centerWorld, removed * 5, Theme.Primary);
            }
            BlockFx.I.Praise("BOOM!", Theme.Primary, 330f);
            Progress.OnPowerUpUsed(true);
            yield return new WaitForSeconds(0.25f);
        }
        finally
        {
            activeClears = Mathf.Max(0, activeClears - 1);
        }

        // Bombadan sonra geri alma, bombayi bedavaya geri getirmesin
        hasUndoSnapshot = false;
        UpdateUndoButtonState();
        SaveGame();
        OnPowerUpStateChanged?.Invoke();
    }

    // Elindeki 3 parcayi saat yonunde 90 derece dondurur
    // ROTATE: butona basinca secim modu acilir, dokunulan TEK parca saat yonunde 90 derece doner.
    // Simetrik parcalar (kare, tek blok, arti) donunce degismedigi icin para alinmaz.
    public bool IsRotateArmed { get; private set; }

    public void ToggleRotate()
    {
        if (IsRotateArmed) { DisarmRotate(); return; }
        if (!CanUsePowerUp) return;
        if (IsBombArmed) DisarmBomb();
        if (Economy.Coins < Economy.RotatePrice)
        {
            UIKit.Toast("NOT ENOUGH COINS", "ROTATE COSTS " + Economy.RotatePrice, Theme.Danger);
            return;
        }
        bool anyRotatable = false;
        for (int i = 0; i < 3; i++)
        {
            DraggableBlock db = slotTransforms[i] != null ? slotTransforms[i].GetComponentInChildren<DraggableBlock>() : null;
            if (db != null && db.originalShape != null && db.originalShape.Count > 0 && !SameShape(RotateShape(db.originalShape), db.originalShape))
                anyRotatable = true;
        }
        if (!anyRotatable)
        {
            UIKit.Toast("NOTHING TO ROTATE", "THESE PIECES LOOK THE SAME TURNED", Theme.Neutral);
            return;
        }
        IsRotateArmed = true;
        Haptics.Light();
        UIKit.Toast("TAP A PIECE", "IT TURNS 90 DEGREES", Theme.Positive);
        OnPowerUpStateChanged?.Invoke();
    }

    public void DisarmRotate()
    {
        if (!IsRotateArmed) return;
        IsRotateArmed = false;
        OnPowerUpStateChanged?.Invoke();
    }

    // DraggableBlock dokunuldugunda (rotate modu aciksa) cagirir
    public void RotateSinglePiece(DraggableBlock db)
    {
        if (!IsRotateArmed || db == null) return;
        if (!isGameActive || IsBusy || PauseManager.IsPauseMenuOpen) { DisarmRotate(); return; }

        int slotIndex = -1;
        for (int i = 0; i < 3; i++)
            if (slotTransforms[i] != null && db.transform.parent == slotTransforms[i]) slotIndex = i;
        if (slotIndex < 0 || db.originalShape == null || db.originalShape.Count == 0) return;

        List<Vector2Int> rotated = RotateShape(db.originalShape);
        if (SameShape(rotated, db.originalShape))
        {
            UIKit.Toast("THIS PIECE CAN'T TURN", "TAP ANOTHER PIECE", Theme.Neutral);
            return; // para alinmaz, mod acik kalir
        }
        if (!Economy.TrySpend(Economy.RotatePrice))
        {
            UIKit.Toast("NOT ENOUGH COINS", "ROTATE COSTS " + Economy.RotatePrice, Theme.Danger);
            DisarmRotate();
            return;
        }

        BlockUnit unit = db.GetComponentInChildren<BlockUnit>();
        Color col = unit != null ? BlockUnit.GetColor(unit.gameObject) : BlockSkin.RandomColor();
        ClearSlot(slotIndex);
        CreateBlockInSlot(slotIndex, rotated, col);
        DraggableBlock fresh = slotTransforms[slotIndex].GetComponentInChildren<DraggableBlock>();
        if (fresh != null) fresh.spawnPopDelay = 0f;

        IsRotateArmed = false;
        Haptics.Medium();
        if (SoundManager.Instance != null) SoundManager.Instance.PlayBlockPlace();
        hasUndoSnapshot = false; // geri alma, donmeyi bedavaya geri almasin
        UpdateUndoButtonState();
        Progress.OnPowerUpUsed(false);
        SaveGame();
        OnPowerUpStateChanged?.Invoke();
        CheckAndSpawnIfAllEmpty(); // donen parca hic sigmiyorsa normal oyun sonu kontrolu
    }

    static bool SameShape(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a.Count != b.Count) return false;
        HashSet<Vector2Int> set = new HashSet<Vector2Int>(Normalize(b));
        foreach (var c in Normalize(a)) if (!set.Contains(c)) return false;
        return true;
    }

    static List<Vector2Int> Normalize(List<Vector2Int> shape)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in shape) { minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y); }
        List<Vector2Int> r = new List<Vector2Int>(shape.Count);
        foreach (var c in shape) r.Add(new Vector2Int(c.x - minX, c.y - minY));
        return r;
    }

    static List<Vector2Int> RotateShape(List<Vector2Int> shape)
    {
        List<Vector2Int> r = new List<Vector2Int>(shape.Count);
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in shape)
        {
            Vector2Int v = new Vector2Int(c.y, -c.x);
            r.Add(v);
            minX = Mathf.Min(minX, v.x);
            minY = Mathf.Min(minY, v.y);
        }
        for (int i = 0; i < r.Count; i++) r[i] = new Vector2Int(r[i].x - minX, r[i].y - minY);
        return r;
    }

    // ======================= FEVER (NEON OVERDRIVE) =======================
    // 5'li combo serisine ulasinca sonraki 4 hamle boyunca patlamalar 2 kat puan verir
    [Header("Fever")]
    public int feverComboThreshold = 5;
    public int feverMoves = 4;
    private int feverMovesLeft;
    public bool IsFever => feverMovesLeft > 0;

    void Start()
    {
        gridStatus = new GameObject[rows, columns];
        if (useJewelStyle)
        {
            // Blok, komsu hucreye neredeyse degecek boyutta (Block Blast gibi siki grid)
            BlockSkin.Init((cellSize + spacing) * 0.94f / cellSize);
            blockColors = BlockSkin.Palette;
            GameBackground.Ensure(true);
        }
        else if (useNeonTheme) GameBackground.Ensure(false);
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

        // Ust bar (para, rekor, skor, duraklat) + alt guclendirme bari (geri al, yeni parca, bomba, dondur)
        GameHud.Create(this);
        PowerUpBar.Create(this);
    }

    public void GenerateLevel()
    {
        isGameActive = true; // Oyunu aktiflestir
        isGameOverRunning = false;
        activeClears = 0;
        boardVersion++;

        // Onceki slot referanslarini temizle
        for (int i = 0; i < slotTransforms.Length; i++) slotTransforms[i] = null;

        // Tum cocuk objeleri temizle (Grid, Slotlar, Bloklar)
        // Tersten dongu ile silmek daha guvenlidir
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child == null) continue;
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        CreateBackPanel();
        CreateGrid();

        // Kayitli oyun varsa onu yukle. Engeller zaten kaydin icinde oldugu icin
        // tekrar yerlestirilmez (yoksa temizlenen engeller geri gelir).
        if (HasSavedGame())
        {
            LoadSavedGame();
        }
        else
        {
            // Yeni oyun: sonuc ekrani sayaclarini sifirla
            linesThisGame = 0;
            bestComboThisGame = 0;
            bestAtGameStart = PlayerPrefs.GetInt("HighScore", 0);

            if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
            {
                PlaceLevelObstacles(LevelManager.Instance.currentLevelData);
            }
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
        if (LevelManager.Instance != null && LevelManager.Instance.IsDailyMode)
        {
            // Gunluk meydan okuma: her gunun kendi kaydi
            return $"Daily_{DailyChallenge.TodayKey}_{suffix}";
        }
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
        // Oyun biterken veya level tamamlanirken kayit yapma (bitmis tahta geri gelmesin)
        if (isGameOverRunning || levelCompleteSequenceRunning) return;
        if (gridStatus == null || gridCells == null) return;

        try
        {
            PlayerPrefs.SetString(GetSaveKey("Grid"), SerializeGridData());
            PlayerPrefs.SetInt(GetSaveKey("HasSavedGame"), 1);
            SaveSlots();

            if (ScoreManager.Instance != null)
            {
                PlayerPrefs.SetInt(GetSaveKey("Score"), ScoreManager.Instance.GetCurrentScore());
            }

            // Haklar da kaydedilir (yoksa uygulamayi kapatip acmak bedava hak verir)
            PlayerPrefs.SetInt(GetSaveKey("Refreshes"), remainingRefreshes);
            PlayerPrefs.SetInt(GetSaveKey("Undos"), remainingUndos);
            PlayerPrefs.SetInt(GetSaveKey("Combo"), comboStreak);
            PlayerPrefs.SetInt(GetSaveKey("MovesSinceClear"), movesSinceClear);
            PlayerPrefs.SetInt(GetSaveKey("Revives"), revivesUsed);
            PlayerPrefs.SetInt(GetSaveKey("Lines"), linesThisGame);
            PlayerPrefs.SetInt(GetSaveKey("BestComboGame"), bestComboThisGame);
            PlayerPrefs.SetInt(GetSaveKey("BestAtStart"), bestAtGameStart);

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
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] == null) continue;
                Color c = BlockUnit.GetColor(gridStatus[x, y]);
                sb.Append(x).Append(',').Append(y).Append(',').Append(ColorUtility.ToHtmlStringRGBA(c)).Append('|');
            }
        }
        return sb.ToString();
    }

    string SerializeSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return "";
        Transform slot = slotTransforms[slotIndex];
        if (slot == null || slot.childCount == 0) return "";

        DraggableBlock db = slot.GetComponentInChildren<DraggableBlock>();
        if (db == null || db.originalShape == null || db.originalShape.Count == 0) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var vec in db.originalShape)
        {
            sb.Append(vec.x).Append(',').Append(vec.y).Append(';');
        }

        // Rengi blok parcasindan al (kok objenin kendi SpriteRenderer'i olabilir)
        BlockUnit unit = db.GetComponentInChildren<BlockUnit>();
        Color c = unit != null ? BlockUnit.GetColor(unit.gameObject) : Color.white;

        return sb.ToString() + "#" + ColorUtility.ToHtmlStringRGBA(c);
    }

    public void CaptureUndoSnapshot()
    {
        undoGridData = SerializeGridData();
        for (int i = 0; i < 3; i++)
        {
            undoSlotData[i] = SerializeSlotData(i);
        }
        undoScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
        undoComboStreak = comboStreak;
        undoMovesSinceClear = movesSinceClear;
        hasUndoSnapshot = true;
        UpdateUndoButtonState();
    }

    void SaveSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            string data = SerializeSlotData(i);
            if (string.IsNullOrEmpty(data)) PlayerPrefs.DeleteKey(GetSaveKey($"Slot_{i}"));
            else PlayerPrefs.SetString(GetSaveKey($"Slot_{i}"), data);
        }
    }

    public void LoadSavedGame()
    {
        RestoreGridFromData(PlayerPrefs.GetString(GetSaveKey("Grid"), ""));

        // Skoru yukle
        if (ScoreManager.Instance != null)
        {
            int savedScore = PlayerPrefs.GetInt(GetSaveKey("Score"), 0);
            ScoreManager.Instance.SetScore(savedScore);
        }

        remainingRefreshes = PlayerPrefs.GetInt(GetSaveKey("Refreshes"), maxRefreshes);
        remainingUndos = PlayerPrefs.GetInt(GetSaveKey("Undos"), maxUndos);
        comboStreak = PlayerPrefs.GetInt(GetSaveKey("Combo"), 0);
        movesSinceClear = PlayerPrefs.GetInt(GetSaveKey("MovesSinceClear"), 0);
        revivesUsed = PlayerPrefs.GetInt(GetSaveKey("Revives"), 0);
        linesThisGame = PlayerPrefs.GetInt(GetSaveKey("Lines"), 0);
        bestComboThisGame = PlayerPrefs.GetInt(GetSaveKey("BestComboGame"), 0);
        bestAtGameStart = PlayerPrefs.GetInt(GetSaveKey("BestAtStart"), PlayerPrefs.GetInt("HighScore", 0));
    }

    void LoadSavedSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            string key = GetSaveKey($"Slot_{i}");
            if (PlayerPrefs.HasKey(key))
            {
                RestoreSlotFromData(i, PlayerPrefs.GetString(key));
            }
        }

        // Bozuk kayit yuzunden slotlar bos kaldiysa yeni blok ver
        if (!HasAnyBlocksInSlots()) SpawnNewBlocks();
    }

    void PlaceBlockAt(int x, int y, Color color)
    {
        if (x < 0 || x >= rows || y < 0 || y >= columns) return;

        // Eger zaten doluysa elleme
        if (gridStatus[x, y] != null) return;

        GameObject cellBlock = Instantiate(cellPrefab, transform);
        cellBlock.transform.localPosition = gridCells[x, y].transform.localPosition;
        cellBlock.transform.localScale = Vector3.one * cellSize;

        SetBlockVisuals(cellBlock, color, 5);

        if (cellBlock.GetComponent<BoxCollider2D>() == null)
            cellBlock.AddComponent<BoxCollider2D>();

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

            // Bozuk veri oyunu cokertmesin
            if (!int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) continue;

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
        if (!ColorUtility.TryParseHtmlString("#" + colorHex, out color)) color = blockColors[0];

        List<Vector2Int> shape = new List<Vector2Int>();
        string[] vecs = shapeData.Split(';');
        foreach (var v in vecs)
        {
            if (string.IsNullOrEmpty(v)) continue;
            string[] xy = v.Split(',');
            if (xy.Length == 2 && int.TryParse(xy[0], out int sx) && int.TryParse(xy[1], out int sy))
            {
                shape.Add(new Vector2Int(sx, sy));
            }
        }

        if (shape.Count == 0) return;
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

        for (int i = 0; i < 3; i++) ClearSlot(i);

        RestoreGridFromData(undoGridData);

        for (int i = 0; i < 3; i++)
        {
            RestoreSlotFromData(i, undoSlotData[i]);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SetScore(undoScore);
        }
        comboStreak = undoComboStreak;
        movesSinceClear = undoMovesSinceClear;

        hasUndoSnapshot = false;
        SaveGame();
        UpdateUndoButtonState();
    }

    // Slottaki bloklari temizler. Once parent'tan ayirir ki ayni frame'de
    // kayit/kontrol yapilirsa yok edilmek uzere olan blok sayilmasin.
    void ClearSlot(int slotIndex)
    {
        Transform slot = slotTransforms[slotIndex];
        if (slot == null) return;
        for (int j = slot.childCount - 1; j >= 0; j--)
        {
            Transform child = slot.GetChild(j);
            child.gameObject.SetActive(false);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    // Tek blok olusturma fonksiyonu (yeni spawn, kayittan yukleme ve geri alma hepsi bunu kullanir)
    void CreateBlockInSlot(int slotIndex, List<Vector2Int> shape, Color color)
    {
        Transform slotTr = slotTransforms[slotIndex];
        if (slotTr == null || shape == null || shape.Count == 0) return;

        GameObject newBlock = Instantiate(blockBasePrefab, slotTr);
        newBlock.name = "ActiveBlock";
        newBlock.transform.localPosition = Vector3.zero;

        // Prefab'dan gelen eski cocuklari temizle
        for (int i = newBlock.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(newBlock.transform.GetChild(i).gameObject);
        }

        DraggableBlock db = newBlock.GetComponent<DraggableBlock>();
        if (db == null) db = newBlock.AddComponent<DraggableBlock>();

        db.slotScale = blockInSlotScale;
        db.dragScale = 1.0f;
        db.originalShape = new List<Vector2Int>(shape);

        // Merkezi hesapla (Center Offset)
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var c in shape)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        float step = cellSize + spacing;
        Vector3 centerOffset = new Vector3((maxX + minX) * step / 2f, (maxY + minY) * step / 2f, 0);

        foreach (Vector2Int pos in shape)
        {
            GameObject part = Instantiate(cellPrefab, newBlock.transform);
            part.transform.localPosition = new Vector3(pos.x * step, pos.y * step, 0) - centerOffset;
            part.transform.localScale = Vector3.one * cellSize;

            SetBlockVisuals(part, color, 10);

            var col = part.GetComponent<BoxCollider2D>();
            if (col == null) col = part.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 2f); // Parmakla kolay tutulsun
            col.isTrigger = false;
            col.enabled = true;
        }

        db.SetupChildMouseEvents();
    }

    // Rastgele renkli yeni blok (tum parcalar ayni renk)
    void CreateBlockInSlot(int slotIndex, Vector2Int[] shape)
    {
        Color blockColor = blockColors[Random.Range(0, blockColors.Length)];
        CreateBlockInSlot(slotIndex, new List<Vector2Int>(shape), blockColor);

        // Yeni parcalar tepside sirayla "pop" ile belirsin
        Transform slot = slotTransforms[slotIndex];
        DraggableBlock db = slot != null ? slot.GetComponentInChildren<DraggableBlock>() : null;
        if (db != null) db.spawnPopDelay = slotIndex * 0.08f;
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
        PlayerPrefs.DeleteKey(GetSaveKey("Refreshes"));
        PlayerPrefs.DeleteKey(GetSaveKey("Undos"));
        PlayerPrefs.DeleteKey(GetSaveKey("Combo"));
        PlayerPrefs.DeleteKey(GetSaveKey("MovesSinceClear"));
        PlayerPrefs.DeleteKey(GetSaveKey("Revives"));
        PlayerPrefs.DeleteKey(GetSaveKey("Lines"));
        PlayerPrefs.DeleteKey(GetSaveKey("BestComboGame"));
        PlayerPrefs.DeleteKey(GetSaveKey("BestAtStart"));
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

                SetBlockVisuals(obstacle, useJewelStyle ? BlockSkin.RandomColor() : color, 5);

                if (obstacle.GetComponent<BoxCollider2D>() == null)
                    obstacle.AddComponent<BoxCollider2D>();

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
        if (levelCompleteSequenceRunning) return;
        levelCompleteSequenceRunning = true;
        isGameActive = false;
        IsBombArmed = false;
        IsRotateArmed = false;

        // Level ilk kez mi bitiyor? (tekrar oynanan level'den tam odul verilmez, yoksa para kasilir)
        bool firstClear = LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null
            && LevelManager.Instance.currentLevelData.levelNumber >= LevelManager.Instance.GetUnlockedLevel();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UnlockNextLevel();
        }

        ClearSave();
        Haptics.Heavy();

        bool daily = LevelManager.Instance != null && LevelManager.Instance.IsDailyMode;
        if (daily)
        {
            int reward = DailyChallenge.MarkCompleted();
            Progress.OnDailyComplete();
            GameObject dailyOverlay = CreateBlockingOverlay("DAILY COMPLETE!", reward > 0 ? "+" + reward + " COINS" : null, Theme.Highlight);
            StartCoroutine(WaitAndReturnToMenu(dailyOverlay));
            return;
        }

        int levelNo = LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null
            ? LevelManager.Instance.currentLevelData.levelNumber : 0;
        int coins = firstClear ? Economy.CoinsForLevel(levelNo) : 5;
        Economy.Add(coins);
        Progress.OnLevelComplete();

        GameObject overlayObj = CreateBlockingOverlay("LEVEL COMPLETE!", "+" + coins + " COINS", Theme.Positive);
        StartCoroutine(WaitAndNextLevel(overlayObj));
    }

    IEnumerator WaitAndReturnToMenu(GameObject overlay)
    {
        yield return new WaitForSeconds(2.2f);
        if (LevelManager.Instance != null) LevelManager.Instance.currentLevelData = null;
        SceneManager.LoadScene("MainMenu");
    }

    IEnumerator WaitAndNextLevel(GameObject textObj)
    {
        yield return new WaitForSeconds(0.7f);

        int completedLevel = 0;
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            completedLevel = LevelManager.Instance.currentLevelData.levelNumber;
        }

        if (textObj != null)
        {
            SetOverlayText(textObj, "LEVEL " + (completedLevel + 1));
        }

        yield return new WaitForSeconds(0.6f);

        // Level gecisinde interstitial (her 2 levelde bir, belirli levelden sonra, aralikli)
        if (AdsManager.Instance != null && completedLevel > 0)
        {
            bool waitForAd = true;
            AdsManager.Instance.TryShowLevelCompleteInterstitial(completedLevel, () => waitForAd = false);
            float adWait = 0f;
            while (waitForAd && adWait < 60f)
            {
                adWait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (LevelManager.Instance != null) LevelManager.Instance.LoadNextLevel();
        if (textObj != null) Destroy(textObj);
        levelCompleteSequenceRunning = false;
    }

    void CreateSlicedPanel(string name, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BlockSkin.BoardPanel;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
    }

    // Ortak font: Inspector'dan atanan font, yoksa Unity'nin dahili fontu
    Font GetFeedbackFont()
    {
        if (feedbackFont != null) return feedbackFont;
        if (cachedFeedbackFont == null) cachedFeedbackFont = NeoUI.Font;
        return cachedFeedbackFont;
    }

    // Ekrana yazi/efekt cizmek icin ortak overlay canvas
    GameObject CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    Text CreateOverlayText(Transform parent, string name, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = GetFeedbackFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Theme.SurfaceRim;
        outline.effectDistance = new Vector2(4, -4);
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0, -8);
        return text;
    }

    // Neon cerceveli, yuvarlak koseli panel (pencerelerin govdesi)
    RectTransform CreateNeonPanel(Transform parent, Vector2 size, Color borderColor)
    {
        GameObject borderObj = new GameObject("NeonBorder");
        borderObj.transform.SetParent(parent, false);
        Image border = borderObj.AddComponent<Image>();
        border.sprite = NeoUI.RoundedSprite;
        border.type = Image.Type.Sliced;
        border.color = borderColor;
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = size + new Vector2(14f, 14f);

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(borderObj.transform, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.sprite = NeoUI.RoundedSprite;
        panel.type = Image.Type.Sliced;
        panel.color = NeoUI.PanelDark;
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = size;
        return panelRect;
    }

    Image CreateDim(Transform parent)
    {
        GameObject bgObj = new GameObject("Dim");
        bgObj.transform.SetParent(parent, false);
        bgObj.transform.SetAsFirstSibling();
        Image dim = bgObj.AddComponent<Image>();
        dim.color = NeoUI.Dim;
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;
        return dim;
    }

    GameObject CreateBlockingOverlay(string message, string subtitle = null, Color? accent = null)
    {
        GameObject canvasObj;
        Color accentColor = accent ?? Theme.Neutral;

        if (gameOverTextPrefab != null)
        {
            canvasObj = Instantiate(gameOverTextPrefab);
        }
        else
        {
            canvasObj = CreateOverlayCanvas("OverlayCanvas", 100);
            CreateDim(canvasObj.transform);

            RectTransform panel = CreateNeonPanel(canvasObj.transform, new Vector2(860f, subtitle != null ? 520f : 400f), accentColor);

            // Ilk Text baslik olarak kullanilir (SetOverlayText onu gunceller)
            Text title = CreateOverlayText(panel, "OverlayText", 100);
            title.color = Theme.Highlight;
            title.rectTransform.sizeDelta = new Vector2(820f, 200f);
            title.rectTransform.anchoredPosition = new Vector2(0f, subtitle != null ? 70f : 0f);
            title.GetComponent<Outline>().effectColor = Theme.SurfaceRim;

            if (subtitle != null)
            {
                Text sub = CreateOverlayText(panel, "Subtitle", 60);
                sub.text = subtitle;
                sub.color = Theme.Text;
                sub.rectTransform.sizeDelta = new Vector2(820f, 120f);
                sub.rectTransform.anchoredPosition = new Vector2(0f, -110f);
            }

            // Hafif "pop" animasyonu
            StartCoroutine(PopIn(panel.parent as RectTransform));
        }

        EnsureOverlayBlocksInput(canvasObj);
        SetOverlayText(canvasObj, message);
        return canvasObj;
    }

    IEnumerator PopIn(RectTransform rect)
    {
        if (rect == null) yield break;
        float t = 0f;
        const float duration = 0.25f;
        while (t < duration && rect != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            // Hafif tasma ile buyume (ease-out-back)
            float s = 1f + 2.2f * Mathf.Pow(p - 1f, 3f) + 1.2f * Mathf.Pow(p - 1f, 2f);
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, s);
            yield return null;
        }
        if (rect != null) rect.localScale = Vector3.one;
    }

    void EnsureOverlayBlocksInput(GameObject overlayObj)
    {
        if (overlayObj == null) return;

        var canvas = overlayObj.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
        }

        if (overlayObj.GetComponentInChildren<GraphicRaycaster>(true) == null)
        {
            var rootCanvas = overlayObj.GetComponent<Canvas>();
            if (rootCanvas != null) overlayObj.AddComponent<GraphicRaycaster>();
        }

        var bg = overlayObj.GetComponentInChildren<Image>(true);
        if (bg == null)
        {
            var canvasTransform = canvas != null ? canvas.transform : overlayObj.transform;
            var bgObj = new GameObject("BackgroundPanel");
            bgObj.transform.SetParent(canvasTransform, false);
            bgObj.transform.SetAsFirstSibling();
            bg = bgObj.AddComponent<Image>();
            bg.color = Theme.Dim;
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
        }

        bg.raycastTarget = true;
    }

    void SetOverlayText(GameObject overlayObj, string message)
    {
        if (overlayObj == null) return;

        var tmp = overlayObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = message;
            return;
        }

        var legacy = overlayObj.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = message;
        }
    }

    void CreateBackPanel()
    {
        float totalWidth = (rows * cellSize) + ((rows - 1) * spacing);
        float totalHeight = (columns * cellSize) + ((columns - 1) * spacing);

        if (useJewelStyle)
        {
            // Block Blast tarzi: lacivert tahta, az yuvarlak koseler, koyu kalin cerceve yerine
            // arka plandan bir ton acik, ince bir kenar cizgisi
            Vector2 size = new Vector2(totalWidth + panelPadding * 2.1f, totalHeight + panelPadding * 2.1f);
            CreateSlicedPanel("BoardRim", size + new Vector2(0.14f, 0.14f), new Color(0.42f, 0.55f, 1f, 0.55f), -3);
            CreateSlicedPanel("Board", size, BlockSkin.BoardColor, -2);
            return;
        }

        if (useNeonTheme)
        {
            // Dis neon cerceve (hafif parlama)
            GameObject glow = Instantiate(panelPrefab, transform);
            glow.name = "PanelGlow";
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = new Vector3(totalWidth + panelPadding + 0.5f, totalHeight + panelPadding + 0.5f, 1);
            SetVisuals(glow, new Color(NeoUI.Purple.r, NeoUI.Purple.g, NeoUI.Purple.b, 0.55f), -3);
        }

        GameObject panel = Instantiate(panelPrefab, transform);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = new Vector3(totalWidth + panelPadding, totalHeight + panelPadding, 1);
        SetVisuals(panel, useNeonTheme ? new Color(0.05f, 0.03f, 0.13f, 0.94f) : panelColor, -2);
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
                if (useJewelStyle && BlockSkin.EmptyCell != null)
                {
                    SpriteRenderer cellSr = cell.GetComponent<SpriteRenderer>();
                    if (cellSr != null) cellSr.sprite = BlockSkin.EmptyCell;
                }
                SetVisuals(cell, CurrentCellColor, 0);

                // BoxCollider2D Ekle (Grid algilama icin)
                BoxCollider2D col = cell.GetComponent<BoxCollider2D>();
                if (col == null) col = cell.AddComponent<BoxCollider2D>();
                col.isTrigger = true;

                // Hucreyi kaydet
                gridCells[x, y] = cell;


                if (useOutline && !useJewelStyle)
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
            slot.transform.localScale = Vector3.one * (slotScale * 0.80f); // %20 kucultuldu

            slotTransforms[i] = slot.transform;
            var slotSr = slot.GetComponent<SpriteRenderer>();
            if (slotSr != null) slotSr.enabled = false;

            // Slot collider'ini trigger yap (bloklari engellemesin)
            BoxCollider2D slotCollider = slot.GetComponent<BoxCollider2D>();
            if (slotCollider == null)
            {
                slotCollider = slot.AddComponent<BoxCollider2D>();
            }
            slotCollider.isTrigger = true; // ONEMLI: Trigger yap ki bloklari engellemesin
            slotCollider.enabled = false; // Slot tiklanmaz, sadece bloklar tutulur

        }
    }

    public void SpawnNewBlocks()
    {
        // Slotlarda hala blok varsa spawn etme
        if (HasAnyBlocksInSlots()) return;

        // MERCY (KURTARICI) SISTEMI: tahta cok doluysa 3 parcadan biri mumkunse
        // satir tamamlayan, olmazsa kesin sigan bir parca olur; zor parca ihtimali azalir.
        float fill = GetBoardFillRatio();
        bool mercy = fill >= mercyFillThreshold;
        int rescueSlot = mercy ? Random.Range(0, 3) : -1;

        // Tum slotlar bos, yeni 3 obje spawn et
        for (int i = 0; i < 3; i++)
        {
            if (i == rescueSlot)
            {
                Vector2Int[] rescue = SelectRescueShape();
                if (rescue != null)
                {
                    CreateBlockInSlot(i, rescue);
                    continue;
                }
            }

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

                // Tahta cok doluyken zor parca yerine orta parca ver
                if (mercy && targetList == hardShapes) targetList = mediumShapes;

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
        HighlightPlacement(positions, isValid, highlightColor);
    }

    // Block Blast tarzi onizleme:
    //  - Gecerli yerde parcanin golgesi (kendi renginde, soluk) gorunur
    //  - Bu hamleyle patlayacak satir/sutunlardaki bloklar parcanin rengine boyanir
    //  - Gecersiz yerde hicbir sey gosterilmez
    public void HighlightPlacement(List<Vector2Int> positions, bool isValid, Color pieceColor)
    {
        ClearHighlight(); // Oncekileri temizle
        if (!isValid) return;

        Color ghost = useJewelStyle ? Color.Lerp(BlockSkin.EmptyCellColor, pieceColor, 0.55f) : highlightColor;

        // 1) Parcanin golgesi
        foreach (var pos in positions)
        {
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns && gridCells[pos.x, pos.y] != null)
            {
                SetVisuals(gridCells[pos.x, pos.y], ghost, 0);
                if (!currentlyHighlighted.Contains(pos)) currentlyHighlighted.Add(pos);
            }
        }

        // 2) Yerlesim sonrasi patlayacak cizgileri bul
        bool[,] filled = new bool[rows, columns];
        for (int x = 0; x < rows; x++)
            for (int y = 0; y < columns; y++)
                filled[x, y] = gridStatus[x, y] != null;
        foreach (var pos in positions)
            if (pos.x >= 0 && pos.x < rows && pos.y >= 0 && pos.y < columns) filled[pos.x, pos.y] = true;

        List<Vector2Int> lineCells = new List<Vector2Int>();
        for (int y = 0; y < columns; y++)
        {
            bool full = true;
            for (int x = 0; x < rows && full; x++) full = filled[x, y];
            if (full) for (int x = 0; x < rows; x++) lineCells.Add(new Vector2Int(x, y));
        }
        for (int x = 0; x < rows; x++)
        {
            bool full = true;
            for (int y = 0; y < columns && full; y++) full = filled[x, y];
            if (full) for (int y = 0; y < columns; y++) lineCells.Add(new Vector2Int(x, y));
        }

        // 3) Patlayacak bloklari parcanin rengine boya (birakinca eski haline doner)
        foreach (var cell in lineCells)
        {
            GameObject block = gridStatus[cell.x, cell.y];
            if (block == null) continue;
            SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
            if (sr == null || previewRecolored.ContainsKey(sr)) continue;

            if (useJewelStyle)
            {
                previewRecolored[sr] = sr.sprite;
                sr.sprite = BlockSkin.GetBlockSprite(pieceColor);
            }
            else
            {
                if (!currentlyHighlighted.Contains(cell)) currentlyHighlighted.Add(cell);
                SetVisuals(gridCells[cell.x, cell.y], highlightColor, 0);
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
                    SetVisuals(gridCells[pos.x, pos.y], CurrentCellColor, 0);
                }
            }
        }
        currentlyHighlighted.Clear();

        foreach (var kv in previewRecolored)
        {
            if (kv.Key != null) kv.Key.sprite = kv.Value;
        }
        previewRecolored.Clear();
    }

    // DraggableBlock yerlestirmeden hemen once cagirir (patlama rengi ve dalga merkezi icin)
    public void NotePlacement(List<Vector2Int> cells, Color color)
    {
        lastPlacedColor = color;
        if (cells == null || cells.Count == 0) return;
        Vector2 sum = Vector2.zero;
        foreach (var c in cells) sum += c;
        lastPlacedCenter = sum / cells.Count;
    }

    // Blok konunca kare basina puan (Block Blast gibi)
    public void AddPlacementScore(int cellCount)
    {
        Progress.OnPlacement();
        if (feverMovesLeft > 0) feverMovesLeft--;
        if (ScoreManager.Instance != null && placementPointsPerCell > 0 && cellCount > 0)
        {
            ScoreManager.Instance.AddScore(cellCount * placementPointsPerCell);
        }
    }

    public void CheckForLines()
    {
        SyncGridStatus();
        if (FindFullLines(out List<int> fullRows, out List<int> fullColumns))
        {
            // Combo serisi: ust uste hamlelerde patlatma yapildikca artar
            comboStreak++;
            movesSinceClear = 0;
            StartCoroutine(ExplodeAndClear(fullRows, fullColumns, 0));
        }
        else
        {
            movesSinceClear++;
            if (movesSinceClear >= comboMoveWindow) comboStreak = 0;
        }
    }

    // Dolu satir ve sutunlari bulur (patlamakta olan bloklar sayilmaz)
    bool FindFullLines(out List<int> fullRows, out List<int> fullColumns)
    {
        fullRows = new List<int>();
        fullColumns = new List<int>();

        for (int y = 0; y < columns; y++)
        {
            bool isFull = true;
            for (int x = 0; x < rows; x++)
            {
                if (gridStatus[x, y] == null) { isFull = false; break; }
            }
            if (isFull) fullRows.Add(y);
        }

        for (int x = 0; x < rows; x++)
        {
            bool isFull = true;
            for (int y = 0; y < columns; y++)
            {
                if (gridStatus[x, y] == null) { isFull = false; break; }
            }
            if (isFull) fullColumns.Add(x);
        }

        return fullRows.Count > 0 || fullColumns.Count > 0;
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


    // Temizlenen satirlarin ustundeki bloklar asagi duser.
    // Satirlar yukaridan asagiya islenir ve her biri icin 1 kaydirilir; boylece
    // yan yana birden fazla satir temizlense de bloklar dogru miktarda duser.
    void DropBlocksAfterClear(List<int> clearedRows)
    {
        if (clearedRows == null || clearedRows.Count == 0) return;

        List<int> sorted = new List<int>(clearedRows);
        sorted.Sort();

        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            int clearedRow = sorted[i];
            for (int y = clearedRow + 1; y < columns; y++)
            {
                for (int x = 0; x < rows; x++)
                {
                    GameObject block = gridStatus[x, y];
                    if (block == null) continue;

                    int newY = y - 1;
                    if (gridStatus[x, newY] != null) continue; // Guvenlik: dolu yere bindirme

                    gridStatus[x, newY] = block;
                    gridStatus[x, y] = null;
                    block.transform.position = GridToWorldPos(new Vector2Int(x, newY));
                }
            }
        }
    }

    IEnumerator ExplodeAndClear(List<int> fullRows, List<int> fullColumns, int chain)
    {
        int myBoard = boardVersion;
        activeClears++;
        try
        {
            HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
            foreach (int row in fullRows)
                for (int x = 0; x < rows; x++) cellsToClear.Add(new Vector2Int(x, row));
            foreach (int col in fullColumns)
                for (int y = 0; y < columns; y++) cellsToClear.Add(new Vector2Int(col, y));

            // PATLAMA ANALIZI: Kombinasyon var mi?
            int totalLines = fullRows.Count + fullColumns.Count;
            bool isBigExplosion = totalLines > 1;

            // SKOR: Temizlenen hucre * 10, birden fazla cizgi carpan, zincir (cascade) ek carpan
            if (ScoreManager.Instance != null)
            {
                int scoreToAdd = cellsToClear.Count * 10;
                if (totalLines > 1) scoreToAdd *= totalLines;
                if (chain > 0) scoreToAdd *= (chain + 1);
                else if (comboStreak > 1) scoreToAdd *= comboStreak; // COMBO carpani
                if (IsFever) scoreToAdd *= 2; // FEVER: 2 kat
                lastScoreGained = scoreToAdd;
                ScoreManager.Instance.AddScore(scoreToAdd); // Level hedefine ulasildiysa ShowLevelComplete burada tetiklenir
                ScoreManager.Instance.PlayComboScorePulse(isBigExplosion || chain > 0 || comboStreak > 1);
            }

            // KAMERA SALLAMA: sadece buyuk patlamalarda, hafif
            if (CameraShake.Instance != null && (isBigExplosion || chain > 0))
            {
                CameraShake.Instance.Shake(0.25f, 0.12f);
            }

            // SES EFEKTI: seri devam ettikce patlama sesi bir ton yukselir (seri bozulunca basa doner)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayClearWithStreak(comboStreak + chain, isBigExplosion || chain > 0);
            }

            // ISTATISTIK / GOREV / BASARIM
            linesThisGame += totalLines;
            Progress.OnLinesCleared(totalLines);
            if (chain == 0 && comboStreak > bestComboThisGame) bestComboThisGame = comboStreak;
            if (chain == 0) Progress.OnCombo(comboStreak);
            if (ScoreManager.Instance != null) Progress.OnScore(ScoreManager.Instance.GetCurrentScore());

            // FEVER baslat: combo esigine ulasildi
            bool feverStarted = false;
            if (chain == 0 && comboStreak >= feverComboThreshold && !IsFever)
            {
                feverMovesLeft = feverMoves + 1; // bu hamle de sayilsin diye +1 (AddPlacementScore once dustu)
                feverStarted = true;
            }

            bool isCombo = chain == 0 && comboStreak > 1;
            if (isBigExplosion || chain > 0 || isCombo) Haptics.Heavy();
            else Haptics.Medium();

            // OVGU KELIMESI (Block Blast tarzi): cizgi sayisina gore
            string praise = null;
            if (chain > 0) praise = "CHAIN x" + (chain + 1) + "!";
            else if (totalLines >= 6) praise = "UNBELIEVABLE!";
            else if (totalLines == 5) praise = "AMAZING!";
            else if (totalLines == 4) praise = "EXCELLENT!";
            else if (totalLines == 3) praise = "GREAT!";
            else if (totalLines == 2) praise = "GOOD!";
            if (praise != null) BlockFx.I.Praise(praise, Theme.Highlight);
            if (isCombo) BlockFx.I.Praise("COMBO " + comboStreak, Theme.Primary, praise != null ? 120f : 220f);
            if (feverStarted)
            {
                BlockFx.I.Praise("FEVER x2!", Theme.Danger, -160f);
                Haptics.Heavy();
            }
            else if (IsFever && chain == 0)
            {
                BlockFx.I.Praise("x2", Theme.Danger, -160f);
            }

            // Patlayacak bloklari topla ve isaretle.
            // Animasyon sirasinda oyuncu yeni blok koyabilir; bu yuzden silme islemi
            // gridStatus'a degil bu listeye gore yapilir (yeni konan blok yanlislikla silinmesin).
            List<GameObject> blocksToExplode = new List<GameObject>();
            List<float> delays = new List<float>();
            Vector3 popupWorld = Vector3.zero;
            foreach (Vector2Int pos in cellsToClear)
            {
                popupWorld += GridToWorldPos(pos);
                GameObject obj = gridStatus[pos.x, pos.y];
                if (obj == null) continue;
                blocksToExplode.Add(obj);
                // DALGA: son konan parcaya yakin bloklar once patlar
                delays.Add(Vector2.Distance(pos, lastPlacedCenter) * 0.035f);
                BlockUnit bu = obj.GetComponent<BlockUnit>();
                if (bu != null) bu.isDying = true;
                gridStatus[pos.x, pos.y] = null;
            }
            if (cellsToClear.Count > 0) popupWorld /= cellsToClear.Count;

            // "+80" puan yazisi temizlenen bolgenin ortasinda
            if (lastScoreGained > 0) BlockFx.I.ScorePopup(popupWorld, lastScoreGained, lastPlacedColor);

            Color burstColor = lastPlacedColor;
            Vector3[] originalScales = new Vector3[blocksToExplode.Count];
            SpriteRenderer[] renderers = new SpriteRenderer[blocksToExplode.Count];
            bool[] burst = new bool[blocksToExplode.Count];
            float maxDelay = 0f;
            for (int i = 0; i < blocksToExplode.Count; i++)
            {
                originalScales[i] = blocksToExplode[i].transform.localScale;
                renderers[i] = blocksToExplode[i].GetComponent<SpriteRenderer>();
                if (renderers[i] != null) renderers[i].sortingOrder = 20;
                maxDelay = Mathf.Max(maxDelay, delays[i]);
            }

            // Her blok kendi sirasi gelince: once hafif buyur ve parlar, sonra parcaciklara ayrilip kuculur
            const float popDuration = 0.22f;
            float elapsed = 0f;
            while (elapsed < maxDelay + popDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < blocksToExplode.Count; i++)
                {
                    GameObject obj = blocksToExplode[i];
                    if (obj == null) continue;
                    float local = (elapsed - delays[i]) / popDuration;
                    if (local <= 0f) continue;

                    if (!burst[i])
                    {
                        burst[i] = true;
                        if (useJewelStyle && renderers[i] != null) renderers[i].sprite = BlockSkin.GetBlockSprite(burstColor);
                        BlockFx.I.Burst(obj.transform.position, burstColor, isBigExplosion ? 5 : 4, isBigExplosion ? 1.2f : 1f);
                    }

                    float p = Mathf.Clamp01(local);
                    float scale = p < 0.35f ? Mathf.Lerp(1f, 1.18f, p / 0.35f) : Mathf.Lerp(1.18f, 0f, (p - 0.35f) / 0.65f);
                    obj.transform.localScale = originalScales[i] * scale;
                    if (renderers[i] != null)
                    {
                        Color c = renderers[i].color;
                        c.a = 1f - Mathf.Clamp01((p - 0.5f) / 0.5f);
                        renderers[i].color = c;
                    }
                }
                yield return null;
                if (myBoard != boardVersion) yield break; // Tahta yeniden olusturuldu
            }

            foreach (var obj in blocksToExplode) SafeDestroy(obj);

            // Level tamamlandiysa devam etme (kayit/spawn yapilmasin)
            if (levelCompleteSequenceRunning) yield break;

            yield return new WaitForSeconds(0.1f);
            if (myBoard != boardVersion || levelCompleteSequenceRunning) yield break;

            // Ustteki bloklari asagi kaydir
            SyncGridStatus();
            DropBlocksAfterClear(fullRows);

            // Dusen bloklar yeni cizgi olusturduysa zincirleme patlat (ekstra puanli)
            if (FindFullLines(out List<int> nextRows, out List<int> nextCols))
            {
                yield return StartCoroutine(ExplodeAndClear(nextRows, nextCols, chain + 1));
            }
        }
        finally
        {
            activeClears = Mathf.Max(0, activeClears - 1);
        }

        // En distaki patlama bitince: gerekirse yeni blok ver, kaydet, game over kontrol et
        if (chain == 0 && myBoard == boardVersion && !levelCompleteSequenceRunning)
        {
            // TAHTA TAMAMEN TEMIZLENDIYSE buyuk bonus
            SyncGridStatus();
            bool boardEmpty = true;
            for (int x = 0; x < rows && boardEmpty; x++)
                for (int y = 0; y < columns && boardEmpty; y++)
                    if (gridStatus[x, y] != null) boardEmpty = false;
            if (boardEmpty && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(allClearBonus);
                BlockFx.I.Praise("ALL CLEAR!", Theme.Positive, 330f);
                Progress.OnAllClear();
                BlockFx.I.ScorePopup(transform.position, allClearBonus, Theme.Positive);
                Haptics.Heavy();
            }

            CheckAndSpawnIfAllEmpty();
        }
    }

    IEnumerator ShowFloatingText(string message, Color color)
    {
        GameObject canvasObj = CreateOverlayCanvas("FloatingTextCanvas", 200);
        // Yazi tiklamalari engellemesin
        canvasObj.GetComponent<GraphicRaycaster>().enabled = false;

        Text text = CreateOverlayText(canvasObj.transform, "FloatingText", 120);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 300);
        text.text = message;
        text.color = color;
        text.raycastTarget = false;
        text.GetComponent<Outline>().effectColor = Theme.SurfaceRim;

        float inDuration = 0.4f;
        float outDuration = 0.6f;
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / inDuration);
            text.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, p);
            yield return null;
        }
        t = 0f;
        while (t < outDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / outDuration);
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
        GameObject canvasObj = CreateOverlayCanvas("PerfectEdgeFlash", 240);
        canvasObj.GetComponent<GraphicRaycaster>().enabled = false;

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




    // Tum slotlar bos oldugunda spawn et
    public void CheckAndSpawnIfAllEmpty()
    {
        StartCoroutine(CheckAndSpawnCoroutine());
    }

    IEnumerator CheckAndSpawnCoroutine()
    {
        // Bir frame bekle (Destroy'un calismasi icin)
        yield return null;
        if (isGameOverRunning || levelCompleteSequenceRunning) yield break;

        if (!HasAnyBlocksInSlots())
        {
            SyncGridStatus();
            SpawnNewBlocks();
            yield return new WaitForSeconds(0.2f);
            if (isGameOverRunning || levelCompleteSequenceRunning) yield break;
        }

        // HER HAMLEDEN SONRA OTOMATIK KAYDET (Kullanici oyundan cikarsa kaybolmasin)
        SaveGame();

        // Patlama suruyorsa game over kontrolunu ertele; patlama bitince tekrar cagrilir
        if (activeClears > 0) yield break;

        if (CheckGameOver())
        {
            ShowGameOverAndRestart();
        }
    }

    // Grid tamamen dolu mu kontrol et

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
            if (slotTransforms[i] != null && slotTransforms[i].childCount > 0)
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

    float GetBoardFillRatio()
    {
        int filled = 0;
        for (int x = 0; x < rows; x++)
            for (int y = 0; y < columns; y++)
                if (gridStatus[x, y] != null) filled++;
        return filled / (float)(rows * columns);
    }

    // Tahtaya sigan ve en cok satir/sutun tamamlayan parcayi bulur (hic tamamlayan yoksa null)
    Vector2Int[] SelectRescueShape()
    {
        List<Vector2Int[]> pool = new List<Vector2Int[]>();
        pool.AddRange(easyShapes);
        pool.AddRange(mediumShapes);
        pool.AddRange(hardShapes);
        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        Vector2Int[] best = null;
        int bestLines = 0;
        foreach (var shape in pool)
        {
            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    if (!DoesShapeFitAt(shape, x, y)) continue;
                    int lines = CountLinesCompletedBy(shape, x, y);
                    if (lines > bestLines)
                    {
                        bestLines = lines;
                        best = shape;
                    }
                }
            }
        }
        return best;
    }

    int CountLinesCompletedBy(Vector2Int[] shape, int gx, int gy)
    {
        HashSet<int> rowsTouched = new HashSet<int>();
        HashSet<int> colsTouched = new HashSet<int>();
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        foreach (var o in shape)
        {
            Vector2Int c = new Vector2Int(gx + o.x, gy + o.y);
            cells.Add(c);
            rowsTouched.Add(c.y);
            colsTouched.Add(c.x);
        }

        int lines = 0;
        foreach (int y in rowsTouched)
        {
            bool full = true;
            for (int x = 0; x < rows && full; x++)
                full = gridStatus[x, y] != null || cells.Contains(new Vector2Int(x, y));
            if (full) lines++;
        }
        foreach (int x in colsTouched)
        {
            bool full = true;
            for (int y = 0; y < columns && full; y++)
                full = gridStatus[x, y] != null || cells.Contains(new Vector2Int(x, y));
            if (full) lines++;
        }
        return lines;
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
            if (CanShapeFit(shape)) return shape;
        }
        return easyShapes[Random.Range(0, easyShapes.Count)];
    }

    // LevelCompleteRoutine was deleted because it's unused.

    public void OnRefreshButtonClicked()
    {
        if (PauseManager.IsPauseMenuOpen || IsBusy || !isGameActive) return;

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
        if (PauseManager.IsPauseMenuOpen || IsBusy || !isGameActive) return;

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
            refreshButton.interactable = isGameActive && !PauseManager.IsPauseMenuOpen;

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

        undoButton.interactable = isGameActive && !PauseManager.IsPauseMenuOpen;

        if (undoButtonBackground != null)
        {
            undoButtonBackground.color = (isGameActive && !PauseManager.IsPauseMenuOpen) ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
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

    void RefreshBlocksWithGuarantee(bool allPlaceable = false)
    {
        for (int i = 0; i < 3; i++) ClearSlot(i);

        SyncGridStatus();
        int guaranteedIndex = Random.Range(0, 3);
        for (int i = 0; i < 3; i++)
        {
            Vector2Int[] shape;
            if (allPlaceable || i == guaranteedIndex)
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

        // Geri al hakki eski bloklari geri getirmesin
        hasUndoSnapshot = false;
        UpdateUndoButtonState();
        SaveGame();
    }

    IEnumerator ShowRefreshBanner()
    {
        GameObject canvasObj = CreateOverlayCanvas("RefreshBannerCanvas", 250);
        canvasObj.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
        var text = CreateOverlayText(canvasObj.transform, "RefreshText", 100);
        text.raycastTarget = false;
        var textObj = text.gameObject;
        var rect = text.rectTransform;
        rect.sizeDelta = new Vector2(700, 200);
        text.text = "REFRESH!";

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
        // Ayni anda birden fazla game over calismasin (eskiden ust uste reklam/restart olabiliyordu)
        if (isGameOverRunning || levelCompleteSequenceRunning) return;
        isGameOverRunning = true;
        isGameActive = false;
        UpdateRefreshButtonState();
        UpdateUndoButtonState();
        StartCoroutine(GameOverSequence());
    }

    // Disaridan tetiklenebilen basit kontrol
    public void TryGameOverCheck()
    {
        if (IsBusy) return;
        if (CheckGameOver())
        {
            ShowGameOverAndRestart();
        }
    }

    IEnumerator GameOverSequence()
    {
        // Event'i cagir
        if (OnGameOver != null) OnGameOver.Invoke();
        Haptics.Heavy();

        // Oyuncu tahtanin dolu halini bir an gorsun
        yield return new WaitForSeconds(0.5f);

        // 1) REVIVE: Odullu reklam hazirsa "Devam et" teklif et (Block Blast'in ana reklam noktasi)
        bool canOfferRevive = revivesUsed < maxRevivesPerGame
                              && AdsManager.Instance != null
                              && AdsManager.Instance.IsRewardedReady();
        if (canOfferRevive)
        {
            bool revived = false;
            yield return StartCoroutine(ShowRevivePopup(result => revived = result));
            if (revived)
            {
                revivesUsed++;
                yield return StartCoroutine(ReviveBoard());
                yield break; // Oyun devam ediyor
            }
        }

        // 2) Oyun gercekten bitti: bloklar yukaridan asagi gumus rengine doner (Block Blast tarzi)
        yield return StartCoroutine(FreezeBoardWave());

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
        bool isLevelMode = LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null;
        bool newBest = !isLevelMode && finalScore > 0 && finalScore > bestAtGameStart;
        int coinsEarned = 0;
        if (!isLevelMode)
        {
            Progress.OnClassicGameEnd(finalScore);
            coinsEarned = Economy.CoinsForScore(finalScore);
            Economy.Add(coinsEarned);
        }
        feverMovesLeft = 0;
        IsBombArmed = false;
        IsRotateArmed = false;

        ClearSave();
        PlayerPrefs.Save(); // Endless modda rekoru diske yaz
        if (AdsManager.Instance != null) AdsManager.Instance.RegisterGameFinished();

        // SONUC EKRANI: oyuncu "Tekrar Oyna" veya "Menu" secene kadar bekler
        int choice = 1; // 1 = tekrar oyna, 2 = menu
        yield return StartCoroutine(ShowResultsScreen(finalScore, newBest, isLevelMode, coinsEarned, c => choice = c));

        // 3) INTERSTITIAL: sadece kurallar uygunsa (ilk oyunlarda yok, aralarda bekleme var)
        if (AdsManager.Instance != null)
        {
            bool waitForAd = true;
            AdsManager.Instance.TryShowGameOverInterstitial(() => waitForAd = false);

            // Reklam SDK'si hic cevap vermezse oyun takilmasin
            float adWait = 0f;
            while (waitForAd && adWait < 60f)
            {
                adWait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (choice == 2)
        {
            // Kayit zaten silindi; isGameOverRunning acik kaldigi icin bitmis tahta kaydedilmez
            bool dailyMode = LevelManager.Instance != null && LevelManager.Instance.IsDailyMode;
            if (dailyMode) LevelManager.Instance.currentLevelData = null;
            SceneManager.LoadScene(isLevelMode && !dailyMode ? "LevelScene" : "MainMenu");
            yield break;
        }

        RestartGame();
    }

    // Game Over sonuc ekrani: skor (sayarak), rekor, bu oyunun cizgi/combo sayilari, Tekrar Oyna / Menu
    IEnumerator ShowResultsScreen(int finalScore, bool newBest, bool isLevelMode, int coinsEarned, System.Action<int> onChoice)
    {
        bool dailyMode = LevelManager.Instance != null && LevelManager.Instance.IsDailyMode;
        GameObject canvasObj = CreateOverlayCanvas("ResultsCanvas", 300);
        CreateDim(canvasObj.transform);
        RectTransform panel = CreateNeonPanel(canvasObj.transform, new Vector2(860f, 1080f), newBest ? Theme.Highlight : Theme.Neutral);
        StartCoroutine(PopIn(panel.parent as RectTransform));

        string titleText = newBest ? "NEW BEST!" : (isLevelMode ? "LEVEL FAILED" : "GAME OVER");
        Text title = CreateOverlayText(panel, "Title", 92);
        title.text = titleText;
        title.color = newBest ? Theme.Highlight : Theme.Text;
        title.rectTransform.sizeDelta = new Vector2(820f, 140f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 410f);

        Text scoreLabel = CreateOverlayText(panel, "ScoreLabel", 44);
        scoreLabel.text = "SCORE";
        scoreLabel.color = Theme.TextMuted;
        scoreLabel.rectTransform.anchoredPosition = new Vector2(0f, 295f);

        Text scoreValue = CreateOverlayText(panel, "ScoreValue", 150);
        scoreValue.text = "0";
        scoreValue.color = Theme.Text;
        scoreValue.rectTransform.sizeDelta = new Vector2(820f, 180f);
        scoreValue.rectTransform.anchoredPosition = new Vector2(0f, 190f);

        Text best = CreateOverlayText(panel, "Best", 50);
        if (dailyMode)
        {
            best.text = "DAILY  GOAL " + LevelManager.Instance.currentLevelData.targetScore;
        }
        else if (isLevelMode)
        {
            int levelNo = LevelManager.Instance.currentLevelData.levelNumber;
            int target = LevelManager.Instance.currentLevelData.targetScore;
            best.text = "LEVEL " + levelNo + "  GOAL " + target;
        }
        else
        {
            best.text = "BEST " + Mathf.Max(PlayerPrefs.GetInt("HighScore", 0), finalScore);
        }
        best.color = Theme.Highlight;
        best.rectTransform.anchoredPosition = new Vector2(0f, 80f);

        // Bu oyunun istatistikleri: iki kutu
        CreateStatBox(panel, new Vector2(-190f, -20f), "LINES", linesThisGame.ToString());
        CreateStatBox(panel, new Vector2(190f, -20f), "BEST COMBO", bestComboThisGame.ToString());

        Button playAgain = CreateOverlayButton(panel, isLevelMode ? "TRY AGAIN" : "PLAY AGAIN", Theme.Primary, new Vector2(0f, -275f), new Vector2(640f, 150f), 70);
        Button menu = CreateOverlayButton(panel, isLevelMode && !dailyMode ? "LEVELS" : "MENU", Theme.Neutral, new Vector2(0f, -425f), new Vector2(420f, 110f), 52);

        // Kazanilan para (sadece klasik mod)
        if (coinsEarned > 0)
        {
            Image coinIcon = UIKit.Icon(panel, UIKit.CoinSprite, new Vector2(-110f, -140f), new Vector2(64f, 64f), Color.white);
            Text coinText = CreateOverlayText(panel, "Coins", 50);
            coinText.text = "+" + coinsEarned;
            coinText.color = Theme.Highlight;
            coinText.alignment = TextAnchor.MiddleLeft;
            coinText.rectTransform.sizeDelta = new Vector2(300f, 70f);
            coinText.rectTransform.anchoredPosition = new Vector2(80f, -140f);
            coinIcon.raycastTarget = false;
        }

        int chosen = 0;
        playAgain.onClick.AddListener(() => { if (chosen == 0) chosen = 1; });
        menu.onClick.AddListener(() => { if (chosen == 0) chosen = 2; });

        if (newBest)
        {
            Haptics.Heavy();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayComboExplosion();
            for (int i = 0; i < BlockSkin.Palette.Length; i++)
            {
                BlockFx.I.Burst(transform.position + new Vector3(Random.Range(-3f, 3f), Random.Range(1f, 4f), 0f), BlockSkin.Palette[i], 6, 1.4f);
            }
        }

        // Skor sayarak yukselir
        float t = 0f;
        const float countDuration = 0.9f;
        while (t < countDuration && chosen == 0)
        {
            t += Time.unscaledDeltaTime;
            float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / countDuration), 3f);
            scoreValue.text = Mathf.RoundToInt(finalScore * p).ToString();
            yield return null;
        }
        scoreValue.text = finalScore.ToString();

        while (chosen == 0) yield return null;

        Destroy(canvasObj);
        onChoice?.Invoke(chosen);
    }

    void CreateStatBox(Transform parent, Vector2 position, string label, string value)
    {
        GameObject box = new GameObject("Stat_" + label);
        box.transform.SetParent(parent, false);
        Image bg = box.AddComponent<Image>();
        bg.sprite = NeoUI.RoundedSprite;
        bg.type = Image.Type.Sliced;
        bg.color = Theme.SurfaceSoft;
        bg.raycastTarget = false;
        bg.rectTransform.sizeDelta = new Vector2(330f, 140f);
        bg.rectTransform.anchoredPosition = position;

        Text valueText = CreateOverlayText(box.transform, "Value", 72);
        valueText.text = value;
        valueText.color = Theme.Text;
        valueText.rectTransform.anchoredPosition = new Vector2(0f, 14f);

        Text labelText = CreateOverlayText(box.transform, "Label", 32);
        labelText.text = label;
        labelText.color = Theme.TextMuted;
        labelText.rectTransform.anchoredPosition = new Vector2(0f, -42f);
    }

    IEnumerator FreezeBoardWave()
    {
        SyncGridStatus();
        for (int y = columns - 1; y >= 0; y--)
        {
            bool any = false;
            for (int x = 0; x < rows; x++)
            {
                GameObject block = gridStatus[x, y];
                if (block == null) continue;
                SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                if (useJewelStyle) sr.sprite = BlockSkin.GetBlockSprite(BlockSkin.FrozenColor);
                else sr.color = BlockSkin.FrozenColor;
                any = true;
            }
            if (any)
            {
                if (SoundManager.Instance != null && y % 2 == 0) SoundManager.Instance.PlayBlockPlace();
                yield return new WaitForSeconds(0.06f);
            }
        }
        yield return new WaitForSeconds(0.35f);
    }

    // "Devam etmek ister misin?" penceresi. Geri sayim biterse veya "No thanks" denirse false doner.
    IEnumerator ShowRevivePopup(System.Action<bool> onResult)
    {
        GameObject canvasObj = CreateOverlayCanvas("ReviveCanvas", 300);

        CreateDim(canvasObj.transform);
        RectTransform panelRect = CreateNeonPanel(canvasObj.transform, new Vector2(860f, 940f), Theme.Neutral);
        Transform panelT = panelRect;
        StartCoroutine(PopIn(panelRect.parent as RectTransform));

        Text title = CreateOverlayText(panelT, "Title", 78);
        title.text = "NO MORE MOVES";
        title.color = Theme.Highlight;
        title.rectTransform.sizeDelta = new Vector2(820f, 140f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 340f);

        // Geri sayim halkasi gorunumu: yuvarlak neon zemin + buyuk sayi
        GameObject ringObj = new GameObject("CountdownRing");
        ringObj.transform.SetParent(panelT, false);
        Image ring = ringObj.AddComponent<Image>();
        ring.sprite = NeoUI.RoundedSprite;
        ring.type = Image.Type.Sliced;
        ring.color = Theme.Primary;
        ring.rectTransform.sizeDelta = new Vector2(250f, 250f);
        ring.rectTransform.anchoredPosition = new Vector2(0f, 110f);
        ring.raycastTarget = false;

        Text countdown = CreateOverlayText(ringObj.transform, "Countdown", 150);
        countdown.rectTransform.sizeDelta = new Vector2(250f, 250f);

        Button continueButton = CreateOverlayButton(panelT, "CONTINUE", Theme.Positive, new Vector2(0f, -140f), new Vector2(640f, 170f), 78);
        Text hint = CreateOverlayText(panelT, "Hint", 36);
        hint.text = "WATCH A SHORT VIDEO TO CLEAR ROWS";
        hint.color = Theme.TextMuted;
        hint.rectTransform.sizeDelta = new Vector2(820f, 80f);
        hint.rectTransform.anchoredPosition = new Vector2(0f, -262f);

        Button noThanksButton = CreateOverlayButton(panelT, "NO THANKS", new Color(1f, 1f, 1f, 0f), new Vector2(0f, -370f), new Vector2(420f, 100f), 46);

        bool decided = false;
        bool waitingForAd = false;
        bool result = false;

        continueButton.onClick.AddListener(() =>
        {
            if (decided || waitingForAd) return;
            waitingForAd = true;
            continueButton.interactable = false;
            noThanksButton.interactable = false;
            AdsManager.Instance.ShowReviveRewarded(success =>
            {
                waitingForAd = false;
                result = success;
                decided = true;
                if (!success) StartCoroutine(ShowFloatingText("AD NOT READY", Color.red));
            });
        });
        noThanksButton.onClick.AddListener(() =>
        {
            if (decided || waitingForAd) return;
            decided = true;
        });

        Vector3 buttonBaseScale = continueButton.transform.localScale;
        float remaining = reviveCountdownSeconds;
        float adWait = 0f;
        while (!decided)
        {
            if (!waitingForAd)
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining <= 0f) break; // Sure doldu
                countdown.text = Mathf.CeilToInt(remaining).ToString();
                // Devam butonu hafifce nabiz atsin
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
                continueButton.transform.localScale = buttonBaseScale * pulse;
            }
            else
            {
                adWait += Time.unscaledDeltaTime;
                if (adWait > 60f) break; // SDK cevap vermezse takilma
            }
            yield return null;
        }

        Destroy(canvasObj);
        onResult?.Invoke(result);
    }

    Button CreateOverlayButton(Transform parent, string label, Color bgColor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = NeoUI.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = bgColor;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateOverlayText(go.transform, "Label", fontSize);
        text.text = label;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    // Revive: en dolu satirlari temizler (puansiz), bloklari dusurur ve 3 yerlesebilir blok verir
    IEnumerator ReviveBoard()
    {
        SyncGridStatus();

        List<int> rowsToClear = Enumerable.Range(0, columns)
            .Select(y => new { y, fill = Enumerable.Range(0, rows).Count(x => gridStatus[x, y] != null) })
            .Where(r => r.fill > 0)
            .OrderByDescending(r => r.fill)
            .Take(Mathf.Max(1, reviveClearRows))
            .Select(r => r.y)
            .ToList();

        List<GameObject> blocks = new List<GameObject>();
        foreach (int y in rowsToClear)
        {
            for (int x = 0; x < rows; x++)
            {
                if (gridStatus[x, y] == null) continue;
                GameObject obj = gridStatus[x, y];
                BlockUnit bu = obj.GetComponent<BlockUnit>();
                if (bu != null) bu.isDying = true;
                blocks.Add(obj);
                gridStatus[x, y] = null;
            }
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayComboExplosion();
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.3f, 0.15f);
        Haptics.Heavy();
        foreach (var b in blocks)
        {
            if (b != null) BlockFx.I.Burst(b.transform.position, BlockUnit.GetColor(b), 3);
        }

        // Hizli sonme animasyonu
        float duration = 0.3f;
        float elapsed = 0f;
        List<SpriteRenderer> renderers = blocks.Select(b => b.GetComponent<SpriteRenderer>()).ToList();
        List<Color> startColors = renderers.Select(r => r != null ? r.color : Color.white).ToList();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;
                Color c = startColors[i];
                c.a = a;
                renderers[i].color = c;
            }
            yield return null;
        }
        foreach (var b in blocks) SafeDestroy(b);

        SyncGridStatus();
        DropBlocksAfterClear(rowsToClear);

        // Oyunu tekrar ac
        isGameOverRunning = false;
        isGameActive = true;
        comboStreak = 0;
        movesSinceClear = 0;
        RefreshBlocksWithGuarantee(true); // Kaydeder ve geri al'i sifirlar
        UpdateRefreshButtonState();
        UpdateUndoButtonState();

        BlockFx.I.Praise("SECOND CHANCE!", Theme.Positive);
    }

    // Oyunu basla
    public void RestartGame()
    {
        isGameOverRunning = false;
        levelCompleteSequenceRunning = false;
        StopRefreshButtonPulse();

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
        for (int i = 0; i < 3; i++) ClearSlot(i);

        // Yenileme hakkini sifirla (Oyuncu tekrar 1 hakka sahip olsun)
        remainingRefreshes = maxRefreshes;
        UpdateRefreshButtonState();
        remainingUndos = maxUndos;
        hasUndoSnapshot = false;
        UpdateUndoButtonState();
        comboStreak = 0;
        movesSinceClear = 0;
        revivesUsed = 0;
        feverMovesLeft = 0;
        IsBombArmed = false;
        IsRotateArmed = false;
        linesThisGame = 0;
        bestComboThisGame = 0;
        bestAtGameStart = PlayerPrefs.GetInt("HighScore", 0);

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

    // Blok parcasinin gorunumu: parlak (renge ozel) sprite + rengi BlockUnit'te sakla
    public void SetBlockVisuals(GameObject obj, Color col, int order)
    {
        col.a = 1f;
        BlockUnit unit = obj.GetComponent<BlockUnit>();
        if (unit == null) unit = obj.AddComponent<BlockUnit>();
        unit.baseColor = col;
        unit.hasBaseColor = true;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.sortingOrder = order;
        if (useJewelStyle && BlockSkin.EmptyCell != null)
        {
            sr.sprite = BlockSkin.GetBlockSprite(col);
            sr.color = Color.white;
        }
        else
        {
            sr.color = col;
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
