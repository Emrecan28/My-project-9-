using UnityEngine;

// Oyun sahnesinin arkasina ana menudeki synthwave arka plani koyar ve kameraya gore olcekler.
// Sahneye elle eklemeye gerek yok: GridManager oyun basinda Ensure() cagirir.
public class GameBackground : MonoBehaviour
{
    const string ResourcePath = "NeoBackground";

    SpriteRenderer sr;
    Camera cam;
    float lastOrtho = -1f, lastAspect = -1f;

    // gradient = true: Block Blast tarzi mavi gradyan (kodla uretilir)
    // gradient = false: ana menudeki synthwave resmi
    public static void Ensure(bool gradient)
    {
        if (FindAnyObjectByType<GameBackground>() != null) return;

        Sprite sprite;
        if (gradient)
        {
            Texture2D tex = BlockSkin.BuildBackgroundTexture();
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        else
        {
            sprite = NeoUI.LoadSprite(ResourcePath);
        }
        if (sprite == null) return; // Resim yoksa eski gorunum kalir

        GameObject go = new GameObject("GameBackground");
        var bg = go.AddComponent<GameBackground>();
        bg.sr = go.AddComponent<SpriteRenderer>();
        bg.sr.sprite = sprite;
        bg.sr.sortingOrder = -100;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || sr == null || sr.sprite == null) return;

        // Kamera boyutu degistiyse (GridManager kamerayi gride sigdirir) yeniden olcekle
        if (!Mathf.Approximately(cam.orthographicSize, lastOrtho) || !Mathf.Approximately(cam.aspect, lastAspect))
        {
            lastOrtho = cam.orthographicSize;
            lastAspect = cam.aspect;

            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            Vector2 spriteSize = sr.sprite.bounds.size;
            float scale = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y); // Ekrani tamamen kapla
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        Vector3 p = cam.transform.position;
        transform.position = new Vector3(p.x, p.y, p.z + 50f);
    }
}
