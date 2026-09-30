#if MOONPULL_CAPTURE
using System.Collections;
using System.IO;
using MoonPull.Meta;
using MoonPull.Rescue;
using MoonPull.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.Boot
{
    /// <summary>
    /// Capture-only player (never shipped), built by .github/workflows/store-capture.yml. Screenshots every screen
    /// (daily reward, menu, the three Village tabs, exploring, a night's start, middle and a rock crash, the pause
    /// menu and the results), then runs the 20-night <see cref="PlaytestBot"/> and screenshots the village it built.
    /// </summary>
    public sealed class StoreCapture : MonoBehaviour
    {
        private string folder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Application.logMessageReceived += PlaytestBot.OnLog;
            var go = new GameObject("StoreCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<StoreCapture>();
        }

        private IEnumerator Start()
        {
            folder = Path.Combine(Directory.GetCurrentDirectory(), "capture");
            Directory.CreateDirectory(folder);

            yield return new WaitForSecondsRealtime(5f);
            var popups = FindFirstObjectByType<PopupManager>(FindObjectsInactive.Include);
            if (popups != null && popups.IsShowing)
            {
                yield return Shot("00-daily");
                popups.CloseAll();
                yield return new WaitForSecondsRealtime(1f);
            }

            yield return Shot("01-menu");

            // A lived-in village for the store shots (the bot resets it later).
            PlayerPrefs.SetInt(NightRescue.VillageKey, 16);
            PlayerPrefs.SetInt("mp_village_level", 3);
            PlayerPrefs.SetInt("mp_village_shelter", 1);
            PlayerPrefs.SetInt("mp_village_restaurant", 2);
            PlayerPrefs.SetInt("mp_village_market", 1);
            TycoonState.SeedForCapture(1, 640f);
            var village = FindFirstObjectByType<VillagePopup>(FindObjectsInactive.Include);
            if (popups != null && village != null)
            {
                popups.Open(village);
                yield return new WaitForSecondsRealtime(4f);
                yield return Shot("02-village-buildings");
                yield return Press(village.transform, "Content/TabIslands", 0.8f);
                yield return Shot("03-village-islands");
                yield return Press(village.transform, "Content/TabBoat", 0.8f);
                yield return Shot("04-village-boat");
                yield return Press(village.transform, "Explore", 2.5f);
                yield return Shot("05-village-explore");
                popups.Close(village);
                yield return new WaitForSecondsRealtime(1.5f);
            }

            MetaGame meta = FindFirstObjectByType<MetaGame>();
            var rescue = FindFirstObjectByType<NightRescue>();
            if (meta != null && rescue != null)
            {
                meta.PlayNext();
                rescue.AutoPilotSkill = 1f;
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Shot("06-play-start");
                yield return new WaitForSecondsRealtime(9f);
                yield return Shot("07-play-mid");

                // Let the next rock hit, to show the crash.
                rescue.AutoPilotSkill = 0f;
                int hits = rescue.Stats.RocksHit;
                float until = Time.realtimeSinceStartup + 25f;
                while (rescue.Stats.RocksHit == hits && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                }

                yield return null;
                yield return Shot("08-play-crash");
                rescue.AutoPilotSkill = 1f;
                yield return new WaitForSecondsRealtime(2f);

                var hud = FindFirstObjectByType<HudScreen>(FindObjectsInactive.Include);
                var pause = FindFirstObjectByType<PausePopup>(FindObjectsInactive.Include);
                if (hud != null && pause != null)
                {
                    yield return Press(hud.transform, "Content/Pause", 1f);
                    yield return Shot("09-pause");
                    yield return Press(pause.transform, "Content/Resume", 0.5f);
                }

                rescue.AutoPilotSkill = -1f;
                rescue.EndNightNow();
                yield return new WaitForSecondsRealtime(3f);
                yield return Shot("10-win");
            }

            yield return PlaytestBot.Run(folder);
            yield return new WaitForSecondsRealtime(2f);
            if (popups != null && village != null)
            {
                popups.CloseAll();
                popups.Open(village);
                yield return new WaitForSecondsRealtime(4f);
                yield return Shot("11-village-after-bot");
            }

            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(0);
        }

        private static IEnumerator Press(Transform root, string path, float wait)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                Debug.LogError("[MoonPull] Capture: missing " + root.name + "/" + path);
                yield break;
            }

            target.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(wait);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            Debug.Log("[MoonPull] Captured " + name);
            yield return null;
        }
    }
}
#endif
