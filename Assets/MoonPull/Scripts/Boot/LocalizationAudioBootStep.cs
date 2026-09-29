using System.Collections;
using MoonPull.Audio;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Haptics;
using MoonPull.Localization;
using MoonPull.Save;
using MoonPull.Settings;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>Registers localization, audio, haptics and settings, and applies saved preferences before any UI shows.</summary>
    public sealed class LocalizationAudioBootStep : BootStep
    {
        [SerializeField] private AudioService audioService;

        public override IEnumerator Run()
        {
            ISaveService save = Services.Get<ISaveService>();

            var localization = new UnityLocalizationService();
            yield return localization.Initialize(save.Data.Settings.Language);
            Services.Register<ILocalizationService>(localization);

            Services.Register<IAudioService>(audioService);
            var haptics = new NativeHapticsService();
            Services.Register<IHapticsService>(haptics);

            var settings = new SettingsService(save, audioService, haptics, localization);
            settings.ApplyAll();
            Services.Register(settings);

            LocalizedText.RefreshAllActive();
        }
    }
}
