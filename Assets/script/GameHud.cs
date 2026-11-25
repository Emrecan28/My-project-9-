using UnityEngine;
using UnityEngine.UI;
using TMPro;

// OYUN EKRANI UST BAR: [para]   BEST 3420 / 326   [pause]
// Sahnedeki eski skor yazilari bu bara tasinir (ScoreManager onlari guncellemeye devam eder);
// eski sol ust yenile/geri al butonlari gizlenir: o ikisi artik alttaki guclendirme barinda.
public class GameHud : MonoBehaviour
{
    GridManager grid;

    public static GameHud Create(GridManager grid)
    {
        GameObject root = UIKit.CreateCanvas("HudCanvas", 15);
        GameHud hud = root.AddComponent<GameHud>();
        hud.grid = grid;
        hud.Build();
        return hud;
    }

    void Build()
    {
        float top = UIKit.TopInset;

        RectTransform bar = new GameObject("TopBar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(transform, false);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, 300f);
        bar.anchoredPosition = new Vector2(0f, -top);

        // Sol: para
        Text coins = UIKit.CoinBadge(bar, Vector2.zero, new Vector2(240f, 84f));
        RectTransform cr = coins.transform.parent as RectTransform;
        cr.anchorMin = cr.anchorMax = new Vector2(0f, 1f);
        cr.anchoredPosition = new Vector2(40f + 120f, -40f - 42f);

        // Sag: duraklat (iki beyaz cubuk)
        Button pause = UIKit.BaseButton(bar, "Pause", Theme.SurfaceSoft, Vector2.zero, new Vector2(110f, 110f));
        RectTransform pr = pause.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(1f, 1f);
        pr.anchoredPosition = new Vector2(-40f - 55f, -40f - 42f);
        PauseBar(pause.transform, -14f);
        PauseBar(pause.transform, 14f);
        pause.onClick.AddListener(() =>
        {
            PauseManager pm = FindFirstObjectByType<PauseManager>();
            if (pm != null && !PauseManager.IsPauseMenuOpen) pm.OpenPauseMenu();
        });

        // Orta: rekor + skor. Hemen tasinir (ilk karede eski yerde gorunmesin);
        // ScoreManager kendi Start'inda boyutu degistirebildigi icin bir kare sonra tekrar uygulanir.
        PlaceScoreTexts(bar);
        StartCoroutine(MoveScoreTexts(bar));

        // Sahnedeki eski butonlari gizle
        if (grid != null)
        {
            if (grid.refreshButton != null) grid.refreshButton.gameObject.SetActive(false);
            if (grid.undoButton != null) grid.undoButton.gameObject.SetActive(false);
        }
        GameObject oldPause = GameObject.Find("BackMenu");
        if (oldPause != null) oldPause.SetActive(false);
    }

    System.Collections.IEnumerator MoveScoreTexts(RectTransform bar)
    {
        yield return null;
        PlaceScoreTexts(bar);
    }

    void PlaceScoreTexts(RectTransform bar)
    {
        ScoreManager sm = ScoreManager.Instance != null ? ScoreManager.Instance : FindFirstObjectByType<ScoreManager>();
        if (sm == null) return;
        MoveText(sm.highScoreText, bar, new Vector2(0f, -60f), new Vector2(560f, 60f), 42f);
        MoveText(sm.currentScoreText, bar, new Vector2(0f, -170f), new Vector2(900f, 150f), 120f);
    }

    // Duz beyaz dikdortgen (yuvarlak sprite kucuk boyda yumurta gibi gorunuyordu)
    static void PauseBar(Transform parent, float x)
    {
        GameObject go = new GameObject("PauseBar", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(16f, 50f);
        img.rectTransform.anchoredPosition = new Vector2(x, 0f);
    }

    static void MoveText(TextMeshProUGUI t, RectTransform parent, Vector2 pos, Vector2 size, float fontSize)
    {
        if (t == null) return;
        RectTransform r = t.rectTransform;
        r.SetParent(parent, false);
        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        t.enableAutoSizing = false;
        t.fontSize = fontSize;
        t.horizontalAlignment = HorizontalAlignmentOptions.Center;
        t.verticalAlignment = VerticalAlignmentOptions.Middle;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }
}
