using UnityEngine;

// Block Blast tarzi gorunum: kodla uretilen parlak (bevelli) blok, gomulu bos hucre,
// yuvarlak tahta paneli ve parcacik sprite'lari. Tum sprite'lar beyaz uretilir,
// renk SpriteRenderer.color ile verilir.
public static class BlockSkin
{
    // Magazadaki blok temalari. 0 = CLASSIC (ucretsiz), digerleri parayla acilir.
    public static readonly string[] ThemeNames = { "CLASSIC", "CANDY", "NEON", "OCEAN", "SUNSET", "JEWEL",
                                                   "FOREST", "LAVA", "ICE", "BERRY", "RETRO", "GOLDEN" };

    public static readonly Color[][] Themes =
    {
        new Color[] { // CLASSIC: Block Blast'taki gibi doygun, parlak
            new Color(0.96f, 0.28f, 0.30f), new Color(1.00f, 0.58f, 0.12f), new Color(1.00f, 0.80f, 0.12f),
            new Color(0.32f, 0.80f, 0.28f), new Color(0.15f, 0.74f, 0.95f), new Color(0.22f, 0.42f, 0.95f),
            new Color(0.62f, 0.32f, 0.95f) },
        new Color[] { // CANDY: pastel
            new Color(1.00f, 0.55f, 0.66f), new Color(1.00f, 0.72f, 0.50f), new Color(1.00f, 0.90f, 0.52f),
            new Color(0.55f, 0.90f, 0.66f), new Color(0.52f, 0.86f, 0.96f), new Color(0.66f, 0.70f, 1.00f),
            new Color(0.86f, 0.62f, 1.00f) },
        new Color[] { // NEON: elektrik renkler
            new Color(1.00f, 0.16f, 0.62f), new Color(1.00f, 0.42f, 0.05f), new Color(0.92f, 1.00f, 0.10f),
            new Color(0.25f, 1.00f, 0.35f), new Color(0.05f, 0.95f, 1.00f), new Color(0.35f, 0.45f, 1.00f),
            new Color(0.80f, 0.20f, 1.00f) },
        new Color[] { // OCEAN: mavi-yesil tonlar
            new Color(0.05f, 0.62f, 0.72f), new Color(0.20f, 0.80f, 0.78f), new Color(0.45f, 0.90f, 0.85f),
            new Color(0.18f, 0.52f, 0.95f), new Color(0.40f, 0.72f, 1.00f), new Color(0.10f, 0.36f, 0.78f),
            new Color(0.62f, 0.86f, 0.98f) },
        new Color[] { // SUNSET: sicak tonlar
            new Color(0.88f, 0.16f, 0.28f), new Color(1.00f, 0.40f, 0.36f), new Color(1.00f, 0.60f, 0.20f),
            new Color(1.00f, 0.80f, 0.28f), new Color(0.98f, 0.42f, 0.62f), new Color(0.76f, 0.26f, 0.62f),
            new Color(0.52f, 0.22f, 0.70f) },
        new Color[] { // JEWEL: koyu mucevher tonlari
            new Color(0.82f, 0.08f, 0.22f), new Color(0.05f, 0.70f, 0.42f), new Color(0.10f, 0.32f, 0.88f),
            new Color(0.55f, 0.18f, 0.85f), new Color(1.00f, 0.70f, 0.10f), new Color(0.62f, 0.10f, 0.32f),
            new Color(0.25f, 0.85f, 0.85f) },
        new Color[] { // FOREST: yesil ve toprak tonlari
            new Color(0.20f, 0.62f, 0.28f), new Color(0.45f, 0.78f, 0.30f), new Color(0.72f, 0.86f, 0.36f),
            new Color(0.55f, 0.40f, 0.22f), new Color(0.85f, 0.65f, 0.30f), new Color(0.12f, 0.48f, 0.42f),
            new Color(0.95f, 0.80f, 0.45f) },
        new Color[] { // LAVA: ates kirmizi-turuncu
            new Color(0.80f, 0.10f, 0.08f), new Color(0.95f, 0.30f, 0.08f), new Color(1.00f, 0.52f, 0.10f),
            new Color(1.00f, 0.74f, 0.20f), new Color(0.60f, 0.08f, 0.12f), new Color(1.00f, 0.42f, 0.30f),
            new Color(0.90f, 0.20f, 0.35f) },
        new Color[] { // ICE: buz mavisi ve beyaz
            new Color(0.70f, 0.90f, 1.00f), new Color(0.45f, 0.78f, 0.98f), new Color(0.25f, 0.60f, 0.95f),
            new Color(0.85f, 0.95f, 1.00f), new Color(0.55f, 0.85f, 0.90f), new Color(0.60f, 0.70f, 0.98f),
            new Color(0.35f, 0.88f, 0.95f) },
        new Color[] { // BERRY: pembe-mor meyve tonlari
            new Color(0.85f, 0.15f, 0.45f), new Color(0.65f, 0.15f, 0.55f), new Color(0.98f, 0.45f, 0.65f),
            new Color(0.45f, 0.20f, 0.65f), new Color(0.95f, 0.30f, 0.30f), new Color(0.78f, 0.50f, 0.95f),
            new Color(1.00f, 0.65f, 0.80f) },
        new Color[] { // RETRO: 80'ler oyun renkleri
            new Color(1.00f, 0.35f, 0.55f), new Color(0.20f, 0.85f, 0.95f), new Color(1.00f, 0.90f, 0.25f),
            new Color(0.55f, 0.95f, 0.40f), new Color(0.65f, 0.40f, 1.00f), new Color(1.00f, 0.55f, 0.20f),
            new Color(0.30f, 0.50f, 1.00f) },
        new Color[] { // GOLDEN: altin, bronz, gumus
            new Color(1.00f, 0.80f, 0.25f), new Color(0.90f, 0.62f, 0.18f), new Color(0.75f, 0.50f, 0.22f),
            new Color(0.85f, 0.85f, 0.90f), new Color(1.00f, 0.90f, 0.55f), new Color(0.65f, 0.68f, 0.75f),
            new Color(0.95f, 0.70f, 0.40f) },
    };

    // Secili temanin renkleri
    public static Color[] Palette => Themes[Economy.SelectedTheme];

    public static Color BoardColor => Theme.Surface;      // tahta zemini (lacivert)
    public static Color EmptyCellColor => Theme.SurfaceSoft; // bos hucre
    public static readonly Color FrozenColor = new Color(0.62f, 0.66f, 0.74f, 1f);  // game over gumus

    const int Tex = 128;
    static Sprite emptyCell, panel, particle;
    static float blockWorldSize = 1f;
    static float ppu = Tex;
    static readonly System.Collections.Generic.Dictionary<Color32, Sprite> blockCache =
        new System.Collections.Generic.Dictionary<Color32, Sprite>();

    // worldSize: sprite'in olcek 1'deyken kac dunya birimi olacagi
    public static void Init(float worldSize)
    {
        if (Mathf.Approximately(worldSize, blockWorldSize) && emptyCell != null) return;
        blockWorldSize = worldSize;
        ppu = Tex / worldSize;
        blockCache.Clear();
        emptyCell = Sprite.Create(BuildEmptyTexture(), new Rect(0, 0, Tex, Tex), new Vector2(0.5f, 0.5f), ppu);
    }

    public static Sprite EmptyCell => emptyCell;

    // Renge ozel parlak blok sprite'i (her renk bir kez uretilir ve saklanir)
    public static Sprite GetBlockSprite(Color color)
    {
        Color32 key = color;
        key.a = 255;
        if (blockCache.TryGetValue(key, out Sprite s) && s != null) return s;
        Color c = key;
        s = Sprite.Create(BuildBlockTexture(c), new Rect(0, 0, Tex, Tex), new Vector2(0.5f, 0.5f), ppu);
        blockCache[key] = s;
        return s;
    }

    // Tahta zemini: daha kucuk kose yaricapi, keskin kenar (128px, kenar payi 28)
    private static Sprite boardPanel;
    public static Sprite BoardPanel
    {
        get
        {
            if (boardPanel == null)
            {
                Texture2D t = BuildRounded(128, 22, 0f);
                boardPanel = Sprite.Create(t, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f, 0,
                    SpriteMeshType.FullRect, new Vector4(28, 28, 28, 28));
            }
            return boardPanel;
        }
    }

    public static Sprite Panel
    {
        get
        {
            if (panel == null)
            {
                Texture2D t = BuildRounded(64, 18, 0f);
                panel = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f, 0,
                    SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
            }
            return panel;
        }
    }

    public static Sprite Particle
    {
        get
        {
            if (particle == null)
            {
                Texture2D t = BuildRounded(32, 8, 0f);
                particle = Sprite.Create(t, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f / 0.35f);
            }
            return particle;
        }
    }

    public static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    // --- Doku uretimi ---

    static float RoundedAlpha(int x, int y, int size, float radius, float inset)
    {
        float min = inset + radius, max = size - inset - radius;
        float cx = Mathf.Clamp(x + 0.5f, min, max);
        float cy = Mathf.Clamp(y + 0.5f, min, max);
        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
        return Mathf.Clamp01(radius - d + 0.5f);
    }

    static Texture2D NewTex(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        return t;
    }

    static Texture2D BuildRounded(int size, float radius, float inset)
    {
        Texture2D t = NewTex(size);
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = new Color32(255, 255, 255, (byte)(RoundedAlpha(x, y, size, radius, inset) * 255));
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // Parlak blok: ust-sol acik kenar, alt-sag koyu kenar, yukarida parlak yansima
    static Texture2D BuildBlockTexture(Color c)
    {
        // Keskin "faset" blok: dort kenarda duz renkli egimler (ust acik, sol hafif acik,
        // sag koyu, alt en koyu), koselerde capraz birlesim, ince koyu dis cizgi ve duz on yuz.
        int s = Tex;
        float radius = s * 0.09f;
        float bevel = s * 0.14f;
        float outline = 2.5f;
        Color top = Color.Lerp(c, Color.white, 0.45f);
        Color left = Color.Lerp(c, Color.white, 0.20f);
        Color right = new Color(c.r * 0.74f, c.g * 0.74f, c.b * 0.74f, 1f);
        Color bottom = new Color(c.r * 0.56f, c.g * 0.56f, c.b * 0.56f, 1f);
        Color edgeLine = new Color(c.r * 0.38f, c.g * 0.38f, c.b * 0.38f, 1f);
        Texture2D t = NewTex(s);
        Color[] px = new Color[s * s];

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float a = RoundedAlpha(x, y, s, radius, 0f);
                if (a <= 0f) { px[y * s + x] = new Color(0, 0, 0, 0); continue; }

                float fx = x + 0.5f, fy = y + 0.5f;
                float dl = fx, dr = s - fx, db = fy, dt = s - fy;
                float edge = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));

                Color col;
                if (edge < bevel)
                {
                    // Hangi kenara en yakinsa o fasetin rengi (koselerde capraz cizgiyle ayrilir)
                    if (dt <= dl && dt <= dr && dt <= db) col = top;
                    else if (db <= dl && db <= dr) col = bottom;
                    else if (dl <= dr) col = left;
                    else col = right;
                    // Faset ile yuz arasinda 1 piksellik yumusak gecis (keskin ama tirtikli degil)
                    float blend = Mathf.Clamp01(bevel - edge);
                    float v0 = fy / s;
                    Color face0 = Color.Lerp(c, Color.Lerp(c, Color.white, 0.08f), v0);
                    col = Color.Lerp(face0, col, blend);
                }
                else
                {
                    // On yuz: hafif dikey ton farki (ust biraz acik)
                    float v = fy / s;
                    col = Color.Lerp(c, Color.Lerp(c, Color.white, 0.08f), v);
                    // Sol ustte kucuk parlak yansima
                    float gx = (fx - s * 0.30f) / (s * 0.10f);
                    float gy = (fy - s * 0.72f) / (s * 0.06f);
                    float gloss = Mathf.Clamp01(1.2f - (gx * gx + gy * gy));
                    col = Color.Lerp(col, Color.white, gloss * 0.45f);
                }

                // Ince koyu dis cizgi: blogu zeminden net ayirir
                if (edge < outline) col = Color.Lerp(edgeLine, col, Mathf.Clamp01(edge - (outline - 1f)));

                col.a = a;
                px[y * s + x] = col;
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // Bos hucre: hafif gomulu (ustte koyu golge, altta ince acik cizgi)
    static Texture2D BuildEmptyTexture()
    {
        // Bos hucre: duz, keskin koseli yuva; ustte ince ic golge, altta ince acik cizgi
        int s = Tex;
        float radius = s * 0.09f;
        Texture2D t = NewTex(s);
        Color[] px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float a = RoundedAlpha(x, y, s, radius, 3f);
                if (a <= 0f) { px[y * s + x] = new Color(1, 1, 1, 0); continue; }
                float fy = y + 0.5f;
                float c = 1f;
                if (fy > s - 3f - s * 0.06f) c = 0.72f;      // ust ic golge (cukur hissi)
                else if (fy < 3f + s * 0.035f) c = 1f;       // alt kenar
                else c = 0.92f;
                px[y * s + x] = new Color(c, c, c, a);
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // Arka plan: Block Blast'taki gibi kraliyet mavisi dikey gradyan, ortada hafif aydinlik
    public static Texture2D BuildBackgroundTexture()
    {
        int w = 64, h = 256;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        Color top = Theme.BgTop;
        Color mid = Theme.BgMid;
        Color bottom = Theme.BgBottom;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);
            Color c = v > 0.5f ? Color.Lerp(mid, top, (v - 0.5f) * 2f) : Color.Lerp(bottom, mid, v * 2f);
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float vignette = 1f - Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 2f) * 0.18f;
                px[y * w + x] = new Color(c.r * vignette, c.g * vignette, c.b * vignette, 1f);
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
