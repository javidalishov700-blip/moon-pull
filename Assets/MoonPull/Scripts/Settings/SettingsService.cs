using System;
using MoonPull.Localization;
using MoonPull.Save;

namespace MoonPull.Settings
{
    /// <summary>Persists player settings and pushes them to audio, haptics and localization.</summary>
    public sealed class SettingsService
    {
        private readonly ISaveService save;
        private readonly Audio.IAudioService audio;
        private readonly Haptics.IHapticsService haptics;
        private readonly ILocalizationService localization;

        public SettingsService(ISaveService save, Audio.IAudioService audio, Haptics.IHapticsService haptics, ILocalizationService localization)
        {
            this.save = save;
            this.audio = audio;
            this.haptics = haptics;
            this.localization = localization;
        }

        public event Action Changed;

        private SettingsData Data => save.Data.Settings;

        public bool MusicOn => Data.MusicOn;
        public bool SfxOn => Data.SfxOn;
        public bool HapticsOn => Data.HapticsOn;
        public string Language => localization.CurrentCode;

        public void ApplyAll()
        {
            audio.SetMusicEnabled(Data.MusicOn);
            audio.SetSfxEnabled(Data.SfxOn);
            haptics.Enabled = Data.HapticsOn;
        }

        public void SetMusic(bool on)
        {
            Data.MusicOn = on;
            audio.SetMusicEnabled(on);
            Commit();
        }

        public void SetSfx(bool on)
        {
            Data.SfxOn = on;
            audio.SetSfxEnabled(on);
            Commit();
        }

        public void SetHaptics(bool on)
        {
            Data.HapticsOn = on;
            haptics.Enabled = on;
            if (on)
            {
                haptics.Play(Haptics.HapticType.Selection);
            }

            Commit();
        }

        public void SetLanguage(string code)
        {
            Data.Language = code;
            localization.SetLanguage(code);
            Commit();
        }

        private void Commit()
        {
            save.MarkDirty();
            Changed?.Invoke();
        }
    }
}
