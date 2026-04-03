#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class iOSPostProcess
{
    const string UserTrackingUsageDescriptionKey = "NSUserTrackingUsageDescription";
    const string DefaultUserTrackingUsageDescription = "We use your device identifier to deliver more relevant ads and improve the app experience.";

    [PostProcessBuild(1000)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        var anyUpdated = false;

        var rootPlistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        anyUpdated |= TryEnsureUserTrackingUsageDescription(rootPlistPath);

        var allPlists = Directory.GetFiles(pathToBuiltProject, "Info.plist", SearchOption.AllDirectories);
        for (var i = 0; i < allPlists.Length; i++)
        {
            anyUpdated |= TryEnsureUserTrackingUsageDescription(allPlists[i]);
        }

        if (!anyUpdated)
        {
            var fallbackPlists = Directory.GetFiles(pathToBuiltProject, "*.plist", SearchOption.AllDirectories);
            for (var i = 0; i < fallbackPlists.Length; i++)
            {
                anyUpdated |= TryEnsureUserTrackingUsageDescription(fallbackPlists[i]);
            }
        }
    }

    static bool TryEnsureUserTrackingUsageDescription(string plistPath)
    {
        if (!File.Exists(plistPath))
        {
            return false;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var root = plist.root;
        if (!root.values.TryGetValue(UserTrackingUsageDescriptionKey, out var existing) || string.IsNullOrWhiteSpace(existing.AsString()))
        {
            root.SetString(UserTrackingUsageDescriptionKey, DefaultUserTrackingUsageDescription);
            plist.WriteToFile(plistPath);
            return true;
        }

        return false;
    }
}
#endif
