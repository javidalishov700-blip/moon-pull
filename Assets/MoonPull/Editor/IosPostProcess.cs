// Runs on the generated Xcode project for every iOS build: export compliance, build number, languages,
// the localized tracking prompt and the app icon asset catalog (App Store Connect error 91111 without it).
#if UNITY_IOS
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace MoonPull.EditorTools
{
    public static class IosPostProcess
    {
        private const string AppIconName = "AppIcon";

        private const string ContentsJson = @"{
  ""images"" : [
    { ""filename"" : ""Icon-1024.png"", ""idiom"" : ""universal"", ""platform"" : ""ios"", ""size"" : ""1024x1024"" },
    { ""filename"" : ""Icon-1024.png"", ""idiom"" : ""ios-marketing"", ""scale"" : ""1x"", ""size"" : ""1024x1024"" }
  ],
  ""info"" : { ""author"" : ""xcode"", ""version"" : 1 }
}
";

        // Matches the languages in Resources/Localization/MoonPull_UI.csv.
        private static readonly (string Code, string Tracking)[] Localizations =
        {
            ("en", "Moon Pull uses this to show ads that are more relevant to you. Ads still show if you decline."),
            ("tr", "Moon Pull bunu sana daha uygun reklamlar göstermek için kullanır. Reddetsen de reklamlar gösterilir."),
            ("es", "Moon Pull usa esto para mostrarte anuncios más relevantes. Seguirás viendo anuncios si lo rechazas."),
            ("pt-BR", "Moon Pull usa isso para mostrar anúncios mais relevantes para você. Os anúncios continuam aparecendo se você recusar."),
            ("de", "Moon Pull nutzt dies, um dir relevantere Werbung zu zeigen. Werbung wird auch bei Ablehnung angezeigt."),
            ("fr", "Moon Pull l'utilise pour vous montrer des publicités plus pertinentes. Des publicités s'affichent même si vous refusez."),
            ("ru", "Moon Pull использует это, чтобы показывать более подходящую вам рекламу. Реклама будет показываться, даже если вы откажетесь."),
            ("ja", "Moon Pull はより関連性の高い広告を表示するためにこれを使用します。拒否しても広告は表示されます。")
        };

        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string builtPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            StampInfoPlist(builtPath);
            WriteAppIcon(builtPath);
            WritePrivacyManifest(builtPath);
            WriteLocalizations(builtPath);
        }

        private static void StampInfoPlist(string builtPath)
        {
            string plistPath = Path.Combine(builtPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("CFBundleIconName", AppIconName);
            plist.root.SetBoolean("UIRequiresFullScreen", true);
            plist.root.SetString("CFBundleVersion", MoonPullBuild.MinutesSince2024().ToString());
            PlistElementArray languages = plist.root.CreateArray("CFBundleLocalizations");
            foreach (var localization in Localizations)
            {
                languages.AddString(localization.Code);
            }

            plist.WriteToFile(plistPath);
        }

        private static void WriteLocalizations(string builtPath)
        {
            string pbxPath = PBXProject.GetPBXProjectPath(builtPath);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);
            string targetGuid = project.GetUnityMainTargetGuid();

            foreach (var localization in Localizations)
            {
                string relative = "Unity-iPhone/" + localization.Code + ".lproj";
                string folder = Path.Combine(builtPath, relative);
                Directory.CreateDirectory(folder);
                string tracking = localization.Tracking.Replace("\\", "\\\\").Replace("\"", "\\\"");
                File.WriteAllText(Path.Combine(folder, "InfoPlist.strings"),
                    "\"NSUserTrackingUsageDescription\" = \"" + tracking + "\";\n", new UTF8Encoding(false));
                if (!project.ContainsFileByProjectPath(relative))
                {
                    project.AddFileToBuild(targetGuid, project.AddFolderReference(relative, relative));
                }
            }

            project.WriteToFile(pbxPath);
        }

        private static void WriteAppIcon(string builtPath)
        {
            string appIconSet = Path.Combine(builtPath, "Unity-iPhone/Images.xcassets/" + AppIconName + ".appiconset");
            Directory.CreateDirectory(appIconSet);
            foreach (string existing in Directory.GetFiles(appIconSet))
            {
                File.Delete(existing);
            }

            Texture2D icon = Art.DrawIcon();
            File.WriteAllBytes(Path.Combine(appIconSet, "Icon-1024.png"), icon.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(icon);
            File.WriteAllText(Path.Combine(appIconSet, "Contents.json"), ContentsJson);

            string pbxPath = PBXProject.GetPBXProjectPath(builtPath);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);
            string targetGuid = project.GetUnityMainTargetGuid();
            project.SetBuildProperty(targetGuid, "ASSETCATALOG_COMPILER_APPICON_NAME", AppIconName);
            project.SetBuildProperty(targetGuid, "ASSETCATALOG_COMPILER_INCLUDE_ALL_APPICON_ASSETS", "YES");
            project.WriteToFile(pbxPath);
        }

        // App Store requires a privacy manifest: required-reason APIs the game uses (PlayerPrefs = UserDefaults) and
        // what it collects. Ads/analytics SDKs ship their own manifests for their parts.
        private const string PrivacyManifest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
  <key>NSPrivacyTracking</key><true/>
  <key>NSPrivacyTrackingDomains</key><array/>
  <key>NSPrivacyCollectedDataTypes</key>
  <array>
    <dict>
      <key>NSPrivacyCollectedDataType</key><string>NSPrivacyCollectedDataTypeDeviceID</string>
      <key>NSPrivacyCollectedDataTypeLinked</key><false/>
      <key>NSPrivacyCollectedDataTypeTracking</key><true/>
      <key>NSPrivacyCollectedDataTypePurposes</key><array><string>NSPrivacyCollectedDataTypePurposeThirdPartyAdvertising</string></array>
    </dict>
    <dict>
      <key>NSPrivacyCollectedDataType</key><string>NSPrivacyCollectedDataTypeProductInteraction</string>
      <key>NSPrivacyCollectedDataTypeLinked</key><false/>
      <key>NSPrivacyCollectedDataTypeTracking</key><false/>
      <key>NSPrivacyCollectedDataTypePurposes</key><array><string>NSPrivacyCollectedDataTypePurposeAnalytics</string></array>
    </dict>
    <dict>
      <key>NSPrivacyCollectedDataType</key><string>NSPrivacyCollectedDataTypeCrashData</string>
      <key>NSPrivacyCollectedDataTypeLinked</key><false/>
      <key>NSPrivacyCollectedDataTypeTracking</key><false/>
      <key>NSPrivacyCollectedDataTypePurposes</key><array><string>NSPrivacyCollectedDataTypePurposeAppFunctionality</string></array>
    </dict>
  </array>
  <key>NSPrivacyAccessedAPITypes</key>
  <array>
    <dict>
      <key>NSPrivacyAccessedAPIType</key><string>NSPrivacyAccessedAPICategoryUserDefaults</string>
      <key>NSPrivacyAccessedAPITypeReasons</key><array><string>CA92.1</string></array>
    </dict>
  </array>
</dict>
</plist>
";

        private static void WritePrivacyManifest(string builtPath)
        {
            const string relative = "Unity-iPhone/PrivacyInfo.xcprivacy";
            File.WriteAllText(Path.Combine(builtPath, relative), PrivacyManifest, new UTF8Encoding(false));
            string pbxPath = PBXProject.GetPBXProjectPath(builtPath);
            var project = new PBXProject();
            project.ReadFromFile(pbxPath);
            string target = project.GetUnityMainTargetGuid();
            if (!project.ContainsFileByProjectPath(relative))
            {
                string guid = project.AddFile(relative, relative);
                project.AddFileToBuild(target, guid);
            }

            project.WriteToFile(pbxPath);
        }
    }
}
#endif
