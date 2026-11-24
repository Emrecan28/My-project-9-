using System.Collections.Generic;
using UnityEngine;

// NEOBLOCK ortak gorsel stili: font, renkler, yuvarlak koseli panel sprite'i.
// Kodla olusturulan tum pencereler ve yazilar buradan beslenir; boylece oyunun her yeri ayni gorunur.
public static class NeoUI
{
    // Renkler Theme.cs'den gelir (eski isimler uyumluluk icin duruyor)
    public static Color Cyan => Theme.Neutral;
    public static Color Pink => Theme.Primary;
    public static Color Purple => Theme.Neutral;
    public static Color Gold => Theme.Highlight;
    public static Color Green => Theme.Positive;
    public static Color PanelDark => Theme.WithAlpha(Theme.Surface, 0.98f);
    public static Color Dim => Theme.Dim;

    static Font cachedFont;
    static Sprite cachedRounded;
    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // Bungee fontu (Assets/Resources/Fonts/NeoFont.ttf). Bulunamazsa Unity'nin dahili fontu.
    public static Font Font
    {
        get
        {
            if (cachedFont == null) cachedFont = Resources.Load<Font>("Fonts/NeoFont");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }
    }

    // Yuvarlak koseli, 9-slice beyaz panel (renk Image.color ile verilir)
    public static Sprite RoundedSprite
    {
        get
        {
            if (cachedRounded == null) cachedRounded = CreateRoundedSprite(96, 32);
            return cachedRounded;
        }
    }

    // Resources altindaki bir resmi sprite olarak yukler (import ayarindan bagimsiz)
    public static Sprite LoadSprite(string resourcePath)
    {
        if (spriteCache.TryGetValue(resourcePath, out Sprite s) && s != null) return s;

        // Once dokunun tamamini al: Unity resmi "Multiple" (parcali) sprite olarak iceri
        // aktarmis olabilir; o durumda Load<Sprite> sadece ilk parcayi dondurur.
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null)
        {
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        else
        {
            s = Resources.Load<Sprite>(resourcePath);
        }
        spriteCache[resourcePath] = s;
        return s;
    }

    static Sprite CreateRoundedSprite(int size, int radius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Koseye en yakin daire merkezine uzaklik (yumusak kenar icin 1px gecis)
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    static Sprite gradientSprite;

    // UI arka planini oyundaki mavi gradyanla degistirir (menu ve level ekrani ayni gorunsun)
    public static void ApplyGradientBackground(UnityEngine.UI.Image image)
    {
        if (image == null) return;
        if (gradientSprite == null)
        {
            Texture2D tex = BlockSkin.BuildBackgroundTexture();
            gradientSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        image.sprite = gradientSprite;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    // Menu butonu: yuvarlak, parlak renkli zemin + Bungee yazi (eski piksel buton resmi yerine)
    // Esit olmayan (x/y farkli) olcegi sifirlar, gorunen boyutu korur; cocuklari da telafi eder
    public static void NormalizeScale(RectTransform rt)
    {
        if (rt == null) return;
        Vector3 s = rt.localScale;
        if (Mathf.Approximately(s.x, 1f) && Mathf.Approximately(s.y, 1f)) return;

        Vector2 visual = new Vector2(rt.rect.width * Mathf.Abs(s.x), rt.rect.height * Mathf.Abs(s.y));
        foreach (Transform child in rt)
        {
            RectTransform c = child as RectTransform;
            if (c == null) continue;
            c.anchoredPosition = Vector2.Scale(c.anchoredPosition, new Vector2(s.x, s.y));
            c.localScale = Vector3.Scale(c.localScale, new Vector3(s.x, s.y, 1f));
        }
        rt.localScale = Vector3.one;
        rt.sizeDelta = visual;
    }

    public static void StyleMenuButton(UnityEngine.UI.Button button, string label, Color color)
    {
        if (button == null) return;
        NormalizeScale(button.GetComponent<RectTransform>());
        var img = button.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.sprite = RoundedSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.2f;
            img.color = color;
        }

        var shadow = button.GetComponent<UnityEngine.UI.Shadow>();
        if (shadow == null) shadow = button.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = Theme.Darken(color, 0.55f);
        shadow.effectDistance = new Vector2(0f, -10f);

        var textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(button.transform, false);
        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.font = Font;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 200;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.18f, 0.12f);
        rt.anchorMax = new Vector2(0.95f, 0.88f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    // Yaziya neon parlama: renkli dis isik + koyu golge
    public static void AddGlow(GameObject textObj, Color glowColor)
    {
        var outline = textObj.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null) outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.85f);
        outline.effectDistance = new Vector2(4f, -4f);

        var shadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(0f, -8f);
    }
}
