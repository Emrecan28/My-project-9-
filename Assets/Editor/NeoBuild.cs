#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Batch-mode iOS build: Unity -batchmode -buildTarget iOS -executeMethod NeoBuild.BuildIOS
public static class NeoBuild
{
    // Simulator build for screenshots: Unity -batchmode -buildTarget iOS -executeMethod NeoBuild.BuildIOSSimulator
    public static void BuildIOSSimulator()
    {
        var previous = PlayerSettings.iOS.sdkVersion;
        var previousArch = PlayerSettings.iOS.simulatorSdkArchitecture;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
        try
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "../Builds/iOS-Simulator",
                target = BuildTarget.iOS,
                options = BuildOptions.None
            });
            Debug.Log($"[NeoBuild] Simulator result: {report.summary.result}");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
        finally
        {
            PlayerSettings.iOS.sdkVersion = previous;
            PlayerSettings.iOS.simulatorSdkArchitecture = previousArch;
        }
    }

    public static void BuildIOS()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "../Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[NeoBuild] Result: {report.summary.result}, errors: {report.summary.totalErrors}, size: {report.summary.totalSize}");
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
#endif
