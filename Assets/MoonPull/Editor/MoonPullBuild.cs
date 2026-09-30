// Headless build entry points for CI (GitHub Actions / Codemagic) and the Unity menu.
//   Unity -batchmode -quit -nographics -projectPath . -buildTarget iOS \
//         -executeMethod MoonPull.EditorTools.MoonPullBuild.BuildIos
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoonPull.EditorTools
{
    public static class MoonPullBuild
    {
        public const string DefaultBundleId = "com.javidalishov.moonpull";
        public const string ProductName = "Moon Pull";
        public const string IconPath = Gen.Root + "/Icon/AppIcon.png";

        private const string AdsPod = "Google-Mobile-Ads-SDK";
        private const string AdsBridge = "unity-plugin-library";

        private static bool generated;

        [MenuItem("Moon Pull/Generate Content and Scene")]
        public static void GenerateMenu()
        {
            generated = false;
            if (!Generate())
            {
                EditorUtility.DisplayDialog("Moon Pull", "Generation reported errors — see the Console.", "OK");
            }
        }

        [MenuItem("Moon Pull/Build iOS Xcode Project")]
        public static void BuildIos() => Run(BuildTarget.iOS, BuildTargetGroup.iOS, CommandLineArgument("-customBuildPath", "ios"));

        [MenuItem("Moon Pull/Build Android APK")]
        public static void BuildAndroid()
        {
            string custom = CommandLineArgument("-customBuildPath", null);
            Run(BuildTarget.Android, BuildTargetGroup.Android, string.IsNullOrEmpty(custom) ? "build/android/moonpull.apk" : custom);
        }

        /// <summary>
        /// Rebuilds every generated asset and the scene once per editor session. Returns false when any
        /// serialized field failed to wire, so CI fails loudly instead of shipping a half-wired game.
        /// </summary>
        public static bool Generate()
        {
            if (generated)
            {
                return Gen.Errors.Count == 0;
            }

            generated = true;
            Gen.ResetErrors();
            Art.ResetCache();
            Meshes.ResetCache();
            Art.EnsureIcon(IconPath);
            Content content = Content.Build();
            string scenePath = SceneFactory.Build(content);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            VerifyScene(scenePath);

            foreach (string problem in Gen.Errors)
            {
                Debug.LogError("[MoonPull] Generator: " + problem);
            }

            Debug.Log($"[MoonPull] Generated {scenePath} with {Gen.Errors.Count} problem(s).");
            return Gen.Errors.Count == 0;
        }

        /// <summary>
        /// Linux player that screenshots the menu and gameplay (Boot/StoreCapture.cs). Mono, no ads or tracking
        /// defines; built and run by .github/workflows/store-capture.yml, never shipped.
        /// </summary>
        public static void BuildCaptureLinux()
        {
            capturing = true;
            if (!Generate())
            {
                Finish(false, "content generation reported errors");
                return;
            }

            NamedBuildTarget named = NamedBuildTarget.Standalone;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingDefineSymbols(named, "MOONPULL_CAPTURE");
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1170; // iPhone 13-15 portrait
            PlayerSettings.defaultScreenHeight = 2532;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore });
            ApplySplashSettings();
            IncludeShaders();

            string custom = CommandLineArgument("-customBuildPath", null);
            string folder = string.IsNullOrEmpty(custom) ? "build/capture" : Path.GetDirectoryName(custom);
            Directory.CreateDirectory(folder);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EditorBuildSettings.scenes[0].path },
                locationPathName = Path.Combine(folder, "MoonPull.x86_64"),
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            Finish(report.summary.result == BuildResult.Succeeded, "the capture build failed");
        }

        private static bool capturing;

        /// <summary>
        /// Reopens the saved scene from disk and reports every object reference that did not survive the save,
        /// so wiring that looks right in memory but is lost on disk fails the build.
        /// </summary>
        private static void VerifyScene(string scenePath)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            int checkedRefs = 0;
            int nullRefs = 0;
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string ns = behaviour != null ? behaviour.GetType().Namespace : null;
                if (ns == null || !ns.StartsWith("MoonPull"))
                {
                    continue;
                }

                var so = new SerializedObject(behaviour);
                SerializedProperty property = so.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script")
                    {
                        continue;
                    }

                    checkedRefs++;
                    if (property.objectReferenceValue == null && (property.name == "config" || property.objectReferenceInstanceIDValue != 0))
                    {
                        nullRefs++;
                        if (nullRefs <= 40)
                        {
                            Debug.LogError($"[MoonPull] Verify: {behaviour.GetType().Name} on '{behaviour.name}' has null {property.propertyPath} (id {property.objectReferenceInstanceIDValue})");
                        }
                    }
                }
            }

            string probe = Gen.Root + "/Config/AdConfig.asset";
            Debug.Log($"[MoonPull] Verify: {checkedRefs} references checked, {nullRefs} null. {probe} exists={System.IO.File.Exists(probe)} loads={AssetDatabase.LoadMainAssetAtPath(probe) != null}");
            if (nullRefs > 0)
            {
                Gen.Error($"{nullRefs} scene reference(s) were lost when the scene was saved");
            }
        }

        /// <summary>Called by the build preprocessor so any build path gets a correct player.</summary>
        public static void PrepareProject()
        {
            Generate();
            if (capturing)
            {
                return;
            }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            ApplyPlayerSettings(target, BuildPipeline.GetBuildTargetGroup(target));
        }

        private static void Run(BuildTarget target, BuildTargetGroup group, string outputPath)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                // Post-process hooks live behind #if UNITY_IOS and do not exist until scripts recompile.
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                Debug.LogWarning($"[MoonPull] Active platform switched to {target}. Run the build again after recompiling.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(2);
                }

                return;
            }

            // Defines first: the generator and the player both compile against them.
            ApplyPlayerSettings(target, group);

            if (!Generate())
            {
                Finish(false, "content generation reported errors");
                return;
            }

            if (target == BuildTarget.iOS && Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            string directory = target == BuildTarget.iOS ? outputPath : Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (target == BuildTarget.iOS)
            {
                LoadPodDependencies();
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { EditorBuildSettings.scenes[0].path },
                locationPathName = outputPath,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            });

            BuildSummary summary = report.summary;
            Debug.Log($"[MoonPull] Build {summary.result}: {summary.totalSize} bytes in {summary.totalTime}.");
            if (summary.result != BuildResult.Succeeded)
            {
                Finish(false, "the player build failed");
                return;
            }

            string problem = target == BuildTarget.iOS ? FindAdsLinkProblem(outputPath) : null;
            Finish(problem == null, problem == null ? null : "the Xcode project cannot link AdMob: " + problem);
        }

        private static void Finish(bool succeeded, string reason)
        {
            if (!succeeded)
            {
                Debug.LogError("[MoonPull] Build stopped: " + reason);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Moon Pull", "Build stopped: " + reason, "OK");
                }
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(succeeded ? 0 : 1);
            }
        }

        /// <summary>
        /// Makes EDM4U read every *Dependencies.xml before a batch-mode build; otherwise the Podfile comes out empty
        /// and the app links without the AdMob SDK.
        /// </summary>
        private static void LoadPodDependencies()
        {
            Type resolver = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                resolver = assembly.GetType("Google.IOSResolver", false);
                if (resolver != null)
                {
                    break;
                }
            }

            MethodInfo refresh = resolver?.GetMethod("RefreshXmlDependencies", BindingFlags.NonPublic | BindingFlags.Static);
            if (refresh == null)
            {
                Debug.LogWarning("[MoonPull] Could not ask the External Dependency Manager to load its pods.");
                return;
            }

            refresh.Invoke(null, null);
        }

        private static string FindAdsLinkProblem(string xcodeProject)
        {
            string podfile = Path.Combine(xcodeProject, "Podfile");
            if (!File.Exists(podfile) || !File.ReadAllText(podfile).Contains(AdsPod))
            {
                return $"its Podfile does not list {AdsPod}";
            }

            string pbxproj = Path.Combine(xcodeProject, "Unity-iPhone.xcodeproj", "project.pbxproj");
            if (!File.Exists(pbxproj) || !File.ReadAllText(pbxproj).Contains(AdsBridge))
            {
                return $"it does not contain {AdsBridge} from Assets/Plugins/iOS";
            }

            return null;
        }

        /// <summary>The repository carries no trusted ProjectSettings state, so every player setting is applied here.</summary>
        public static void ApplyPlayerSettings(BuildTarget target, BuildTargetGroup group)
        {
            NamedBuildTarget named = NamedBuildTarget.FromBuildTargetGroup(group);

            PlayerSettings.companyName = EnvOr("COMPANY_NAME", "Javid Alishov");
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(named, EnvOr("BUNDLE_ID", DefaultBundleId));
            PlayerSettings.bundleVersion = EnvOr("APP_VERSION", "1.0.0");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.stripEngineCode = false;

            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(named, ManagedStrippingLevel.Minimal);
            QualitySettings.vSyncCount = 0;

            AddScriptingDefine(named, "MOONPULL_ADMOB");
            AddScriptingDefine(named, "MOONPULL_ATT");

            ApplySplashSettings();
            IncludeShaders();

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }

            if (target == BuildTarget.Android)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                PlayerSettings.Android.bundleVersionCode = MinutesSince2024();
                return;
            }

            if (target != BuildTarget.iOS)
            {
                return;
            }

            PlayerSettings.iOS.targetOSVersionString = EnvOr("IOS_MIN_VERSION", "13.0");
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.buildNumber = MinutesSince2024().ToString();
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.hideHomeButton = true;
            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.Default);

            string teamId = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
            if (!string.IsNullOrEmpty(teamId))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = teamId;
            }
        }

        public static void ApplySplashSettings()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.04f, 0.06f, 0.15f);
        }

        /// <summary>Custom shaders are only referenced by generated materials; pin them so stripping never drops one.</summary>
        private static void IncludeShaders()
        {
            var graphics = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset"));
            SerializedProperty list = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                return;
            }

            foreach (string name in new[] { "MoonPull/Water", "MoonPull/Flat", "MoonPull/Sky", "MoonPull/Moon", "MoonPull/Glow", "MoonPull/Post", "UI/Default", "Sprites/Default" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning("[MoonPull] Shader not found: " + name);
                    continue;
                }

                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    present |= list.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                }

                if (!present)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                }
            }

            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Monotonic build number: App Store Connect rejects a repeated one and CI counters restart.</summary>
        public static int MinutesSince2024() => (int)(DateTime.UtcNow - new DateTime(2024, 1, 1)).TotalMinutes;

        private static void AddScriptingDefine(NamedBuildTarget named, string define)
        {
            string existing = PlayerSettings.GetScriptingDefineSymbols(named);
            string[] symbols = existing.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (Array.IndexOf(symbols, define) < 0)
            {
                PlayerSettings.SetScriptingDefineSymbols(named, existing.Length > 0 ? existing + ";" + define : define);
            }
        }

        private static string EnvOr(string key, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private static string CommandLineArgument(string flag, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag && !string.IsNullOrEmpty(args[i + 1]))
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
