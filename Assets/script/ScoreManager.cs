using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI highScoreText;

    private int score = 0;
    private int highScore = 0;

    // Level Modu icin Hedef Puan
    private int targetScore = 0;
    private bool isLevelMode = false;
    private bool isLevelCompleted = false; // Level tamamlandi mi kontrolu
    private Coroutine scorePulseCoroutine;

    // Pulse animasyonu icin orijinal degerler (bir kere alinir; yoksa ust uste
    // kombo geldiginde yazi buyuk/renkli kalabiliyordu)
    private bool pulseBaseCaptured;
    private Vector3 scoreBaseScale, highBaseScale;
    private Color scoreBaseColor, highBaseColor;

    // Ekranda gosterilen skor; gercek skora dogru hizla sayarak yaklasir
    private int displayedScore;

    void Update()
    {
        if (displayedScore == score) return;
        int diff = score - displayedScore;
        // Fark buyukse hizli, kucukse tek tek say (yaklasik 0.4 sn'de yetisir)
        int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(diff) * Time.deltaTime * 8f));
        displayedScore = diff > 0 ? Mathf.Min(score, displayedScore + step) : Mathf.Max(score, displayedScore - step);
        UpdateUI();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        highScore = PlayerPrefs.GetInt("HighScore", 0); // Kayitli rekoru yukle
    }

    void Start()
    {
        // HUD: buyuk beyaz skor, altin rengi rekor (Block Blast tarzi)
        if (currentScoreText != null)
        {
            currentScoreText.color = Color.white;
            if (currentScoreText.enableAutoSizing) currentScoreText.fontSizeMax *= 1.35f;
            else currentScoreText.fontSize *= 1.35f;
            // "565/1000" gibi uzun skorlar alt satira kaymasin: tek satir, tasarsa kutuya sigacak kadar kucul
            currentScoreText.textWrappingMode = TextWrappingModes.NoWrap;
            currentScoreText.overflowMode = TextOverflowModes.Overflow;
            RectTransform sr = currentScoreText.rectTransform;
            // Kutu olcekli (x2.2) oldugu icin 420 genislik ekranda ~900 birim eder; yazi hep ortada
            if (sr.sizeDelta.x < 420f) sr.sizeDelta = new Vector2(420f, sr.sizeDelta.y);
            currentScoreText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        }
        if (highScoreText != null) highScoreText.color = Theme.Highlight;

        ResetLevelStatus();

        // LevelManager'dan veri al
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            isLevelMode = true;
            targetScore = LevelManager.Instance.currentLevelData.targetScore;
        }
        else
        {
            isLevelMode = false;
        }

        UpdateUI();
    }

    // Scene basladiginda ScoreManager'in LevelManager verisini tekrar okumasini sagla
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Yeni bir sahne (ozellikle GameScene) yuklendiginde LevelManager verilerini tekrar kontrol et
        if (LevelManager.Instance != null)
        {
            if (LevelManager.Instance.currentLevelData != null)
            {
                isLevelMode = true;
                targetScore = LevelManager.Instance.currentLevelData.targetScore;
            }
            else
            {
                isLevelMode = false;
            }
        }
        UpdateUI();
    }

    void ResetLevelStatus()
    {
        isLevelCompleted = false;
    }

    public void AddScore(int amount)
    {
        score += amount;

        // Endless modda rekor takibi
        if (!isLevelMode)
        {
            if (score > highScore)
            {
                highScore = score;
                PlayerPrefs.SetInt("HighScore", highScore); // Yeni rekoru kaydet
            }
        }

        UpdateUI();

        // Level Modu Kontrolu
        if (isLevelMode && score >= targetScore && !isLevelCompleted)
        {
            CompleteLevel();
        }
    }

    void CompleteLevel()
    {
        isLevelCompleted = true; // Tekrar tetiklenmesini onle

        // Level Tamamlandi!
        // GridManager uzerinden tebrik mesaji gosterilebilir veya burada basitce gecis yapilabilir.
        // Kullanici "LEVEL COMPLETE!" yazisi istiyor.

        // GridManager'a ulasip oyunu durdurmasini veya efekti baslatmasini isteyelim
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null)
        {
            gm.ShowLevelComplete();
        }
    }

    public int GetCurrentScore()
    {
        return score;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
        displayedScore = newScore; // Kayittan yukleme/geri alma: animasyonsuz
        UpdateUI();
    }

    public void ResetScore()
    {
        score = 0;
        displayedScore = 0;
        ResetLevelStatus();

        // Level modundaysak target'i guncelle (Restart durumunda)
        if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
        {
            isLevelMode = true;
            targetScore = LevelManager.Instance.currentLevelData.targetScore;
        }
        else
        {
            isLevelMode = false;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (currentScoreText == null) return;

        if (isLevelMode)
        {
            // Level Modu: buyuk skor / hedef
            currentScoreText.text = displayedScore + "/" + targetScore;

            if (highScoreText != null)
            {
                int levelNo = (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
                    ? LevelManager.Instance.currentLevelData.levelNumber : 0;
                bool daily = LevelManager.Instance != null && LevelManager.Instance.IsDailyMode;
                highScoreText.text = daily ? "DAILY  GOAL " + targetScore
                    : (levelNo > 0 ? "LEVEL " + levelNo + "  GOAL " + targetScore : "GOAL " + targetScore);
            }
        }
        else
        {
            // Sonsuz Mod: sadece buyuk skor (Block Blast gibi sade), ustte rekor
            currentScoreText.text = displayedScore.ToString();

            if (highScoreText != null)
            {
                highScoreText.text = "BEST " + Mathf.Max(highScore, score);
            }
        }
    }

    public void PlayComboScorePulse(bool bigCombo)
    {
        if (currentScoreText == null) return;

        if (scorePulseCoroutine != null)
        {
            StopCoroutine(scorePulseCoroutine);
        }

        scorePulseCoroutine = StartCoroutine(ScorePulseRoutine(bigCombo));
    }

    void CapturePulseBase()
    {
        if (pulseBaseCaptured) return;
        scoreBaseScale = currentScoreText.transform.localScale;
        scoreBaseColor = currentScoreText.color;
        if (highScoreText != null)
        {
            highBaseScale = highScoreText.transform.localScale;
            highBaseColor = highScoreText.color;
        }
        pulseBaseCaptured = true;
    }

    System.Collections.IEnumerator ScorePulseRoutine(bool bigCombo)
    {
        CapturePulseBase();

        float duration = bigCombo ? 0.38f : 0.24f;
        float scaleBoost = bigCombo ? 1.25f : 1.12f;
        Color pulseColor = bigCombo ? Theme.Highlight : Color.white;
        bool hasHigh = highScoreText != null;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);

            currentScoreText.transform.localScale = scoreBaseScale * Mathf.Lerp(1f, scaleBoost, wave);
            currentScoreText.color = Color.Lerp(scoreBaseColor, pulseColor, wave);

            if (hasHigh)
            {
                highScoreText.transform.localScale = highBaseScale * Mathf.Lerp(1f, bigCombo ? 1.12f : 1.06f, wave);
                highScoreText.color = Color.Lerp(highBaseColor, pulseColor, wave * 0.6f);
            }

            yield return null;
        }

        currentScoreText.transform.localScale = scoreBaseScale;
        currentScoreText.color = scoreBaseColor;
        if (hasHigh)
        {
            highScoreText.transform.localScale = highBaseScale;
            highScoreText.color = highBaseColor;
        }

        scorePulseCoroutine = null;
    }
}
