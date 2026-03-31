using UnityEngine;
using System.Collections.Generic;

public class DraggableBlock : MonoBehaviour
{
    private Vector3 originalPosition;
    private Transform originalParent; // Original parent'i kaydet
    private Vector3 offset;
    private bool isDragging = false;
    private List<BlockUnit> childUnits = new List<BlockUnit>();
    private GridManager gridManager;

    [Header("--- Boyut Ayarlari ---")]
    public float slotScale = 0.45f; // Yuvadaki kucuk hali
    public float dragScale = 1.0f;  // Suruklerkenki normal hali

    // Orijinal şekil verisi (GridManager tarafından atanır)
    public List<Vector2Int> originalShape = new List<Vector2Int>();

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        originalPosition = transform.localPosition;
        originalParent = transform.parent; // Original parent'i kaydet
        UpdateChildUnits();

        // Baslangicta yuvada kucuk durmasini sagla
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        transform.localScale = Vector3.one * (slotScale / parentScale);
        
        // ANA OBJEYE COLLIDER EKLE - MUTLAKA!
        BoxCollider2D mainCollider = GetComponent<BoxCollider2D>();
        if (mainCollider == null)
        {
            mainCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Collider boyutunu dinamik olarak hesapla
        CalculateColliderBounds(mainCollider);
        
        mainCollider.isTrigger = false;
        mainCollider.enabled = true;
        
        // Camera'da Physics2DRaycaster olup olmadigini kontrol et
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
        {
            mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }
        
        // Child'lara mouse event'leri ekle
        Invoke(nameof(SetupChildMouseEvents), 0.1f);
    }
    
    public void SetupChildMouseEvents()
    {
        UpdateChildUnits();
        foreach (var unit in childUnits)
        {
            // Her child'a BlockMouseHandler ekle (zaten varsa ekleme)
            BlockMouseHandler existingHandler = unit.GetComponent<BlockMouseHandler>();
            if (existingHandler == null)
            {
                BlockMouseHandler handler = unit.gameObject.AddComponent<BlockMouseHandler>();
                handler.draggableBlock = this;
            }
            else if (existingHandler.draggableBlock != this)
            {
                // Eger baska bir DraggableBlock'a bagliysa, guncelle
                existingHandler.draggableBlock = this;
            }
        }
        
        // Collider'i guncelle (Sonradan child eklendiyse)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) CalculateColliderBounds(col);
    }
    
    void CalculateColliderBounds(BoxCollider2D collider)
    {
        UpdateChildUnits();
        if (childUnits.Count == 0) return;

        // Ilk child'i baz al
        Bounds bounds = new Bounds(childUnits[0].transform.localPosition, Vector3.zero);
        
        foreach(var unit in childUnits)
        {
            bounds.Encapsulate(unit.transform.localPosition);
        }
        
        // Bloklarin kendisi de yer kaplar (Genellikle 1 birim)
        // Bounds sadece merkez noktalari kapsar.
        // Ornegin (0,0) ve (1,0) varsa bounds center (0.5, 0), size (1, 0) olur.
        // Ama gercek kaplama alani (-0.5, -0.5) to (1.5, 0.5) yani size (2, 1).
        // Yani size'a +1 eklemek (veya scale'e gore) gerekir.
        // Child'larin scale'i genellikle 1 (parent scale ile yonetiliyor).
        
        collider.size = new Vector2(bounds.size.x + 1f, bounds.size.y + 1f);
        collider.offset = bounds.center;
    }
    
    void UpdateChildUnits()
    {
        childUnits.Clear();
        childUnits.AddRange(GetComponentsInChildren<BlockUnit>());
    }
    
    // Public method - child'lardan cagrilacak
    public void HandleMouseDown()
    {
        OnMouseDown();
    }
    
    public void HandleMouseDrag()
    {
        OnMouseDrag();
    }
    
    public void HandleMouseUp()
    {
        OnMouseUp();
    }

    private Coroutine pickupCoroutine;

    void OnMouseDown()
    {
        // Oyun aktif degilse hicbir sey yapma
        if (gridManager != null && !gridManager.isGameActive) return;

        UpdateChildUnits(); // ChildUnits'i guncelle
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;

        // --- GORSEL EFEKTLER (Visual Feedback) ---
        // 1. Trace Effect (Iz Efekti): KALDIRILDI (Kullanici istegi uzerine)
        // CreateTraceEffect();

        // 2. Pickup Animation (Tutma Efekti): Hafifce ziplayarak buyume
        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
        pickupCoroutine = StartCoroutine(AnimatePickup());
        // -----------------------------------------
        
        // Sorting order'i artir (ustte gorunsun)
        foreach (var unit in childUnits)
        {
            SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 100;
        }
    }

    void CreateTraceEffect()
    {
        // Gecici bir obje olustur (Hayalet)
        GameObject traceObj = new GameObject("TraceEffect");
        traceObj.transform.position = transform.position;
        traceObj.transform.rotation = transform.rotation;
        traceObj.transform.localScale = transform.localScale; // Mevcut (kucuk) boyutunda basla

        // Child unitlerin kopyalarini olustur
        foreach (var unit in childUnits)
        {
            SpriteRenderer originalSr = unit.GetComponent<SpriteRenderer>();
            if (originalSr != null)
            {
                GameObject clone = new GameObject("TracePart");
                clone.transform.SetParent(traceObj.transform);
                clone.transform.localPosition = unit.transform.localPosition;
                clone.transform.localRotation = unit.transform.localRotation;
                clone.transform.localScale = unit.transform.localScale;

                SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
                cloneSr.sprite = originalSr.sprite;
                // Rengi ayni al ama biraz seffaf yap
                cloneSr.color = new Color(originalSr.color.r, originalSr.color.g, originalSr.color.b, 0.4f);
                cloneSr.sortingOrder = 5; // Bloklarin altinda kalsin
            }
        }

        // Efekti yonetmek icin GridManager veya bagimsiz bir coroutine kullan
        // DraggableBlock uzerinde baslatirsak ve obje yok olursa sorun olabilir, ama TraceObj bagimsiz
        // O yuzden TraceObj uzerine gecici bir script ekleyebiliriz veya burada MonoBehavior oldugu icin
        // bu script uzerinden coroutine baslatabiliriz (obje yok olmadigi surece)
        StartCoroutine(AnimateTrace(traceObj));
    }

    System.Collections.IEnumerator AnimateTrace(GameObject obj)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = obj.transform.localScale;
        Vector3 targetScale = startScale * 1.3f; // Oldugu yerde genislesin

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Genisle
            obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // Sol (Fade Out)
            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(0.4f, 0f, t);
                    sr.color = c;
                }
            }

            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    System.Collections.IEnumerator AnimatePickup()
    {
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.one * (dragScale / parentScale);
        Vector3 punchScale = targetScale * 1.2f; // Hedef boyuttan %20 daha buyuk (Punch)

        float duration = 0.1f; // Cok hizli
        float elapsed = 0f;

        // 1. Asama: Hizlica buyu (Punch)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth step
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(startScale, punchScale, t);
            yield return null;
        }

        // 2. Asama: Normale don (Settle)
        elapsed = 0f;
        duration = 0.08f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(punchScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    void OnMouseDrag()
    {
        // Oyun aktif degilse hicbir sey yapma
        if (gridManager != null && !gridManager.isGameActive) return;

        if (!isDragging) return;
        
        // Parmağın altında kalmaması için bloğu parmağın "üstüne" taşıyoruz.
        // Y eksenine eklenecek değer (Offset). Ne kadar büyük olursa o kadar yukarıda durur.
        Vector3 touchOffset = new Vector3(0, 2.5f, 0); 
        
        transform.position = GetMouseWorldPos() + offset + touchOffset;
        
        // Preview (Onizleme)
        if (gridManager != null)
        {
            bool isValid;
            List<Vector2Int> projectedPos = GetProjectedGridPositions(out isValid);
            
            if (projectedPos.Count > 0)
            {
                gridManager.HighlightPlacement(projectedPos, isValid);
            }
            else
            {
                gridManager.ClearHighlight();
            }
        }
    }

    void OnMouseUp()
    {
        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
        
        isDragging = false;
        
        // Vurguyu temizle
        if (gridManager != null) gridManager.ClearHighlight();

        // Grid uzerinde mi kontrol et
        bool isOverGrid = false;
        UpdateChildUnits(); // ChildUnits'i guncelle
        foreach (var unit in childUnits)
        {
            Transform tile = unit.GetTileUnderMe();
            if (tile != null)
            {
                isOverGrid = true;
                break;
            }
        }

        if (isOverGrid && CanPlace())
        {
            PlaceOnGrid();
        }
        else
        {
            // YERLESTIRILEMEZSE veya GRID DISINDAYSA: Eski yerine don ve tekrar kucult
            ReturnToSlot(true); // Hata efekti ile don
        }
    }
    
    void ReturnToSlot(bool withErrorEffect = false)
    {
        // Sorting order'i geri al (once bunu yapalim ki gecis temiz olsun)
        foreach (var unit in childUnits)
        {
            SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 10;
        }

        // 1. Durum: Original parent hala mevcutsa oraya don
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero; // Tam merkeze oturt
            
            // Scale'i ayarla
            float parentScale = originalParent.localScale.x;
            transform.localScale = Vector3.one * (slotScale / parentScale);
        }
        else
        {
            // 2. Durum: Parent kaybolmussa (nadir olur ama guvenlik icin), GridManager'dan bos slot bul
            if (gridManager != null)
            {
                bool foundSlot = false;
                for (int i = 0; i < 3; i++)
                {
                    Transform slot = gridManager.GetSlotTransform(i);
                    if (slot != null && slot.childCount == 0)
                    {
                        transform.SetParent(slot);
                        transform.localPosition = Vector3.zero;
                        float parentScale = slot.localScale.x;
                        transform.localScale = Vector3.one * (slotScale / parentScale);
                        originalParent = slot; // Parent bilgisini guncelle
                        foundSlot = true;
                        break;
                    }
                }
                
                // Eger hicbir slot bulunamazsa (cok cok nadir), objeyi yok etme, oldugu yerde birak (hata olmamasi icin)
                if (!foundSlot)
                {
                    Debug.LogWarning("Blok geri donecek slot bulamadi!");
                }
            }
        }
        
        // Hata efekti isteniyorsa calistir
        if (withErrorEffect)
        {
            StartCoroutine(PlayErrorEffect());
        }

        // Geri donus sonrasinda hareket kalmis mi kontrol et
        if (gridManager != null)
        {
            gridManager.SyncGridStatus(); // Grid durumunu zorla guncelle (Game Over check oncesi)
            gridManager.TryGameOverCheck();
        }
    }

    System.Collections.IEnumerator PlayErrorEffect()
    {
        // Renkleri kirmizi yap ve titret
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        List<Color> originalColors = new List<Color>();
        
        foreach (var unit in childUnits)
        {
            SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                renderers.Add(sr);
                originalColors.Add(sr.color);
                sr.color = Color.red; // Kirmizi yap
            }
        }
        
        // Titreme efekti
        Vector3 startPos = transform.localPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float xOffset = Mathf.Sin(elapsed * 50f) * 10f; // Hizli titreme
            transform.localPosition = startPos + new Vector3(xOffset, 0, 0);
            yield return null;
        }
        
        // Eski haline dondur
        transform.localPosition = startPos;
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }
    }

    bool CanPlace()
    {
        UpdateChildUnits(); // ChildUnits'i guncelle
        if (childUnits.Count == 0) return false;

        // YA HEP YA HIC KURALI:
        // Tum parcalar gecerli ve bos bir grid hucresine denk gelmeli.
        // Tek bir parca bile disarida kalsa veya dolu yere gelse islem iptal.
        foreach (var unit in childUnits)
        {
            Transform tile = unit.GetTileUnderMe();
            
            // 1. Grid disinda mi?
            if (tile == null) return false;
            
            // 2. Gecerli bir grid koordinati var mi?
            Vector2Int? gridPos = gridManager.WorldPosToGrid(tile.position);
            if (!gridPos.HasValue) return false; // Koordinat hesaplanamadi -> Gecersiz
            
            // 3. O koordinat dolu mu?
            if (gridManager.IsGridOccupied(gridPos.Value)) return false; // Dolu -> Gecersiz
        }
        
        return true;
    }

    void PlaceOnGrid()
    {
        // SES EFEKTI: Blok yerlestirme sesi
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBlockPlace();
        }

        UpdateChildUnits(); // ChildUnits'i guncelle
        if (gridManager != null)
        {
            gridManager.CaptureUndoSnapshot();
        }
        
        // Guvenlik onlemi: Eger yerlestirme sirasinda bir sorun olursa (nadiren),
        // parcalarin yarisinin yerlesip yarisinin kalmasini onlemek icin tekrar kontrol.
        // Ancak performans icin bunu CanPlace'e guvenerek yapiyoruz.
        
        foreach (var unit in childUnits)
        {
            Transform tile = unit.GetTileUnderMe();
            // CanPlace() true dondugu icin teorik olarak tile null olamaz ve gridPos valid olmalidir.
            
            if (tile != null)
            {
                // Grid koordinatini hesapla
                Vector2Int? gridPos = gridManager.WorldPosToGrid(tile.position);
                
                if (gridPos.HasValue)
                {
                    // Grid koordinatindan tam pozisyonu al (yamuk durmayi onlemek icin)
                    Vector3 exactPosition = gridManager.GridToWorldPos(gridPos.Value);
                    unit.transform.position = exactPosition;
                    unit.transform.SetParent(gridManager.transform);
                    if (unit.GetComponent<Collider2D>()) unit.GetComponent<Collider2D>().enabled = false;
                    
                    // GridStatus'e kaydet
                    gridManager.PlaceBlockAtGrid(gridPos.Value, unit.gameObject);
                }
            }
        }
        
        // Blok yerlestirildi, satir/sutun kontrolu yap
        gridManager.CheckForLines();
        
        // Objeyi yok et
        Destroy(gameObject);
        
        // Tum slotlar bos mu kontrol et ve spawn et (coroutine ile bir sonraki frame'de)
        gridManager.CheckAndSpawnIfAllEmpty();
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = 10f;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
    
    // Potansiyel yerlesim yerlerini hesapla (Preview icin) - Guncellendi
    List<Vector2Int> GetProjectedGridPositions(out bool isValid)
    {
        isValid = true;
        UpdateChildUnits();
        if (childUnits.Count == 0) return new List<Vector2Int>();
        
        List<Vector2Int> positions = new List<Vector2Int>();
        
        foreach (var unit in childUnits)
        {
            Transform tile = unit.GetTileUnderMe();
            
            // 1. Grid disinda mi?
            if (tile == null) 
            {
                isValid = false;
                continue; // Highlight edemeyiz
            }
            
            // 2. Gecerli bir grid koordinati var mi?
            Vector2Int? gridPos = gridManager.WorldPosToGrid(tile.position);
            if (!gridPos.HasValue) 
            {
                isValid = false;
                continue;
            }
            
            // Pozisyonu ekle (Grid uzerinde oldugu icin highlight edilebilir)
            positions.Add(gridPos.Value);

            // 3. O koordinat dolu mu?
            if (gridManager.IsGridOccupied(gridPos.Value)) 
            {
                isValid = false;
            }
        }
        
        return positions;
    }
}
