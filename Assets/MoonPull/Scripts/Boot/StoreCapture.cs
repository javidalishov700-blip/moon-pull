#if MOONPULL_CAPTURE
using System.Collections;
using System.IO;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>
    /// Capture-only player (never shipped): screenshots the main menu, starts the next level and screenshots
    /// gameplay, then quits. Built by .github/workflows/store-capture.yml.
    /// </summary>
    public sealed class StoreCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("StoreCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<StoreCapture>();
        }

        private IEnumerator Start()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "capture");
            Directory.CreateDirectory(folder);

            yield return new WaitForSecondsRealtime(5f);
            yield return Shot(folder, "01-menu");

            MetaGame meta = FindFirstObjectByType<MetaGame>();
            if (meta != null)
            {
                meta.PlayNext();
            }

            // Surf like a player: dive on the way down, release to fly off the next crest, and grab frames in between.
            var rescue = FindFirstObjectByType<MoonPull.Rescue.NightRescue>();
            float[] times = { 2.5f, 3f, 3f, 3.5f, 4f };
            for (int i = 0; i < times.Length; i++)
            {
                float until = Time.realtimeSinceStartup + times[i];
                while (Time.realtimeSinceStartup < until)
                {
                    if (rescue != null)
                    {
                        rescue.ForcedHold = Mathf.Repeat(Time.realtimeSinceStartup, 1.3f) < 0.65f;
                    }

                    yield return null;
                }

                if (rescue != null)
                {
                    rescue.ForcedHold = false;
                }

                yield return Shot(folder, "0" + (i + 2) + "-play");
            }

            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(0);
        }

        private static IEnumerator Shot(string folder, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            Debug.Log("[MoonPull] Captured " + name);
            yield return null;
        }
    }
}
#endif
