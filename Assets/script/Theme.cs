using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// NEOBLOCK RENK SISTEMI
// Oyundaki TUM renkler buradan gelir. Bir rengi degistirmek icin sadece bu dosyayi duzenle.
//
// Kural (renk uyumu):
//  - Zemin: mavi gradyan  -> Yuzeyler: lacivert tonlari (tahta, paneller, HUD butonlari)
//  - Tek bir sicak ana vurgu: TURUNCU (ana aksiyon: Classic, Restart, siradaki level, geri sayim)
//  - Olumlu/ilerle: YESIL (Levels, Resume, Continue, tamamlanan level, All Clear)
//  - Notr ikincil butonlar: ACIK MAVI (Menu, Levels(pause), kilitli level)
//  - Basliklar/rekor/ovgu: ALTIN; yazilar: BEYAZ
//  - Neon pembe/camgobegi/mor gibi bagimsiz renkler artik kullanilmiyor.
public static class Theme
{
    // Zemin gradyani
    public static readonly Color BgTop = new Color(0.30f, 0.42f, 0.92f, 1f);
    public static readonly Color BgMid = new Color(0.24f, 0.33f, 0.80f, 1f);
    public static readonly Color BgBottom = new Color(0.15f, 0.20f, 0.55f, 1f);

    // Yuzeyler (lacivert aile)
    public static readonly Color Surface = new Color(0.10f, 0.13f, 0.33f, 1f);       // tahta, paneller
    public static readonly Color SurfaceRim = new Color(0.06f, 0.08f, 0.22f, 1f);    // tahta cercevesi, yazi kontur
    public static readonly Color SurfaceSoft = new Color(0.16f, 0.20f, 0.45f, 1f);   // bos hucre
    public static readonly Color HudButton = new Color(0.10f, 0.13f, 0.33f, 0.88f);  // ekrandaki yuvarlak ikon butonlari
    public static readonly Color Dim = new Color(0.03f, 0.05f, 0.16f, 0.72f);        // pencere arkasi karartma

    // Vurgular
    public static readonly Color Primary = new Color(1.00f, 0.60f, 0.12f, 1f);   // turuncu
    public static readonly Color Positive = new Color(0.30f, 0.78f, 0.30f, 1f);  // yesil
    public static readonly Color Neutral = new Color(0.28f, 0.40f, 0.88f, 1f);   // acik mavi
    public static readonly Color Highlight = new Color(1.00f, 0.83f, 0.25f, 1f); // altin
    public static readonly Color Danger = new Color(0.95f, 0.30f, 0.32f, 1f);    // kirmizi (sadece kapali/uyari)

    // Yazi
    public static readonly Color Text = Color.white;
    public static readonly Color TextMuted = new Color(0.80f, 0.86f, 1f, 0.75f);

    public static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    // ---------------- SAHNE RENKLENDIRME ----------------
    // Sahnelerde elle ayarlanmis eski renkleri (gri butonlar, mor paneller) oyun acilirken temaya ceker.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToScene(scene);

    static void ApplyToScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Oyun sahnesi: ust kosedeki yuvarlak ikon butonlari (pause, yenile, geri al)
            Tint(root.transform, "Canvas/Panel/BackMenu", HudButton);
            Tint(root.transform, "Canvas/Panel/+", HudButton);
            Tint(root.transform, "Canvas/Panel/<-", HudButton);

            // Ana menu: alt ikon butonlari ve bilgi panelleri
            Tint(root.transform, "gameplay", HudButton);
            Tint(root.transform, "GameMusic", HudButton);
            Tint(root.transform, "GameMusic/Image", Danger);
            Tint(root.transform, "gameplayp", Surface);
            Tint(root.transform, "gameplayp/Scroll View (1)", Surface);
            Tint(root.transform, "HelpPanel", Surface);
            Tint(root.transform, "HelpPanel/Scroll View", Surface);
            Tint(root.transform, "SettingPanel", Surface);
        }
    }

    static void Tint(Transform root, string path, Color color)
    {
        // Kok objenin kendisi ya da altindaki yol (aktif olmayan objeler de bulunur)
        Transform t = root.Find(path);
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
