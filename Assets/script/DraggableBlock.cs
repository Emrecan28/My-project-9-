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
    public float pickupColliderPadding = 0.5f;
    public float dropSnapMaxDistanceMultiplier = 0.75f;

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
        if (spawnPopDelay >= 0f) StartCoroutine(SpawnPop(spawnPopDelay, transform.localScale));

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

        float padding = 1f + Mathf.Max(0f, pickupColliderPadding);
        collider.size = new Vector2(bounds.size.x + padding, bounds.size.y + padding);
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
    private Coroutine errorCoroutine;

    // Yeni gelen parca tepside "pop" ile belirsin (GridManager ayarlar; -1 = animasyon yok)
    [HideInInspector] public float spawnPopDelay = -1f;

    Color PieceColor
    {
        get
        {
            if (childUnits.Count == 0) UpdateChildUnits();
            return childUnits.Count > 0 ? BlockUnit.GetColor(childUnits[0].gameObject) : Color.white;
        }
    }

    // Ayni anda sadece bir blok tutulabilir (iki parmakla iki blok suruklenmesin)
    private static DraggableBlock activeDrag;
    public static bool IsDragging => activeDrag != null;

    void OnDisable()
    {
        if (activeDrag == this) activeDrag = null;
    }

    void OnMouseDown()
    {
        if (PauseManager.IsPauseMenuOpen) return;

        // Oyun aktif degilse hicbir sey yapma
        if (gridManager != null && !gridManager.isGameActive) return;

        // Bomba hedefi secilirken parca tutulmasin
        if (gridManager != null && gridManager.IsBombArmed) return;

        // ROTATE modu: dokunulan parca doner, surukleme baslamaz
        if (gridManager != null && gridManager.IsRotateArmed)
        {
            gridManager.RotateSinglePiece(this);
            return;
        }

        // Baska bir blok zaten tutuluyorsa bunu alma
        if (activeDrag != null && activeDrag != this) return;
        activeDrag = this;

        // Hata titremesi suruyorsa durdur ve blogu yerine oturt
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
            transform.localPosition = Vector3.zero;
        }

        UpdateChildUnits(); // ChildUnits'i guncelle
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;

        // --- GORSEL EFEKTLER (Visual Feedback) ---
        // Pickup Animation (Tutma Efekti): Hafifce ziplayarak buyume
        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
        pickupCoroutine = StartCoroutine(AnimatePickup());
        Haptics.Light(); // tutma aninda kucuk "tik"
        // -----------------------------------------

        // Sorting order'i artir (ustte gorunsun)
        foreach (var unit in childUnits)
        {
            SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 100;
        }
    }



    // Tepside belirme: 0'dan buyuyup hafif tasarak yerine oturur
    System.Collections.IEnumerator SpawnPop(float delay, Vector3 target)
    {
        transform.localScale = Vector3.zero;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float t = 0f;
        const float duration = 0.28f;
        while (t < duration)
        {
            if (isDragging) yield break; // Oyuncu hemen tuttuysa birak
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float s = 1f + 2.4f * Mathf.Pow(p - 1f, 3f) + 1.4f * Mathf.Pow(p - 1f, 2f); // ease-out-back
            transform.localScale = target * s;
            yield return null;
        }
        if (!isDragging) transform.localScale = target;
    }

    System.Collections.IEnumerator AnimatePickup()
    {
        float parentScale = transform.parent != null ? transform.parent.localScale.x : 1f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.one * (dragScale / parentScale);
        Vector3 punchScale = targetScale * 1.06f; // Hafif tasma, sonra tam tahta boyutu

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
        if (PauseManager.IsPauseMenuOpen) return;

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
                gridManager.HighlightPlacement(projectedPos, isValid, PieceColor);
            }
            else
            {
                gridManager.ClearHighlight();
            }
        }
    }

    void OnMouseUp()
    {
        if (activeDrag == this) activeDrag = null;
        if (!isDragging) return; // Bu blok hic tutulmadiysa (or. baska blok suruklenirken) islem yapma

        if (PauseManager.IsPauseMenuOpen)
        {
            if (isDragging) ReturnToSlot(false);
            isDragging = false;
            if (gridManager != null) gridManager.ClearHighlight();
            return;
        }

        if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);

        isDragging = false;

        // Surukleme sirasinda oyun durduysa (game over, reklam vb.) blogu yerine koy
        if (gridManager != null && !gridManager.isGameActive)
        {
            gridManager.ClearHighlight();
            ReturnToSlot(false);
            return;
        }

        // Vurguyu temizle
        if (gridManager != null) gridManager.ClearHighlight();

        // Grid uzerinde mi kontrol et
        bool isOverGrid = false;
        UpdateChildUnits(); // ChildUnits'i guncelle
        foreach (var unit in childUnits)
        {
            if (TryGetGridPosForUnit(unit, out _))
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
        Vector3 fromWorld = transform.position;
        Vector3 fromWorldScale = transform.lossyScale;

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
        // Parca bulundugu yerden tepsideki yerine suzulerek doner (Block Blast tarzi, kirmizi uyari yok)
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(GlideBack(fromWorld, fromWorldScale));

        // Geri donus sonrasinda hareket kalmis mi kontrol et
        if (gridManager != null)
        {
            gridManager.SyncGridStatus(); // Grid durumunu zorla guncelle (Game Over check oncesi)
            gridManager.TryGameOverCheck();
        }
    }

    System.Collections.IEnumerator GlideBack(Vector3 fromWorld, Vector3 fromWorldScale)
    {
        Transform parent = transform.parent;
        Vector3 targetLocalScale = transform.localScale;
        Vector3 fromLocal = parent != null ? parent.InverseTransformPoint(fromWorld) : fromWorld;
        float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.x) : 1f;
        Vector3 fromLocalScale = fromWorldScale / parentScale;

        float t = 0f;
        const float duration = 0.18f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            transform.localPosition = Vector3.Lerp(fromLocal, Vector3.zero, ease);
            transform.localScale = Vector3.Lerp(fromLocalScale, targetLocalScale, ease);
            yield return null;
        }
        transform.localPosition = Vector3.zero;
        transform.localScale = targetLocalScale;

        foreach (var unit in childUnits)
        {
            SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 10;
        }
        errorCoroutine = null;
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

        // Titreme efekti. Genlik dunya biriminde verilir ve slotun olcegine bolunur;
        // eskiden 10 yerel birim slot olcegiyle carpilip blogu ekrandan disari firlatiyordu.
        Vector3 startPos = transform.localPosition;
        float duration = 0.3f;
        float elapsed = 0f;
        float parentScale = transform.parent != null ? Mathf.Max(0.0001f, transform.parent.lossyScale.x) : 1f;
        float amplitude = 0.15f / parentScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / duration);
            float xOffset = Mathf.Sin(elapsed * 50f) * amplitude * fade; // Hizli titreme
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
        errorCoroutine = null;
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
            if (!TryGetGridPosForUnit(unit, out var gridPos)) return false;

            // 3. O koordinat dolu mu?
            if (gridManager.IsGridOccupied(gridPos)) return false; // Dolu -> Gecersiz
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

        // Patlama dalgasinin merkezi ve rengi icin yerlesimi bildir
        List<Vector2Int> placedCells = new List<Vector2Int>();
        foreach (var unit in childUnits)
        {
            if (TryGetGridPosForUnit(unit, out var cellPos)) placedCells.Add(cellPos);
        }
        gridManager.NotePlacement(placedCells, PieceColor);

        foreach (var unit in childUnits)
        {
            if (TryGetGridPosForUnit(unit, out var gridPos))
            {
                Vector3 exactPosition = gridManager.GridToWorldPos(gridPos);
                unit.transform.position = exactPosition;
                unit.transform.SetParent(gridManager.transform);
                if (unit.GetComponent<Collider2D>()) unit.GetComponent<Collider2D>().enabled = false;
                gridManager.PlaceBlockAtGrid(gridPos, unit.gameObject);
                SpriteRenderer placedSr = unit.GetComponent<SpriteRenderer>();
                if (placedSr != null) placedSr.sortingOrder = 5;
                BlockFx.I.Bounce(unit.transform, 0.12f, 0.15f); // Yerine "tik" diye oturma hissi
            }
        }

        // Yerlestirme puani + hafif titresim
        gridManager.AddPlacementScore(childUnits.Count);
        Haptics.Light();

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
        Camera cam = Camera.main;
        if (cam == null && Camera.allCamerasCount > 0)
        {
            cam = Camera.allCameras[0];
        }
        if (cam == null) return Vector3.zero;
        return cam.ScreenToWorldPoint(mousePoint);
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
            if (!TryGetGridPosForUnit(unit, out var gridPos))
            {
                isValid = false;
                continue;
            }

            // Pozisyonu ekle (Grid uzerinde oldugu icin highlight edilebilir)
            positions.Add(gridPos);

            // 3. O koordinat dolu mu?
            if (gridManager.IsGridOccupied(gridPos))
            {
                isValid = false;
            }
        }

        return positions;
    }

    bool TryGetGridPosForUnit(BlockUnit unit, out Vector2Int gridPos)
    {
        gridPos = default;
        if (gridManager == null || unit == null) return false;

        Vector2Int? candidate = gridManager.WorldPosToGrid(unit.transform.position);
        if (!candidate.HasValue) return false;

        float threshold = (gridManager.cellSize + gridManager.spacing) * dropSnapMaxDistanceMultiplier;
        Vector3 targetWorld = gridManager.GridToWorldPos(candidate.Value);
        if (Vector2.Distance(unit.transform.position, targetWorld) > threshold) return false;

        gridPos = candidate.Value;
        return true;
    }
}
