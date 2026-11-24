#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

// Runs after the iOS Resolver writes the Podfile (order 40) and before it runs `pod install` (order 50).
// Forces every pod to iOS 15.0: newer Xcode rejects the 10-13 targets some ad SDK pods ship with.
public static class PodfileDeploymentTargetFix
{
    const string Marker = "# NEOBLOCK deployment target fix";

    [PostProcessBuild(45)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        var podfile = Path.Combine(path, "Podfile");
        if (!File.Exists(podfile)) return;

        var text = File.ReadAllText(podfile);
        if (text.Contains(Marker)) return;

        text += "\n" + Marker + "\n" +
                "post_install do |installer|\n" +
                "  installer.pods_project.targets.each do |t|\n" +
                "    t.build_configurations.each do |config|\n" +
                "      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '15.0'\n" +
                "    end\n" +
                "  end\n" +
                "end\n";
        File.WriteAllText(podfile, text);
    }
}
#endif
