using UnityEngine;

public class BlockUnit : MonoBehaviour
{
    // Blok patlatilmak uzere isaretlendi mi?
    // GridManager.SyncGridStatus tarafindan yoksayilmasi icin.
    public bool isDying = false;

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
                // GridManager tarafindan olusturulan hucrelerin adi "Cell_x_y" formatindadir.
                if (hit.name.StartsWith("Cell_"))
                {
                    return hit.transform;
                }
            }
        }
        return null;
    }
}
