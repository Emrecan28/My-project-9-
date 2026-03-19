using System;
using UnityEngine;
using Unity.Services.LevelPlay;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

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

    // Runtime IDs
    string currentAppKey;
    string currentInterstitialId;
    string currentRewardedId;

    LevelPlayInterstitialAd interstitialAd;
    LevelPlayRewardedAd rewardedAd;

    bool initialized;

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
    }

    void Start()
    {
        // Platforma gore ID secimi
#if UNITY_IOS
        currentAppKey = iosAppKey;
        currentInterstitialId = iosInterstitialAdUnitId;
        currentRewardedId = iosRewardedAdUnitId;

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Debug.Log("AdsManager: iOS ATT izni isteniyor...");
            ATTWrapper.RequestTrackingAuthorization((status) =>
            {
                Debug.Log("AdsManager: iOS ATT durumu: " + status);
                // Status: 0=NotDetermined, 1=Restricted, 2=Denied, 3=Authorized
                InitializeLevelPlay();
            });
        }
        else
        {
            InitializeLevelPlay();
        }
#else
        currentAppKey = androidAppKey;
        currentInterstitialId = androidInterstitialAdUnitId;
        currentRewardedId = androidRewardedAdUnitId;
        InitializeLevelPlay();
#endif
    }

    void InitializeLevelPlay()
    {
        if (string.IsNullOrEmpty(currentAppKey))
        {
#if UNITY_IOS
            Debug.LogWarning("AdsManager: iOS App Key bos! Lutfen Inspector'dan iOS App Key alanini doldurun.");
#else
            Debug.LogWarning("AdsManager: Android App Key bos! Lutfen Inspector'dan Android App Key alanini doldurun.");
#endif
            return;
        }

        if (initialized)
        {
            return;
        }

        Debug.Log("AdsManager: LevelPlay init başlatılıyor.");
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(currentAppKey);
        initialized = true;
        CreateInterstitial();
        CreateRewarded();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
        }

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
        Debug.Log("AdsManager: LevelPlay init başarılı.");
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError("AdsManager: LevelPlay init başarısız: " + error);
    }

    void CreateInterstitial()
    {
        if (string.IsNullOrEmpty(currentInterstitialId))
        {
            Debug.LogWarning("AdsManager: Interstitial Ad Unit Id boş, interstitial oluşturulmadı.");
            return;
        }

        interstitialAd = new LevelPlayInterstitialAd(currentInterstitialId);
        interstitialAd.OnAdLoaded += OnInterstitialLoaded;
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdDisplayed += OnInterstitialDisplayed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        Debug.Log("AdsManager: Interstitial oluşturuldu, yükleniyor.");
        interstitialAd.LoadAd();
    }

    void CreateRewarded()
    {
        if (string.IsNullOrEmpty(currentRewardedId))
        {
            Debug.LogWarning("AdsManager: Rewarded Ad Unit Id boş, rewarded oluşturulmadı.");
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(currentRewardedId);
        rewardedAd.OnAdLoaded += OnRewardedLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedAd.OnAdDisplayed += OnRewardedDisplayed;
        rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        rewardedAd.OnAdRewarded += OnRewardedRewarded;
        rewardedAd.OnAdClosed += OnRewardedClosed;
        Debug.Log("AdsManager: Rewarded oluşturuldu, yükleniyor.");
        rewardedAd.LoadAd();
    }

    public void ShowGameOverInterstitial(Action<bool> onCompleted)
    {
        ShowInterstitial(gameOverPlacement, onCompleted);
    }

    public void ShowExtraLifeRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(extraLifePlacement, onCompleted);
    }

    void ShowInterstitial(string placement, Action<bool> onCompleted)
    {
        if (interstitialAd == null)
        {
            Debug.LogWarning("AdsManager: Interstitial nesnesi yok, yeniden oluşturuluyor.");
            CreateInterstitial();
            onCompleted?.Invoke(false);
            return;
        }

        bool ready = interstitialAd.IsAdReady();
        bool capped = false;

        if (!string.IsNullOrEmpty(placement))
        {
            capped = LevelPlayInterstitialAd.IsPlacementCapped(placement);
        }

        if (!ready || capped)
        {
            Debug.LogWarning("AdsManager: Interstitial hazır değil veya placement capped, yeniden yükleniyor.");
            interstitialAd.LoadAd();
            onCompleted?.Invoke(false);
            return;
        }

        interstitialCallback = onCompleted;

        if (string.IsNullOrEmpty(placement))
        {
            Debug.Log("AdsManager: Interstitial gösteriliyor (placement yok).");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.Log("AdsManager: Interstitial gösteriliyor, placement=" + placement);
            interstitialAd.ShowAd(placement);
        }
    }

    void ShowRewarded(string placement, Action<bool> onCompleted)
    {
        if (rewardedAd == null)
        {
            Debug.LogWarning("AdsManager: Rewarded nesnesi yok, yeniden oluşturuluyor.");
            CreateRewarded();
            onCompleted?.Invoke(false);
            return;
        }

        bool ready = rewardedAd.IsAdReady();
        bool capped = false;

        if (!string.IsNullOrEmpty(placement))
        {
            capped = LevelPlayRewardedAd.IsPlacementCapped(placement);
        }

        if (!ready || capped)
        {
            Debug.LogWarning("AdsManager: Rewarded hazır değil veya placement capped, yeniden yükleniyor.");
            rewardedAd.LoadAd();
            onCompleted?.Invoke(false);
            return;
        }

        rewardedCallback = onCompleted;
        rewardedEarned = false;

        if (string.IsNullOrEmpty(placement))
        {
            Debug.Log("AdsManager: Rewarded gösteriliyor (placement yok).");
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("AdsManager: Rewarded gösteriliyor, placement=" + placement);
            rewardedAd.ShowAd(placement);
        }
    }

    void OnInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Interstitial yüklendi.");
    }

    void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("AdsManager: Interstitial yüklenemedi: " + error);
        
        if (interstitialCallback != null)
        {
            var callback = interstitialCallback;
            interstitialCallback = null;
            MainThreadDispatcher.Enqueue(() => { callback(false); });
        }
        
        // Yeniden yüklemeyi dene
        CancelInvoke(nameof(LoadInterstitialAd));
        Invoke(nameof(LoadInterstitialAd), 5f);
    }

    void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("AdsManager: Interstitial yeniden yükleniyor...");
            interstitialAd.LoadAd();
        }
    }

    void OnInterstitialDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Interstitial görüntüleniyor.");
    }

    void OnInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError("AdsManager: Interstitial gösterim hatası: " + error);
        interstitialAd.LoadAd();
        
        if (interstitialCallback != null)
        {
            var callback = interstitialCallback;
            interstitialCallback = null;
            MainThreadDispatcher.Enqueue(() => { callback(false); });
        }
    }

    void OnInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Interstitial kapandı, yeniden yükleniyor.");
        interstitialAd.LoadAd();

        if (interstitialCallback != null)
        {
            var callback = interstitialCallback;
            interstitialCallback = null;
            MainThreadDispatcher.Enqueue(() => { callback(true); });
        }
    }

    void OnRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Rewarded yüklendi.");
    }

    void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("AdsManager: Rewarded yüklenemedi: " + error);
        
        if (rewardedCallback != null)
        {
            var callback = rewardedCallback;
            rewardedCallback = null;
            MainThreadDispatcher.Enqueue(() => { callback(false); });
        }
        
        // Yeniden yüklemeyi dene
        CancelInvoke(nameof(LoadRewardedAd));
        Invoke(nameof(LoadRewardedAd), 5f);
    }

    void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            Debug.Log("AdsManager: Rewarded yeniden yükleniyor...");
            rewardedAd.LoadAd();
        }
    }

    void OnRewardedDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Rewarded görüntüleniyor.");
    }

    void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError("AdsManager: Rewarded gösterim hatası: " + error);
        rewardedAd.LoadAd();
        
        if (rewardedCallback != null)
        {
            var callback = rewardedCallback;
            rewardedCallback = null;
            MainThreadDispatcher.Enqueue(() => { callback(false); });
        }
        
        rewardedEarned = false;
    }

    void OnRewardedRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log("AdsManager: Reward kazanıldı.");
        rewardedEarned = true;
    }

    void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdsManager: Rewarded kapandı, yeniden yükleniyor.");
        rewardedAd.LoadAd();

        if (rewardedCallback != null)
        {
            var callback = rewardedCallback;
            rewardedCallback = null;
            bool earned = rewardedEarned;
            rewardedEarned = false;
            MainThreadDispatcher.Enqueue(() => { callback(earned); });
        }
    }
}
