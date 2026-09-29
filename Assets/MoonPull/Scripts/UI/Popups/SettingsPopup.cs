using System.Collections.Generic;
using MoonPull.Consent;
using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Settings;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class SettingsPopup : UIPopup
    {
        [SerializeField] private ToggleButton musicToggle;
        [SerializeField] private ToggleButton soundToggle;
        [SerializeField] private ToggleButton hapticsToggle;
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private Button privacyOptionsButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private LocalizedText versionLabel;
        [SerializeField] private string privacyPolicyUrl = "https://example.com/moonpull/privacy";

        private readonly List<string> codes = new List<string>(8);
        private bool binding;

        protected override void Awake()
        {
            base.Awake();
            musicToggle.Toggled += on => Settings.SetMusic(on);
            soundToggle.Toggled += on => Settings.SetSfx(on);
            hapticsToggle.Toggled += on => Settings.SetHaptics(on);
            languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
            privacyOptionsButton.onClick.AddListener(() => Services.Get<IConsentService>().ShowPrivacyOptions(Refresh));
            privacyPolicyButton.onClick.AddListener(() => Application.OpenURL(privacyPolicyUrl));
        }

        private static SettingsService Settings => Services.Get<SettingsService>();

        protected override void OnShown() => Refresh();

        private void Refresh()
        {
            SettingsService settings = Settings;
            musicToggle.SetState(settings.MusicOn);
            soundToggle.SetState(settings.SfxOn);
            hapticsToggle.SetState(settings.HapticsOn);
            privacyOptionsButton.gameObject.SetActive(Services.TryGet(out IConsentService consent) && consent.PrivacyOptionsRequired);
            versionLabel.SetKey(LocKeys.SettingsVersion, Application.version);

            binding = true;
            ILocalizationService localization = Services.Get<ILocalizationService>();
            codes.Clear();
            languageDropdown.ClearOptions();
            var options = new List<string>(8);
            int selected = 0;
            for (int i = 0; i < localization.SupportedCodes.Count; i++)
            {
                string code = localization.SupportedCodes[i];
                codes.Add(code);
                options.Add(localization.DisplayName(code));
                if (code == localization.CurrentCode)
                {
                    selected = i;
                }
            }

            languageDropdown.AddOptions(options);
            languageDropdown.SetValueWithoutNotify(selected);
            binding = false;
        }

        private void OnLanguageSelected(int index)
        {
            if (!binding && index >= 0 && index < codes.Count)
            {
                Settings.SetLanguage(codes[index]);
            }
        }
    }
}
