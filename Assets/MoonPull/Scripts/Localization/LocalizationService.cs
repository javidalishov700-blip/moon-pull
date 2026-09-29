using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MoonPull.Localization
{
    public interface ILocalizationService
    {
        string CurrentCode { get; }
        IReadOnlyList<string> SupportedCodes { get; }
        event Action LanguageChanged;

        /// <summary>Localized string for <paramref name="key"/>; returns the key itself if missing, so gaps are visible in QA.</summary>
        string Get(string key);

        string Format(string key, params object[] args);

        /// <summary>Formats a number with the current locale's separators (1,250 vs 1.250).</summary>
        string Number(long value);

        string DisplayName(string code);

        void SetLanguage(string code);
    }

    /// <summary>Unity Localization backend. All UI text goes through the "UI" string table.</summary>
    public sealed class UnityLocalizationService : ILocalizationService
    {
        public const string Table = "UI";

        private static readonly string[] Codes = { "en", "tr", "es", "pt-BR", "de", "fr", "ru", "ja" };

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "en", "English" },
            { "tr", "Türkçe" },
            { "es", "Español" },
            { "pt-BR", "Português (Brasil)" },
            { "de", "Deutsch" },
            { "fr", "Français" },
            { "ru", "Русский" },
            { "ja", "日本語" }
        };

        public event Action LanguageChanged;

        public string CurrentCode => LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale.Identifier.Code : "en";

        public IReadOnlyList<string> SupportedCodes => Codes;

        private CultureInfo Culture =>
            LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale.Identifier.CultureInfo ?? CultureInfo.InvariantCulture : CultureInfo.InvariantCulture;

        /// <summary>Waits for the localization system, then applies the saved language (empty = device language).</summary>
        public IEnumerator Initialize(string savedCode)
        {
            yield return LocalizationSettings.InitializationOperation;
            if (!string.IsNullOrEmpty(savedCode))
            {
                SetLanguage(savedCode);
            }

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public string Get(string key)
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            return string.IsNullOrEmpty(value) ? key : value;
        }

        public string Format(string key, params object[] args)
        {
            try
            {
                return string.Format(Culture, Get(key), args);
            }
            catch (FormatException)
            {
                return Get(key);
            }
        }

        public string Number(long value) => value.ToString("N0", Culture);

        public string DisplayName(string code) => Names.TryGetValue(code, out string name) ? name : code;

        public void SetLanguage(string code)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale != null && locale != LocalizationSettings.SelectedLocale)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        private void OnLocaleChanged(Locale locale) => LanguageChanged?.Invoke();
    }
}
