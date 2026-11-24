using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Kodla kurulan tum menu/pencere arayuzleri icin ortak yapi taslari.
// Hepsi ayni olculeri, ayni fontu (Bungee) ve Theme renklerini kullanir; boylece her ekran ayni gorunur.
public static class UIKit
{
    public const float RefWidth = 1080f;
    public const float RefHeight = 1920f;

    // ---------------- CANVAS / PENCERE ----------------
    public static GameObject CreateCanvas(string name, int sortingOrder, bool persistent = false)
    {
        GameObject root = new GameObject(name);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        scaler.matchWidthOrHeight = 0f; // genislige gore: telefon boylari ne olursa olsun yatayda tasma yok
        root.AddComponent<GraphicRaycaster>();
        if (persistent) UnityEngine.Object.DontDestroyOnLoad(root);
        return root;
    }

    public static Image FullScreen(Transform parent, Color color, bool raycast = true)
    {
        GameObject go = new GameObject("Dim", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        Stretch(img.rectTransform);
        return img;
    }

    // Karartilmis arka plan + cerceveli yuvarlak pencere + baslik. Pencerenin govdesini dondurur.
    public static RectTransform CreateWindow(string name, string title, Vector2 size, out GameObject root, int sortingOrder = 500)
    {
        root = CreateCanvas(name, sortingOrder);
        FullScreen(root.transform, Theme.Dim);

        Image border = Panel(root.transform, "Border", size + new Vector2(14f, 14f), Theme.Neutral);
        Image body = Panel(border.transform, "Body", size, NeoUI.PanelDark);

        Text t = Label(body.transform, title, 80, Theme.Highlight, TextAnchor.MiddleCenter);
        t.rectTransform.sizeDelta = new Vector2(size.x - 60f, 120f);
        t.rectTransform.anchoredPosition = new Vector2(0f, size.y * 0.5f - 90f);

        // Kisa ekranlarda (iPad, eski iPhone) pencere tasmasin: gerekirse kucult
        float maxH = CanvasHeight * 0.92f;
        float scale = Mathf.Min(1f, maxH / (size.y + 14f));
        border.rectTransform.localScale = Vector3.one * scale;

        Runner.Instance.StartCoroutine(PopIn(border.rectTransform));
        return body.rectTransform;
    }

    // Genislige gore olceklenen canvas'in yuksekligi (1080 genislik birimine gore)
    public static float CanvasHeight => RefWidth * Screen.height / Mathf.Max(1f, (float)Screen.width);

    // Ekranin ustundeki centik/durum cubugu payi (canvas biriminde)
    public static float TopInset => (Screen.height - Screen.safeArea.yMax) * RefWidth / Mathf.Max(1f, (float)Screen.width);
    public static float BottomInset => Screen.safeArea.yMin * RefWidth / Mathf.Max(1f, (float)Screen.width);

    // Kaydirilabilir liste alani. Donen 'content' icine satirlari yukaridan asagi diz,
    // sonra content.sizeDelta'nin y'sini toplam yukseklige ayarla.
    public static RectTransform ScrollArea(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject view = new GameObject("ScrollArea", typeof(RectTransform));
        view.transform.SetParent(parent, false);
        RectTransform vr = view.GetComponent<RectTransform>();
        vr.sizeDelta = size;
        vr.anchoredPosition = position;
        Image hit = view.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f); // surukleme icin gorunmez yuzey
        view.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(view.transform, false);
        RectTransform cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 1f);
        cr.anchorMax = new Vector2(0.5f, 1f);
        cr.pivot = new Vector2(0.5f, 1f);
        cr.sizeDelta = new Vector2(size.x, size.y);
        cr.anchoredPosition = Vector2.zero;

        ScrollRect sr = view.AddComponent<ScrollRect>();
        sr.content = cr;
        sr.viewport = vr;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 40f;
        return cr;
    }

    // Pencerenin sag ustune yuvarlak X butonu
    public static Button CloseButton(RectTransform window, GameObject root)
    {
        Vector2 half = window.sizeDelta * 0.5f;
        Button b = TextButton(window, "X", Theme.Danger, new Vector2(half.x - 62f, half.y - 62f), new Vector2(92f, 92f), 50);
        b.onClick.AddListener(() => UnityEngine.Object.Destroy(root));
        return b;
    }

    public static Image Panel(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = NeoUI.RoundedSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    // ---------------- YAZI ----------------
    public static Text Label(Transform parent, string content, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = NeoUI.Font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = Theme.SurfaceRim;
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    // Kutunun icine sigan (best-fit) yazi: buton etiketleri tasmaz, hep ortada durur
    public static Text FitLabel(Transform parent, string content, int maxSize, Color color)
    {
        Text t = Label(parent, content, maxSize, color, TextAnchor.MiddleCenter);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 12;
        t.resizeTextMaxSize = maxSize;
        RectTransform r = t.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(16f, 8f);
        r.offsetMax = new Vector2(-16f, -8f);
        return t;
    }

    // ---------------- BUTON ----------------
    // Block Blast tarzi "kalin" buton: alt kisimda koyu golge seridi, ustte parlak yuzey
    public static Button TextButton(Transform parent, string label, Color color, Vector2 position, Vector2 size, int fontSize)
    {
        Button b = BaseButton(parent, "Button_" + label, color, position, size);
        FitLabel(b.transform, label, fontSize, Theme.Text);
        return b;
    }

    public static Button BaseButton(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = NeoUI.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = Theme.Darken(color, 0.55f);
        shadow.effectDistance = new Vector2(0f, -9f);

        // Ust yarida hafif parlaklik (cam efekti)
        GameObject gloss = new GameObject("Gloss", typeof(RectTransform));
        gloss.transform.SetParent(go.transform, false);
        Image g = gloss.AddComponent<Image>();
        g.sprite = NeoUI.RoundedSprite;
        g.type = Image.Type.Sliced;
        // Koyu butonlarda parlaklik gri leke gibi durmasin diye renge gore ayarlanir
        float lum = color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
        g.color = new Color(1f, 1f, 1f, lum > 0.45f ? 0.14f : 0.06f);
        g.raycastTarget = false;
        RectTransform gr = g.rectTransform;
        gr.anchorMin = new Vector2(0f, 0.55f);
        gr.anchorMax = new Vector2(1f, 1f);
        gr.offsetMin = new Vector2(8f, 0f);
        gr.offsetMax = new Vector2(-8f, -6f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock cb = button.colors;
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        button.colors = cb;
        go.AddComponent<PressBounce>();
        return button;
    }

    // ---------------- KUCUK PARCALAR ----------------
    public static Image Icon(Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Icon", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = size;
        img.rectTransform.anchoredPosition = position;
        return img;
    }

    // Altin para rozeti: [ (o) 1234 ]
    public static Text CoinBadge(Transform parent, Vector2 position, Vector2 size)
    {
        Image bg = Panel(parent, "CoinBadge", size, Theme.WithAlpha(Theme.Surface, 0.9f));
        bg.raycastTarget = false;
        bg.rectTransform.anchoredPosition = position;
        Icon(bg.transform, CoinSprite, new Vector2(-size.x * 0.5f + size.y * 0.5f, 0f), Vector2.one * (size.y - 18f), Color.white);
        Text t = Label(bg.transform, Economy.Coins.ToString(), Mathf.RoundToInt(size.y * 0.5f), Theme.Highlight, TextAnchor.MiddleCenter);
        t.rectTransform.anchoredPosition = new Vector2(size.y * 0.4f, 0f);
        t.rectTransform.sizeDelta = new Vector2(size.x - size.y, size.y);
        CoinLabel live = bg.gameObject.AddComponent<CoinLabel>();
        live.text = t;
        return t;
    }

    // Ilerleme cubugu (0-1)
    public static Image ProgressBar(Transform parent, Vector2 position, Vector2 size, float value, Color fill)
    {
        Image bg = Panel(parent, "Bar", size, Theme.SurfaceRim);
        bg.raycastTarget = false;
        bg.rectTransform.anchoredPosition = position;
        Image f = Panel(bg.transform, "Fill", size, fill);
        f.raycastTarget = false;
        RectTransform fr = f.rectTransform;
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;
        fr.sizeDelta = Vector2.zero;
        f.enabled = value > 0.001f;
        return f;
    }

    public static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public static IEnumerator PopIn(RectTransform rect)
    {
        if (rect == null) yield break;
        Vector3 target = rect.localScale;
        float t = 0f;
        while (t < 0.22f && rect != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.22f) - 1f;
            float back = 1f + p * p * (2.7f * p + 1.7f); // ease-out-back (hafif tasip yerine oturur)
            rect.localScale = target * Mathf.LerpUnclamped(0.7f, 1f, back);
            yield return null;
        }
        if (rect != null) rect.localScale = target;
    }

    // ---------------- BILDIRIM (TOAST) ----------------
    // Ekranin ustunde kisa sure gorunen serit (basarim acildi, odul alindi vb.). Sahne degisse de kalir.
    public static void Toast(string title, string subtitle, Color accent)
    {
        GameObject root = CreateCanvas("Toast", 900, true);
        Image bg = Panel(root.transform, "Toast", new Vector2(900f, subtitle != null ? 190f : 130f), Theme.WithAlpha(Theme.Surface, 0.97f));
        bg.raycastTarget = false;
        RectTransform r = bg.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        Outline o = bg.gameObject.AddComponent<Outline>();
        o.effectColor = accent;
        o.effectDistance = new Vector2(5f, -5f);

        Text t = Label(bg.transform, title, 52, accent, TextAnchor.MiddleCenter);
        t.rectTransform.anchoredPosition = new Vector2(0f, subtitle != null ? 32f : 0f);
        if (subtitle != null)
        {
            Text s = Label(bg.transform, subtitle, 38, Theme.Text, TextAnchor.MiddleCenter);
            s.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        }
        Runner.Instance.StartCoroutine(ToastRoutine(root, r));
    }

    static int activeToasts;

    static IEnumerator ToastRoutine(GameObject root, RectTransform r)
    {
        // Ust uste gelen bildirimler sirayla gosterilsin
        int order = activeToasts++;
        float wait = order * 1.4f;
        r.anchoredPosition = new Vector2(0f, 260f);
        while (wait > 0f) { wait -= Time.unscaledDeltaTime; yield return null; }

        float t = 0f;
        while (t < 0.3f && r != null)
        {
            t += Time.unscaledDeltaTime;
            float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.3f), 3f);
            r.anchoredPosition = new Vector2(0f, Mathf.Lerp(260f, -90f, p));
            yield return null;
        }
        float hold = 1.8f;
        while (hold > 0f) { hold -= Time.unscaledDeltaTime; yield return null; }
        t = 0f;
        while (t < 0.25f && r != null)
        {
            t += Time.unscaledDeltaTime;
            r.anchoredPosition = new Vector2(0f, Mathf.Lerp(-90f, 260f, Mathf.Clamp01(t / 0.25f)));
            yield return null;
        }
        activeToasts = Mathf.Max(0, activeToasts - 1);
        if (root != null) UnityEngine.Object.Destroy(root);
    }

    // ---------------- KODLA CIZILEN SPRITE'LAR ----------------
    static Sprite coinSprite, lockSprite, circleSprite, starSprite, chevronSprite;

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
                circleSprite = MakeSprite(96, (x, y, s) => Disc(x, y, s * 0.5f, s * 0.5f, s * 0.5f - 1f));
            return circleSprite;
        }
    }

    // Altin para: dis halka koyu altin, ic disk parlak altin, ortada parlak cizgi
    public static Sprite CoinSprite
    {
        get
        {
            if (coinSprite == null)
            {
                Color rim = new Color(0.85f, 0.55f, 0.05f, 1f);
                Color face = new Color(1f, 0.82f, 0.22f, 1f);
                Color shine = new Color(1f, 0.95f, 0.65f, 1f);
                coinSprite = MakeColorSprite(96, (x, y, s) =>
                {
                    float c = s * 0.5f;
                    float outer = Disc(x, y, c, c, c - 1f);
                    if (outer <= 0f) return Color.clear;
                    float inner = Disc(x, y, c, c, c * 0.74f);
                    Color col = Color.Lerp(rim, face, inner);
                    // Ortada dikey parlak serit
                    float bar = Mathf.Clamp01(1f - Mathf.Abs(x - c) / (s * 0.07f)) * Disc(x, y, c, c, c * 0.45f);
                    col = Color.Lerp(col, shine, bar * 0.9f);
                    col.a = outer;
                    return col;
                });
            }
            return coinSprite;
        }
    }

    // Kilit: govde (yuvarlak kare) + ust halka
    public static Sprite LockSprite
    {
        get
        {
            if (lockSprite == null)
            {
                lockSprite = MakeSprite(96, (x, y, s) =>
                {
                    float a = 0f;
                    // Govde
                    float bx = Mathf.Abs(x - s * 0.5f), by = y - s * 0.30f;
                    if (bx < s * 0.32f && Mathf.Abs(by) < s * 0.24f) a = 1f;
                    // Halka (ust yari daire)
                    float dx = x - s * 0.5f, dy = y - s * 0.56f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dy >= 0f && d < s * 0.25f && d > s * 0.14f) a = 1f;
                    if (dy < 0f && dy > -s * 0.05f && (Mathf.Abs(dx) > s * 0.14f && Mathf.Abs(dx) < s * 0.25f)) a = 1f;
                    // Anahtar deligi
                    float kx = x - s * 0.5f, ky = y - s * 0.32f;
                    if (kx * kx + ky * ky < (s * 0.06f) * (s * 0.06f)) a = 0f;
                    return a;
                });
            }
            return lockSprite;
        }
    }

    // Bes koseli yildiz
    public static Sprite StarSprite
    {
        get
        {
            if (starSprite == null)
            {
                starSprite = MakeSprite(96, (x, y, s) =>
                {
                    float cx = s * 0.5f, cy = s * 0.47f;
                    float ang = Mathf.Atan2(y - cy, x - cx);
                    float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float k = Mathf.Cos(5f * (ang - Mathf.PI * 0.5f));
                    float edge = Mathf.Lerp(s * 0.22f, s * 0.48f, (k + 1f) * 0.5f);
                    return Mathf.Clamp01(edge - r + 0.5f);
                });
            }
            return starSprite;
        }
    }

    // Sola bakan kalin ok basi ( < )
    public static Sprite ChevronSprite
    {
        get
        {
            if (chevronSprite == null)
            {
                chevronSprite = MakeSprite(96, (x, y, s) =>
                {
                    float cy = s * 0.5f;
                    float dy = Mathf.Abs(y - cy);
                    float lineX = s * 0.30f + dy * 0.9f; // V'nin merkez cizgisi
                    float dist = Mathf.Abs(x - lineX) * 0.74f;
                    if (dy > s * 0.34f) return 0f;
                    return Mathf.Clamp01(s * 0.10f - dist + 0.5f);
                });
            }
            return chevronSprite;
        }
    }

    static float Disc(float x, float y, float cx, float cy, float r)
    {
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        return Mathf.Clamp01(r - d + 0.5f);
    }

    static Sprite MakeSprite(int size, Func<float, float, float, float> alpha)
    {
        return MakeColorSprite(size, (x, y, s) => new Color(1f, 1f, 1f, alpha(x, y, s)));
    }

    static Sprite MakeColorSprite(int size, Func<float, float, float, Color> pixel)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = pixel(x + 0.5f, y + 0.5f, size);
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // Coroutine calistirmak icin gizli, kalici bir obje
    public class Runner : MonoBehaviour
    {
        static Runner instance;
        public static Runner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UIKitRunner");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Runner>();
                }
                return instance;
            }
        }
    }
}

// Basinca hafifce kuculup geri gelen buton hissi
public class PressBounce : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
{
    Vector3 baseScale = Vector3.one;
    bool captured;

    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
    {
        if (!captured) { baseScale = transform.localScale; captured = true; }
        transform.localScale = baseScale * 0.94f;
    }

    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
    {
        transform.localScale = captured ? baseScale : Vector3.one;
    }
}

// Para miktari degisince yaziyi gunceller
public class CoinLabel : MonoBehaviour
{
    public Text text;
    int shown = -1;

    void Update()
    {
        if (text == null) return;
        int c = Economy.Coins;
        if (c == shown) return;
        shown = c;
        text.text = c.ToString();
    }
}
