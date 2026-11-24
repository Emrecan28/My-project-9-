using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Hafif telefon titresimi (blok koyma, patlama, game over).
// iOS'ta Apple'in "Taptic" geri bildirimi kullanilir; Android'de kisa titresim.
public static class Haptics
{
    const string PrefKey = "HapticsSetting";

    public static bool IsOn
    {
        get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
        set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Toggle() => IsOn = !IsOn;

    public static void Light() => Play(0);
    public static void Medium() => Play(1);
    public static void Heavy() => Play(2);

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void _NeoHaptic(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static int sdkInt = -1;
    // Unity'nin VIBRATE iznini manifest'e eklemesi icin Handheld.Vibrate referansi gerekir.
    // Bu alan hep false; kod hic calismaz ama referans derlemede kalir.
    public static bool forceVibratePermissionReference;
#endif

    static void Play(int style)
    {
        if (!IsOn) return;

#if UNITY_IOS && !UNITY_EDITOR
        _NeoHaptic(style);
#elif UNITY_ANDROID && !UNITY_EDITOR
        if (forceVibratePermissionReference) Handheld.Vibrate();
        try
        {
            if (vibrator == null)
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdkInt = version.GetStatic<int>("SDK_INT");
                }
            }
            if (vibrator == null) return;

            long ms = style == 0 ? 15 : (style == 1 ? 30 : 55);
            if (sdkInt >= 26)
            {
                int amplitude = style == 0 ? 60 : (style == 1 ? 140 : 255);
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", ms);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Haptics: " + e.Message);
        }
#endif
    }
}
