using System.Collections;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>Loads the save, registers <see cref="ISaveService"/>, and flushes it whenever the app is backgrounded.</summary>
    public sealed class SaveBootStep : BootStep
    {
        [SerializeField] private LevelGenConfig generation;
        [SerializeField] private RegionCatalog regions;

        private SaveService save;

        public override IEnumerator Run()
        {
            save = new SaveService(new PlayerPrefsStorage(), generation.TotalLevels, regions.Count);
            save.CorruptionDetected += message => Debug.LogWarning($"[Save] {message}");
            save.Load();

            AdsStateData ads = save.Data.Ads;
            if (ads.FirstLaunchUtcTicks <= 0)
            {
                ads.FirstLaunchUtcTicks = Services.Get<IClock>().UtcNow.Ticks;
            }

            ads.SessionCount++;
            save.SaveNow();
            Services.Register<ISaveService>(save);
            yield break;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                save?.SaveIfDirty();
            }
        }

        private void OnApplicationQuit()
        {
            save?.SaveIfDirty();
        }
    }
}
