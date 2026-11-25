using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// LEVEL SECIM EKRANI: tamamen kodla kurulur (eski prefab/scroll yerine).
// Ust bar (geri, baslik, para) + ilerleme + 4 sutunlu esit kare level kutulari.
public class LevelSelectUI : MonoBehaviour
{
    [Header("Eski sahne referanslari (artik kullanilmiyor, sahnede kalabilir)")]
    public GameObject buttonPrefab;
    public Transform contentPanel;
    public Button backButton;

    const int Columns = 4;
    const float Cell = 200f;
    const float Gap = 36f;

    GameObject root;
    RectTransform currentTile;

    void Start()
    {
        if (LevelManager.Instance == null)
        {
            var lmObj = new GameObject("LevelManager");
            lmObj.AddComponent<LevelManager>();
        }

        HideOldScreen();
        RefreshUI();
    }

    void HideOldScreen()
    {
        // Bu sahnenin kok canvas'i (baska kalici canvas'lar karismasin)
        Canvas canvas = null;
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c != null && c.isRootCanvas && c.gameObject.scene == gameObject.scene && c.transform.Find("Panel") != null) { canvas = c; break; }
        }
        if (canvas == null)
        {
            Canvas own = GetComponentInParent<Canvas>(true);
            if (own != null) canvas = own.rootCanvas;
        }
        if (canvas == null) return;
        Transform bg = canvas.transform.Find("Image");
        if (bg != null) NeoUI.ApplyGradientBackground(bg.GetComponent<Image>());
        Transform panel = canvas.transform.Find("Panel");
        HideKeepingThis(panel);
        Transform back = canvas.transform.Find("backmenubutton");
        HideKeepingThis(back);
    }

    // Eski objeyi gizle; ama bu script onun uzerindeyse objeyi kapatma (yoksa script de durur),
    // sadece gorselini ve cocuklarini kapat
    void HideKeepingThis(Transform t)
    {
        if (t == null) return;
        if (t != transform && !transform.IsChildOf(t))
        {
            t.gameObject.SetActive(false);
            return;
        }
        Graphic g = t.GetComponent<Graphic>();
        if (g != null) g.enabled = false;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i);
            if (c != transform && !transform.IsChildOf(c)) c.gameObject.SetActive(false);
        }
    }

    void GoBack()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.LoadMenu();
        else SceneManager.LoadScene("MainMenu");
    }

    // Siradaki level kutusu hafifce nabiz atar
    void Update()
    {
        if (currentTile == null) return;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f;
        currentTile.localScale = new Vector3(pulse, pulse, 1f);
    }

    public void RefreshUI()
    {
        if (this == null || LevelManager.Instance == null) return;
        if (root != null) Destroy(root);
        currentTile = null;

        root = UIKit.CreateCanvas("LevelSelectCanvas", 10);
        float top = UIKit.TopInset;
        float h = UIKit.CanvasHeight;

        // ---- Ust bar ----
        RectTransform bar = new GameObject("TopBar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(root.transform, false);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, 150f);
        bar.anchoredPosition = new Vector2(0f, -top - 20f);

        Button back = UIKit.BaseButton(bar, "Back", Theme.SurfaceSoft, Vector2.zero, new Vector2(120f, 120f));
        RectTransform br = back.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0f, 0.5f);
        br.anchoredPosition = new Vector2(100f, 0f);
        UIKit.Icon(back.transform, UIKit.ChevronSprite, new Vector2(-4f, 0f), new Vector2(70f, 70f), Theme.Text);
        back.onClick.AddListener(GoBack);

        Text title = UIKit.Label(bar, "LEVELS", 84, Theme.Text, TextAnchor.MiddleCenter);
        title.rectTransform.anchoredPosition = Vector2.zero;

        Text coins = UIKit.CoinBadge(bar, Vector2.zero, new Vector2(230f, 80f));
        RectTransform cr = coins.transform.parent as RectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
        cr.anchoredPosition = new Vector2(-150f, 0f);

        // ---- Ilerleme ----
        int unlocked = LevelManager.Instance.GetUnlockedLevel();
        int total = LevelManager.Instance.levels.Count;
        int done = Mathf.Clamp(unlocked - 1, 0, total);

        RectTransform info = new GameObject("Progress", typeof(RectTransform)).GetComponent<RectTransform>();
        info.SetParent(root.transform, false);
        info.anchorMin = info.anchorMax = new Vector2(0.5f, 1f);
        info.sizeDelta = new Vector2(900f, 90f);
        info.anchoredPosition = new Vector2(0f, -top - 230f);
        UIKit.ProgressBar(info, new Vector2(0f, -18f), new Vector2(860f, 34f), done / (float)Mathf.Max(1, total), Theme.Positive);
        Text pt = UIKit.Label(info, done + " / " + total + " COMPLETED", 40, Theme.TextMuted, TextAnchor.MiddleCenter);
        pt.rectTransform.anchoredPosition = new Vector2(0f, 30f);

        // ---- Level kutulari (kaydirilabilir) ----
        float gridTop = top + 300f;
        float viewH = h - gridTop - UIKit.BottomInset - 30f;
        float gridW = Columns * Cell + (Columns - 1) * Gap;
        RectTransform content = UIKit.ScrollArea(root.transform, Vector2.zero, new Vector2(gridW + 60f, viewH));
        RectTransform view = content.parent as RectTransform;
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 1f);
        view.pivot = new Vector2(0.5f, 1f);
        view.anchoredPosition = new Vector2(0f, -gridTop);

        int rows = Mathf.CeilToInt(total / (float)Columns);
        float contentH = rows * Cell + (rows - 1) * Gap + 60f;
        content.sizeDelta = new Vector2(gridW + 60f, contentH);

        float currentY = 0f;
        foreach (var level in LevelManager.Instance.levels)
        {
            int i = level.levelNumber - 1;
            int col = i % Columns, row = i / Columns;
            Vector2 pos = new Vector2(-gridW * 0.5f + Cell * 0.5f + col * (Cell + Gap), -30f - Cell * 0.5f - row * (Cell + Gap));
            RectTransform tile = CreateTile(content, level.levelNumber, unlocked, pos);
            if (level.levelNumber == unlocked) { currentTile = tile; currentY = -pos.y; }
        }

        // Siradaki leveli ekranin ortasina kaydir
        float maxScroll = Mathf.Max(0f, contentH - viewH);
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(currentY - viewH * 0.5f, 0f, maxScroll));
    }

    RectTransform CreateTile(RectTransform parent, int number, int unlocked, Vector2 pos)
    {
        bool completed = number < unlocked;
        bool current = number == unlocked;
        bool locked = number > unlocked;

        Color color = completed ? Theme.Positive : (current ? Theme.Primary : Theme.SurfaceSoft);
        Button b = UIKit.BaseButton(parent, "Level_" + number, color, pos, new Vector2(Cell, Cell));
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;

        if (current)
        {
            // Beyaz cerceve: kutunun arkasinda biraz daha buyuk beyaz yuvarlak kare
            Image rim = UIKit.Panel(b.transform, "Rim", new Vector2(Cell + 16f, Cell + 16f), Color.white);
            rim.raycastTarget = false;
            rim.transform.SetAsFirstSibling();
            // Rim, butonun kendi gorselinin arkasinda kalsin diye ayri bir katman: butonun ustune degil arkaya
            rim.rectTransform.SetParent(parent, false);
            rim.rectTransform.anchorMin = rim.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rim.rectTransform.anchoredPosition = pos;
            rim.transform.SetSiblingIndex(b.transform.GetSiblingIndex());
            rim.gameObject.AddComponent<FollowScale>().target = rt;
        }

        if (locked)
        {
            UIKit.Icon(b.transform, UIKit.LockSprite, new Vector2(0f, 8f), new Vector2(84f, 84f), Theme.WithAlpha(Theme.TextMuted, 0.8f));
            Text n = UIKit.Label(b.transform, number.ToString(), 30, Theme.TextMuted, TextAnchor.MiddleCenter);
            n.rectTransform.anchoredPosition = new Vector2(0f, -66f);
            b.interactable = false;
            ColorBlock cb = b.colors;
            cb.disabledColor = Color.white; // kilitli kutu soluk gri olmasin, kendi rengiyle dursun
            b.colors = cb;
        }
        else
        {
            Text n = UIKit.Label(b.transform, number.ToString(), 84, Theme.Text, TextAnchor.MiddleCenter);
            n.rectTransform.anchoredPosition = new Vector2(0f, completed ? 14f : 0f);
            if (completed)
            {
                UIKit.Icon(b.transform, UIKit.StarSprite, new Vector2(0f, -60f), new Vector2(54f, 54f), Theme.Highlight);
            }
            int lvl = number;
            b.onClick.AddListener(() =>
            {
                Haptics.Light();
                LevelManager.Instance.LoadLevel(lvl);
            });
        }
        return rt;
    }
}

// Siradaki levelin beyaz cercevesi kutuyla birlikte nabiz atsin
public class FollowScale : MonoBehaviour
{
    public Transform target;
    void LateUpdate()
    {
        if (target != null) transform.localScale = target.localScale;
    }
}
