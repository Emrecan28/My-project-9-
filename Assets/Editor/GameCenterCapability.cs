#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// Adds the Game Center capability (entitlement + GameKit) to the generated Xcode project,
// so the "neoblock_best" leaderboard works without editing Signing & Capabilities by hand.
public static class GameCenterCapability
{
    [PostProcessBuild(900)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        var projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        var mainTarget = proj.GetUnityMainTargetGuid();

        var entitlements = "Unity-iPhone/NEOBLOCK.entitlements";
        var manager = new ProjectCapabilityManager(projPath, entitlements, null, mainTarget);
        manager.AddGameCenter();
        manager.WriteToFile();

        // Make sure UnityFramework links GameKit too (Social API lives there).
        proj.ReadFromFile(projPath);
        proj.AddFrameworkToProject(proj.GetUnityFrameworkTargetGuid(), "GameKit.framework", false);
        proj.WriteToFile(projPath);
    }
}
#endif
