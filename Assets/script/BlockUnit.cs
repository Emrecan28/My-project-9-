using UnityEngine;

public class BlockUnit : MonoBehaviour
{
    // Blok patlatilmak uzere isaretlendi mi?
    // GridManager.SyncGridStatus tarafindan yoksayilmasi icin.
    public bool isDying = false;

    // Blogun asil rengi. Parlak blok gorunumunde SpriteRenderer beyaz kalir ve renk
    // dokunun icindedir; kayit/onizleme/efektler rengi buradan okur.
    public Color baseColor = Color.white;
    public bool hasBaseColor = false;

    // Altindaki grid karesini (Tile) bulup donduren fonksiyon
    public Transform GetTileUnderMe()
    {
        // Physics2D.OverlapPointAll kullanarak o noktadaki TUM colliderlari bul
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (var hit in hits)
        {
            // 1. Kendimiz degilsek
            // 2. Parentimiz degilsek (Blok parcasi, blogun kendisine carpmasin)
            if (hit.gameObject != gameObject && hit.transform.parent != transform.parent)
            {
                // Grid hucresi kontrolu: Ismi "Cell_" ile basliyorsa
                if (hit.name.StartsWith("Cell_"))
                {
                    return hit.transform;
                }
            }
        }
        return null;
    }

    // Objenin blok rengini dondurur (BlockUnit varsa ondan, yoksa SpriteRenderer'dan)
    public static Color GetColor(GameObject obj)
    {
        if (obj == null) return Color.white;
        BlockUnit unit = obj.GetComponent<BlockUnit>();
        if (unit != null && unit.hasBaseColor) return unit.baseColor;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        return sr != null ? sr.color : Color.white;
    }
}
