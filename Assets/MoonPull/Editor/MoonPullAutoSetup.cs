using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Runs for every player build, including ones a CI service starts through the default pipeline, so the
    /// generated scene and player settings are always in place.
    /// </summary>
    public sealed class MoonPullBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            MoonPullBuild.PrepareProject();
            if (Gen.Errors.Count > 0)
            {
                throw new BuildFailedException($"Moon Pull content generation reported {Gen.Errors.Count} error(s).");
            }
        }
    }

    /// <summary>On first editor load, generate the scene so opening the project locally just works.</summary>
    [InitializeOnLoad]
    public static class MoonPullAutoSetup
    {
        static MoonPullAutoSetup()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            try
            {
                MoonPullBuild.ApplySplashSettings();
                if (!System.IO.File.Exists(Gen.Root + "/Main.unity"))
                {
                    MoonPullBuild.Generate();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[MoonPull] Auto setup skipped: " + exception.Message);
            }
        }
    }
}
