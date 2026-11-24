using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Oyun ekraninin altindaki tek guclendirme cubugu: [GERI AL] [YENI PARCA] [BOMBA] [DONDUR]
// Geri al ve yeni parca: ucretsiz hak, bitince reklamla. Bomba ve dondur: parayla (gercek para yok).
// Para miktari ust barda (GameHud).
public class PowerUpBar : MonoBehaviour
{
    GridManager grid;
    RectTransform bar;
    Button bombButton, rotateButton, undoButton, refreshButton;
    Text undoSub, refreshSub;
    Image bombImage;
    Shadow bombShadow;
    Text bombTitle, bombCost, rotateTitle;
    Image rotateImage;
    Shadow rotateShadow;

    public static PowerUpBar Create(GridManager grid)
    {
        GameObject root = UIKit.CreateCanvas("PowerUpCanvas", 20);
        PowerUpBar bar = root.AddComponent<PowerUpBar>();
        bar.grid = grid;
        bar.Build();
        return bar;
    }

    void Build()
    {
        bar = new GameObject("Bar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(transform, false);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(980f, 120f);
        bar.anchoredPosition = new Vector2(0f, UIKit.BottomInset + 110f);

        undoButton = AdButton("UNDO", Theme.Neutral, new Vector2(-369f, 0f), out undoSub);
        undoButton.onClick.AddListener(() => { if (grid != null) grid.OnUndoButtonClicked(); });

        refreshButton = AdButton("REFRESH", Theme.Highlight, new Vector2(-123f, 0f), out refreshSub);
        refreshButton.onClick.AddListener(() => { if (grid != null) grid.OnRefreshButtonClicked(); });

        bombButton = PowerButton("BOMB", Economy.BombPrice, Theme.Primary, new Vector2(123f, 0f), out bombTitle, out bombCost);
        bombImage = bombButton.GetComponent<Image>();
        bombShadow = bombButton.GetComponent<Shadow>();
        bombButton.onClick.AddListener(() => { if (grid != null) grid.ToggleBomb(); });

        rotateButton = PowerButton("ROTATE", Economy.RotatePrice, Theme.Positive, new Vector2(369f, 0f), out rotateTitle, out _);
        rotateButton.onClick.AddListener(() => { if (grid != null) grid.ToggleRotate(); });
        rotateImage = rotateButton.GetComponent<Image>();
        rotateShadow = rotateButton.GetComponent<Shadow>();

        if (grid != null) grid.OnPowerUpStateChanged += RefreshBomb;
        StartCoroutine(PlaceUnderPieces());
    }

    void OnDestroy()
    {
        if (grid != null) grid.OnPowerUpStateChanged -= RefreshBomb;
    }

    const float ButtonW = 228f, ButtonH = 112f;

    Text Title(Button b, string title)
    {
        Text t = UIKit.Label(b.transform, title, 36, Theme.Text, TextAnchor.MiddleCenter);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 18;
        t.resizeTextMaxSize = 36;
        t.rectTransform.sizeDelta = new Vector2(ButtonW - 24f, 46f);
        t.rectTransform.anchoredPosition = new Vector2(0f, 18f);
        return t;
    }

    // Ucretsiz hakki olan, bitince reklamla acilan buton (alt satirda "FREE x1" / "WATCH AD")
    Button AdButton(string title, Color color, Vector2 pos, out Text sub)
    {
        Button b = UIKit.BaseButton(bar, "Power_" + title, color, pos, new Vector2(ButtonW, ButtonH));
        Title(b, title);
        sub = UIKit.Label(b.transform, "", 26, Theme.Text, TextAnchor.MiddleCenter);
        sub.rectTransform.sizeDelta = new Vector2(ButtonW - 20f, 36f);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -28f);
        return b;
    }

    Button PowerButton(string title, int price, Color color, Vector2 pos, out Text titleText, out Text costText)
    {
        Button b = UIKit.BaseButton(bar, "Power_" + title, color, pos, new Vector2(ButtonW, ButtonH));
        titleText = Title(b, title);

        UIKit.Icon(b.transform, UIKit.CoinSprite, new Vector2(-34f, -28f), new Vector2(34f, 34f), Color.white);
        costText = UIKit.Label(b.transform, price.ToString(), 30, Theme.Text, TextAnchor.MiddleLeft);
        costText.rectTransform.sizeDelta = new Vector2(100f, 40f);
        costText.rectTransform.anchoredPosition = new Vector2(34f, -28f);
        return b;
    }

    // Kamera ayarlandiktan sonra: parcalarin alt kenari ile ekranin alti arasindaki boslugun ortasina otur
    IEnumerator PlaceUnderPieces()
    {
        yield return null;
        yield return null;
        Camera cam = Camera.main;
        if (cam == null || grid == null) yield break;

        float gridHalf = (grid.columns * grid.cellSize + (grid.columns - 1) * grid.spacing) * 0.5f;
        Vector3 slotCenter = grid.transform.TransformPoint(new Vector3(0f, -gridHalf - grid.bottomOffset, 0f));
        // En uzun parca (3 hucre) slotta yaklasik bu kadar asagi iner
        Vector3 piecesBottom = slotCenter - new Vector3(0f, 2.4f, 0f);
        float screenY = cam.WorldToScreenPoint(piecesBottom).y;
        float unitsPerPixel = UIKit.RefWidth / Mathf.Max(1f, (float)Screen.width);
        float piecesBottomCanvas = screenY * unitsPerPixel;
        float safeBottom = UIKit.BottomInset;

        float space = piecesBottomCanvas - safeBottom;
        if (space >= 140f)
        {
            bar.anchoredPosition = new Vector2(0f, safeBottom + space * 0.5f);
            float s = Mathf.Clamp(space / 170f, 0.75f, 1f);
            bar.localScale = Vector3.one * s;
        }
        else
        {
            // Yer yoksa en alta kucuk olarak
            bar.anchoredPosition = new Vector2(0f, safeBottom + 60f);
            bar.localScale = Vector3.one * 0.75f;
        }
    }

    void RefreshBomb()
    {
        if (bombButton == null || grid == null) return;
        bool armed = grid.IsBombArmed;
        Color c = armed ? Theme.Danger : Theme.Primary;
        bombImage.color = c;
        if (bombShadow != null) bombShadow.effectColor = Theme.Darken(c, 0.55f);
        bombTitle.text = armed ? "CANCEL" : "BOMB";
        bombCost.text = Economy.BombPrice.ToString();

        bool rArmed = grid.IsRotateArmed;
        Color rc = rArmed ? Theme.Danger : Theme.Positive;
        rotateImage.color = rc;
        if (rotateShadow != null) rotateShadow.effectColor = Theme.Darken(rc, 0.55f);
        rotateTitle.text = rArmed ? "CANCEL" : "ROTATE";
    }

    void Update()
    {
        if (grid == null) return;
        bool anyArmed = grid.IsBombArmed || grid.IsRotateArmed;
        bool bombUsable = grid.IsBombArmed || (grid.CanUsePowerUp && !grid.IsRotateArmed);
        if (bombButton.interactable != bombUsable) bombButton.interactable = bombUsable;
        bool rotateUsable = grid.IsRotateArmed || (grid.CanUsePowerUp && !grid.IsBombArmed);
        if (rotateButton.interactable != rotateUsable) rotateButton.interactable = rotateUsable;
        bool canRefresh = grid.CanUsePowerUp && !anyArmed;
        if (refreshButton.interactable != canRefresh) refreshButton.interactable = canRefresh;
        bool canUndo = canRefresh && grid.HasUndo;
        if (undoButton.interactable != canUndo) undoButton.interactable = canUndo;

        string u = grid.remainingUndos > 0 ? "FREE x" + grid.remainingUndos : "WATCH AD";
        if (undoSub.text != u) undoSub.text = u;
        string r = grid.remainingRefreshes > 0 ? "FREE x" + grid.remainingRefreshes : "WATCH AD";
        if (refreshSub.text != r) refreshSub.text = r;

        // Secim modundaki buton nabiz atsin
        Pulse(bombButton.transform, grid.IsBombArmed);
        Pulse(rotateButton.transform, grid.IsRotateArmed);
    }

    static void Pulse(Transform t, bool on)
    {
        if (on)
        {
            float p = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.05f;
            t.localScale = new Vector3(p, p, 1f);
        }
        else if (t.localScale != Vector3.one)
        {
            t.localScale = Vector3.one;
        }
    }
}
