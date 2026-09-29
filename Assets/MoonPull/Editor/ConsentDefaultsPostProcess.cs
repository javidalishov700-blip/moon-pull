#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Makes Firebase Analytics start in "denied" consent mode on iOS until the UMP result is applied at runtime
    /// (Android uses the meta-data entries in the custom AndroidManifest).
    /// </summary>
    public static class ConsentDefaultsPostProcess
    {
        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;
            root.SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_ANALYTICS_STORAGE", false);
            root.SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_STORAGE", false);
            root.SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_USER_DATA", false);
            root.SetBoolean("GOOGLE_ANALYTICS_DEFAULT_ALLOW_AD_PERSONALIZATION_SIGNALS", false);
            // Portrait-only, full-screen game: opt out of iPad multitasking so orientation stays locked.
            root.SetBoolean("UIRequiresFullScreen", true);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
