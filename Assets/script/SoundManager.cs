using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance; // Diğer scriptlerden ulaşmak için

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource; // Muzik icin ayri kaynak

    [Header("Clips")]
    public AudioClip blockPlaceSound;
    public AudioClip rowClearSound;
    public AudioClip backgroundMusic; // Menu muzigi
    [Tooltip("Oyun ici muzik. Bos birakilirsa Assets/Resources/GameplayMusic dosyasi otomatik yuklenir.")]
    public AudioClip gameplayMusic;

    [Header("Volumes")]
    [Range(0f, 1f)] public float menuMusicVolume = 0.5f;
    [Range(0f, 1f)] public float gameplayMusicVolume = 0.3f; // Oyun ici daha sakin
    [Tooltip("Oyun icinde muzik, menu sesinin bu orani kadar calar (kisik arka plan)")]
    [Range(0f, 1f)] public float gameplayMusicFactor = 0.3f;
    [Tooltip("Parcanin basindaki garip/sessiz kismi atla (saniye). 0 = otomatik: bastaki sessizligi bulur.")]
    public float musicStartOffset = 0f;
    [Tooltip("Muzik acilirken ve sahne degisirken ses gecis suresi")]
    public float musicFadeTime = 1.2f;
    private float currentMusicVolume = 0.5f;
    private float targetMusicVolume = 0.5f;   // gitmek istedigimiz ses
    private float fadedMusicVolume = 0f;      // su an calan (yumusak gecisli) ses
    private float loopStartTime = 0f;

    // Ayarlar
    public bool IsMusicOn { get; private set; } = true;
    public bool IsSfxOn { get; private set; } = true;

    private void Awake()
    {
        // Singleton yapısı: Sahne değişse bile bu obje tek kalır
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne gecislerinde yok olmasin

            // --- OTOMATIK KURULUM ---
            // Eger AudioSource'lar atanmamissa kodla olustur
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false; // Efektler otomatik baslamasin
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true; // Muzik dongude olsun
                musicSource.playOnAwake = false;
                musicSource.volume = 0.5f; // Varsayilan ses seviyesi
            }
            // -------------------------

            // Sahnedeki muzik kaynagi "Play On Awake" ile kendiliginden basladiysa durdur:
            // muzigi her zaman bizim yumusak baslangicimiz (fade-in + bastaki garip kismi atlama) baslatsin
            musicSource.playOnAwake = false;
            if (musicSource.isPlaying) musicSource.Stop();
            musicSource.volume = 0f;

            if (gameplayMusic == null) gameplayMusic = Resources.Load<AudioClip>("GameplayMusic");
            currentMusicVolume = menuMusicVolume;

            LoadSettings(); // Kayitli ayarlari yukle
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Eger bu obje asil Instance degilse (yani yok edilecek fazlaliksa) hicbir islem yapma
        if (Instance != this) return;

        // Play On Awake ile baslamis olabilir: bizim yumusak baslangicimizla yeniden baslat
        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
        ApplyMusicForScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ApplyMusicForScene(scene.name);
    }

    // Menu ve level secim: menu muzigi. Oyun: oyun ici muzik (varsa), yoksa sessiz.
    void ApplyMusicForScene(string sceneName)
    {
        if (sceneName == "GameScene")
        {
            // Ayri oyun muzigi yoksa menu muzigi KESILMEDEN devam eder, sadece kisilir
            AudioClip clip = gameplayMusic != null ? gameplayMusic : backgroundMusic;
            float vol = gameplayMusic != null ? gameplayMusicVolume : menuMusicVolume * gameplayMusicFactor;
            if (clip != null) PlayClip(clip, vol);
            else StopMusic();
        }
        else if (sceneName == "MainMenu" || sceneName == "LevelScene")
        {
            PlayClip(backgroundMusic, menuMusicVolume);
        }
    }

    void PlayClip(AudioClip clip, float volume)
    {
        if (musicSource == null || clip == null) return;
        targetMusicVolume = volume;
        currentMusicVolume = volume;

        // Ayni parca zaten caliyorsa bastan baslatma: sadece ses yumusakca hedefe gider
        if (musicSource.isPlaying && musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        loopStartTime = FindMusicStart(clip);
        fadedMusicVolume = 0f; // sessizden baslayip yavasca acilir
        musicSource.volume = 0f;
        musicSource.time = Mathf.Clamp(loopStartTime, 0f, Mathf.Max(0f, clip.length - 0.1f));
        musicSource.Play();
    }

    // Parcanin basindaki sessizligi/tikirtiyi atlamak icin ilk "duyulur" noktayi bulur
    float FindMusicStart(AudioClip clip)
    {
        if (musicStartOffset > 0f) return musicStartOffset;
        try
        {
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            int channels = Mathf.Max(1, clip.channels);
            int frames = Mathf.Min(clip.samples, clip.frequency * 4); // ilk 4 saniye
            float[] data = new float[frames * channels];
            if (!clip.GetData(data, 0)) return 0f;
            for (int i = 0; i < data.Length; i++)
            {
                if (Mathf.Abs(data[i]) > 0.02f)
                {
                    float t = (i / channels) / (float)clip.frequency;
                    return Mathf.Max(0f, t - 0.05f);
                }
            }
        }
        catch (System.Exception) { }
        return 0f; // okunamazsa bastan cal (fade-in yine yumusatir)
    }

    // Ses gecisleri (fade) ve dongu basinda garip kismi tekrar atlama
    void Update()
    {
        if (Instance != this || musicSource == null) return;
        float goal = IsMusicOn ? targetMusicVolume : 0f;
        float speed = Mathf.Max(0.01f, musicFadeTime);
        fadedMusicVolume = Mathf.MoveTowards(fadedMusicVolume, goal, Time.unscaledDeltaTime / speed);
        musicSource.volume = fadedMusicVolume;
        musicSource.mute = !IsMusicOn && fadedMusicVolume <= 0.001f;

        if (loopStartTime > 0.05f && musicSource.isPlaying && musicSource.time < loopStartTime - 0.05f)
        {
            musicSource.time = loopStartTime;
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void LoadSettings()
    {
        // Varsayilan olarak 1 (Acik)
        IsMusicOn = PlayerPrefs.GetInt("MusicSetting", 1) == 1;
        IsSfxOn = PlayerPrefs.GetInt("SfxSetting", 1) == 1;

        ApplySettings();
    }

    private void ApplySettings()
    {
        // Muzik sesi Update'te yumusak gecisle (fade) ayarlanir
        if (musicSource != null && !IsMusicOn && fadedMusicVolume <= 0.001f) musicSource.mute = true;
        if (musicSource != null && IsMusicOn) musicSource.mute = false;

        if (sfxSource != null)
        {
            sfxSource.mute = !IsSfxOn;
            // Sfx icin volume kontrolu (Sfx genellikle full ses calinir)
            sfxSource.volume = IsSfxOn ? 1.0f : 0f;
        }
    }

    // ARTIK DIREKT AYARLAYABILME FONKSIYONLARI DA VAR
    public void SetMusic(bool isOn)
    {
        IsMusicOn = isOn;
        PlayerPrefs.SetInt("MusicSetting", IsMusicOn ? 1 : 0);
        ApplySettings();
    }

    public void SetSfx(bool isOn)
    {
        IsSfxOn = isOn;
        PlayerPrefs.SetInt("SfxSetting", IsSfxOn ? 1 : 0);
        ApplySettings();
    }

    public void ToggleMusic()
    {
        SetMusic(!IsMusicOn);
    }

    public void ToggleSfx()
    {
        SetSfx(!IsSfxOn);
    }

    public void PlayMusic()
    {
        PlayClip(backgroundMusic, menuMusicVolume);
    }

    public void PlayBlockPlace()
    {
        if (!IsSfxOn) return; // Ses kapaliysa calama

        // Sesi kısa ve hızlıca çalar (üst üste binmeleri engellemez, daha doğal duyulur)
        if (sfxSource != null && blockPlaceSound != null)
            sfxSource.PlayOneShot(blockPlaceSound);
    }

    public void PlayRowClear()
    {
        if (!IsSfxOn) return;

        if (sfxSource != null && rowClearSound != null)
        {
            // Patlama sesi genellikle daha baskn olmal, gerekirse ses seviyesini (volume) 1.0f yapabilirsin
            sfxSource.PlayOneShot(rowClearSound, 1.0f);
        }
    }

    public void PlayComboExplosion()
    {
        if (!IsSfxOn) return;

        if (sfxSource != null && rowClearSound != null)
        {
            // Kombo patlama: Daha yuksek ses ve yankili efekt
            sfxSource.PlayOneShot(rowClearSound, 1.0f);
            // Hafif gecikmeli ikinci ses ile tokluk hissi ver
            StartCoroutine(PlayDelayedSound(rowClearSound, 0.05f));
        }
    }

    // Seri (combo) arttikca patlama sesi yarim ton yarim ton yukselir (Block Blast hissi).
    // Ayri bir kaynak kullanir ki diger seslerin pitch'i bozulmasin.
    private AudioSource clearSource;

    public void PlayClearWithStreak(int streak, bool big)
    {
        if (!IsSfxOn || rowClearSound == null) return;
        if (clearSource == null)
        {
            clearSource = gameObject.AddComponent<AudioSource>();
            clearSource.playOnAwake = false;
            if (sfxSource != null)
            {
                clearSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
                clearSource.volume = sfxSource.volume;
            }
        }
        int steps = Mathf.Clamp(streak - 1, 0, 8);
        clearSource.pitch = Mathf.Pow(1.059463f, steps * 1.5f); // her seri adiminda ~1.5 yarim ton
        clearSource.PlayOneShot(rowClearSound, 1.0f);
        if (big)
        {
            StartCoroutine(PlayDelayedSound(rowClearSound, 0.05f));
        }
    }

    private System.Collections.IEnumerator PlayDelayedSound(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsSfxOn && sfxSource != null)
            sfxSource.PlayOneShot(clip, 1.0f);
    }
}
