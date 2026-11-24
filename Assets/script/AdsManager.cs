using System;
using System.Collections;
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    const string AttRequestedPlayerPrefsKey = "ATT_Requested";

    [Header("Android Settings")]
    [SerializeField] string androidAppKey;
    [SerializeField] string androidInterstitialAdUnitId;
    [SerializeField] string androidRewardedAdUnitId;

    [Header("iOS Settings")]
    [SerializeField] string iosAppKey;
    [SerializeField] string iosInterstitialAdUnitId;
    [SerializeField] string iosRewardedAdUnitId;

    [Header("General Settings")]
    [SerializeField] string gameOverPlacement;
    [SerializeField] string extraLifePlacement;
    [SerializeField] string undoPlacement;

    [SerializeField] string revivePlacement;
    [SerializeField] string levelCompletePlacement;

    [Header("Interstitial Pacing (Block Blast tarzi, ama oyuncuyu bogmadan)")]
    [Tooltip("Oyuncu bu kadar oyun bitirmeden interstitial gosterilmez (yeni oyuncu ilk izlenimi bozulmasin)")]
    [SerializeField] int minGamesBeforeInterstitial = 3;
    [Tooltip("Iki interstitial arasi en az bu kadar saniye olmali")]
    [SerializeField] float minSecondsBetweenInterstitials = 120f;
    [Tooltip("Oyuncu odullu reklam izlediyse bu kadar saniye interstitial gosterilmez")]
    [SerializeField] float minSecondsAfterRewarded = 60f;
    [Tooltip("Level modunda kac levelde bir interstitial denensin")]
    [SerializeField] int levelInterstitialEvery = 2;
    [Tooltip("Level modunda interstitial bu levelden itibaren baslar")]
    [SerializeField] int levelInterstitialStartLevel = 4;

    const string GamesFinishedKey = "Ads_GamesFinished";
    float lastInterstitialTime = -99999f;
    float lastRewardedTime = -99999f;

    [Tooltip("Reklam kapandiktan sonra odul olayinin gelmesi icin beklenecek sure (bazi aglar odulu kapanistan sonra bildirir)")]
    [SerializeField] float rewardGraceSeconds = 1.0f;

    // Runtime IDs
    string currentAppKey;
    string currentInterstitialId;
    string currentRewardedId;

    LevelPlayInterstitialAd interstitialAd;
    LevelPlayRewardedAd rewardedAd;

    bool initStarted;   // Init cagrildi
    bool initCompleted; // SDK hazir (OnInitSuccess geldi)
    bool idsSelected;

    Action<bool> interstitialCallback;
    Action<bool> rewardedCallback;
    bool rewardedEarned;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureMainThreadDispatcher();
    }

    static void EnsureMainThreadDispatcher()
    {
        if (UnityEngine.Object.FindAnyObjectByType<MainThreadDispatcher>() != null) return;

        var go = new GameObject("MainThreadDispatcher");
        go.AddComponent<MainThreadDispatcher>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        // SDK'yi oyun acilir acilmaz baslat. Eskiden ilk reklam istendigi anda
        // baslatiliyordu; bu yuzden ilk game over'da reklam hic hazir olmuyordu
        // ve iOS izin penceresi oyunun ortasinda cikiyordu.
        StartInitialization();
    }

    // ---------------- INIT ----------------

    void SelectPlatformIdsIfNeeded()
    {
        if (idsSelected) return;

#if UNITY_IOS
        currentAppKey = iosAppKey;
        currentInterstitialId = iosInterstitialAdUnitId;
        currentRewardedId = iosRewardedAdUnitId;
#else
        currentAppKey = androidAppKey;
        currentInterstitialId = androidInterstitialAdUnitId;
        currentRewardedId = androidRewardedAdUnitId;
#endif

        idsSelected = true;
    }

    void StartInitialization()
    {
        SelectPlatformIdsIfNeeded();
        if (initStarted) return;
        initStarted = true;

        RequestAttIfNeeded(InitializeLevelPlay);
    }

    void RequestAttIfNeeded(Action onCompleted)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (PlayerPrefs.GetInt(AttRequestedPlayerPrefsKey, 0) == 1)
        {
            onCompleted?.Invoke();
            return;
        }

        PlayerPrefs.SetInt(AttRequestedPlayerPrefsKey, 1);
        PlayerPrefs.Save();

        ATTWrapper.RequestTrackingAuthorization(_ => { onCompleted?.Invoke(); });
#else
        onCompleted?.Invoke();
#endif
    }

    void InitializeLevelPlay()
    {
        if (string.IsNullOrEmpty(currentAppKey))
        {
            Debug.LogWarning("AdsManager: App Key bos! Inspector'dan bu platformun App Key alanini doldurun.");
            return;
        }

        Debug.Log("AdsManager: LevelPlay init başlatılıyor.");
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(currentAppKey);
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (interstitialAd != null)
        {
            interstitialAd.DestroyAd();
            interstitialAd = null;
        }

        if (rewardedAd != null)
        {
            rewardedAd.DestroyAd();
            rewardedAd = null;
        }
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("AdsManager: LevelPlay init başarılı.");
            initCompleted = true;
            // Reklam nesneleri SDK hazir olduktan sonra olusturulmali
            CreateInterstitial();
            CreateRewarded();
        });
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.LogError("AdsManager: LevelPlay init başarısız: " + error);
            // Internet yoksa vb. 30 sn sonra tekrar dene
            initStarted = false;
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            CancelInvoke(nameof(StartInitialization));
            Invoke(nameof(StartInitialization), 30f);
        });
    }

    // ---------------- AD OBJECTS ----------------

    void CreateInterstitial()
    {
        if (interstitialAd != null) return;
        if (string.IsNullOrEmpty(currentInterstitialId))
        {
            Debug.LogWarning("AdsManager: Interstitial Ad Unit Id boş! Inspector'dan doldurun.");
        }

        interstitialAd = new LevelPlayInterstitialAd(currentInterstitialId ?? "");
        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.LoadAd();
    }

    void CreateRewarded()
    {
        if (rewardedAd != null) return;
        if (string.IsNullOrEmpty(currentRewardedId))
        {
            Debug.LogWarning("AdsManager: Rewarded Ad Unit Id boş! Inspector'dan doldurun.");
        }

        rewardedAd = new LevelPlayRewardedAd(currentRewardedId ?? "");
        rewardedAd.OnAdLoaded += OnRewardedLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedAd.OnAdDisplayed += OnRewardedDisplayed;
        rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        rewardedAd.OnAdRewarded += OnRewardedRewarded;
        rewardedAd.OnAdClosed += OnRewardedClosed;
        rewardedAd.LoadAd();
    }

    void LoadInterstitialAd()
    {
        if (interstitialAd != null) interstitialAd.LoadAd();
    }

    void LoadRewardedAd()
    {
        if (rewardedAd != null) rewardedAd.LoadAd();
    }

    // ---------------- PUBLIC API ----------------

    public bool IsRewardedReady()
    {
        return initCompleted && rewardedAd != null && rewardedAd.IsAdReady();
    }

    public bool IsInterstitialReady()
    {
        return initCompleted && interstitialAd != null && interstitialAd.IsAdReady();
    }

    public void ShowGameOverInterstitial(Action<bool> onCompleted)
    {
        ShowInterstitial(gameOverPlacement, onCompleted);
    }

    public void ShowExtraLifeRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(extraLifePlacement, onCompleted);
    }

    // Magazada "reklam izle, para kazan"
    public void ShowCoinsRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(extraLifePlacement, onCompleted);
    }

    public void ShowUndoRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(undoPlacement, onCompleted);
    }

    public void ShowReviveRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(revivePlacement, onCompleted);
    }

    // ---------------- PACING ----------------

    // Bir oyun (tur) bittiginde cagrilir
    public void RegisterGameFinished()
    {
        PlayerPrefs.SetInt(GamesFinishedKey, PlayerPrefs.GetInt(GamesFinishedKey, 0) + 1);
        PlayerPrefs.Save();
    }

    public bool CanShowPacedInterstitial()
    {
        if (PlayerPrefs.GetInt(GamesFinishedKey, 0) < minGamesBeforeInterstitial) return false;
        float now = Time.realtimeSinceStartup;
        if (now - lastInterstitialTime < minSecondsBetweenInterstitials) return false;
        if (now - lastRewardedTime < minSecondsAfterRewarded) return false;
        return IsInterstitialReady();
    }

    // Game over: kurallar uygunsa interstitial gosterir, degilse hemen devam eder
    public void TryShowGameOverInterstitial(Action onDone)
    {
        if (!CanShowPacedInterstitial()) { onDone?.Invoke(); return; }
        ShowInterstitial(gameOverPlacement, _ => onDone?.Invoke());
    }

    // Level gecisi: her N levelde bir ve belli bir levelden sonra
    public void TryShowLevelCompleteInterstitial(int completedLevel, Action onDone)
    {
        bool levelRule = completedLevel >= levelInterstitialStartLevel
                         && levelInterstitialEvery > 0
                         && completedLevel % levelInterstitialEvery == 0;
        if (!levelRule || !CanShowPacedInterstitial()) { onDone?.Invoke(); return; }
        ShowInterstitial(levelCompletePlacement, _ => onDone?.Invoke());
    }

    void ShowInterstitial(string placement, Action<bool> onCompleted)
    {
        if (!initCompleted || interstitialAd == null)
        {
            StartInitialization();
            onCompleted?.Invoke(false);
            return;
        }

        if (interstitialCallback != null)
        {
            // Zaten bir reklam acik
            onCompleted?.Invoke(false);
            return;
        }

        bool capped = !string.IsNullOrEmpty(placement) && LevelPlayInterstitialAd.IsPlacementCapped(placement);
        if (!interstitialAd.IsAdReady() || capped)
        {
            Debug.LogWarning("AdsManager: Interstitial hazır değil veya placement capped.");
            if (!interstitialAd.IsAdReady()) interstitialAd.LoadAd();
            onCompleted?.Invoke(false);
            return;
        }

        interstitialCallback = onCompleted;
        if (string.IsNullOrEmpty(placement)) interstitialAd.ShowAd();
        else interstitialAd.ShowAd(placement);
    }

    void ShowRewarded(string placement, Action<bool> onCompleted)
    {
        if (!initCompleted || rewardedAd == null)
        {
            StartInitialization();
            onCompleted?.Invoke(false);
            return;
        }

        if (rewardedCallback != null)
        {
            onCompleted?.Invoke(false);
            return;
        }

        bool capped = !string.IsNullOrEmpty(placement) && LevelPlayRewardedAd.IsPlacementCapped(placement);
        if (!rewardedAd.IsAdReady() || capped)
        {
            Debug.LogWarning("AdsManager: Rewarded hazır değil veya placement capped.");
            if (!rewardedAd.IsAdReady()) rewardedAd.LoadAd();
            onCompleted?.Invoke(false);
            return;
        }

        rewardedCallback = onCompleted;
        rewardedEarned = false;
        if (string.IsNullOrEmpty(placement)) rewardedAd.ShowAd();
        else rewardedAd.ShowAd(placement);
    }

    void FinishInterstitial(bool success)
    {
        if (success) lastInterstitialTime = Time.realtimeSinceStartup;
        var callback = interstitialCallback;
        interstitialCallback = null;
        callback?.Invoke(success);
    }

    void FinishRewarded(bool earned)
    {
        if (earned) lastRewardedTime = Time.realtimeSinceStartup;
        var callback = rewardedCallback;
        rewardedCallback = null;
        rewardedEarned = false;
        callback?.Invoke(earned);
    }

    // ---------------- INTERSTITIAL EVENTS (SDK thread'inden gelebilir) ----------------

    void OnInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("AdsManager: Interstitial yüklendi."));
    }

    void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.LogWarning("AdsManager: Interstitial yüklenemedi: " + error);
            CancelInvoke(nameof(LoadInterstitialAd));
            Invoke(nameof(LoadInterstitialAd), 10f);
        });
    }

    void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("AdsManager: Interstitial görüntüleniyor."));
    }

    void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.LogError("AdsManager: Interstitial gösterim hatası: " + error);
            LoadInterstitialAd();
            FinishInterstitial(false);
        });
    }

    void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            LoadInterstitialAd();
            FinishInterstitial(true);
        });
    }

    // ---------------- REWARDED EVENTS ----------------

    void OnRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("AdsManager: Rewarded yüklendi."));
    }

    void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.LogWarning("AdsManager: Rewarded yüklenemedi: " + error);
            CancelInvoke(nameof(LoadRewardedAd));
            Invoke(nameof(LoadRewardedAd), 10f);
        });
    }

    void OnRewardedDisplayed(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() => Debug.Log("AdsManager: Rewarded görüntüleniyor."));
    }

    void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.LogError("AdsManager: Rewarded gösterim hatası: " + error);
            LoadRewardedAd();
            FinishRewarded(false);
        });
    }

    void OnRewardedRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("AdsManager: Reward kazanıldı.");
            rewardedEarned = true;
        });
    }

    void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            LoadRewardedAd();
            // Odul sadece reklam sonuna kadar izlendiyse verilir.
            // Bazi aglar odulu kapanistan hemen sonra bildirdigi icin kisa bir sure beklenir.
            StartCoroutine(ResolveRewardAfterClose());
        });
    }

    IEnumerator ResolveRewardAfterClose()
    {
        float waited = 0f;
        while (!rewardedEarned && waited < rewardGraceSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        FinishRewarded(rewardedEarned);
    }
}
