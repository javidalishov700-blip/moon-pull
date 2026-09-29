using UnityEngine;

namespace MoonPull.Analytics
{
    /// <summary>
    /// Reads IAB TCF v2 purpose consents written by UMP and maps them to Google Consent Mode v2:
    /// ad_storage ← P1, ad_user_data ← P1+P7, ad_personalization ← P3+P4, analytics_storage ← P1.
    /// No TCF string means GDPR does not apply, so everything is granted.
    /// </summary>
    public static class TcfConsentReader
    {
        private const string PurposeConsentsKey = "IABTCF_PurposeConsents";
        private const string GdprAppliesKey = "IABTCF_gdprApplies";

        public static AnalyticsConsent Read()
        {
            ReadRaw(out int gdprApplies, out string purposes);
            return FromTcf(gdprApplies, purposes);
        }

        /// <summary>Pure mapping, unit-testable.</summary>
        public static AnalyticsConsent FromTcf(int gdprApplies, string purposeConsents)
        {
            if (gdprApplies != 1)
            {
                return AnalyticsConsent.AllGranted;
            }

            if (string.IsNullOrEmpty(purposeConsents))
            {
                return AnalyticsConsent.AllDenied;
            }

            bool p1 = Has(purposeConsents, 1);
            bool p3 = Has(purposeConsents, 3);
            bool p4 = Has(purposeConsents, 4);
            bool p7 = Has(purposeConsents, 7);
            return new AnalyticsConsent(p1, p1, p1 && p7, p3 && p4);
        }

        private static bool Has(string purposes, int purpose) => purposes.Length >= purpose && purposes[purpose - 1] == '1';

        private static void ReadRaw(out int gdprApplies, out string purposes)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // UMP writes TCF keys to the default SharedPreferences, which Unity's PlayerPrefs file does not cover.
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var manager = new AndroidJavaClass("android.preference.PreferenceManager"))
            using (AndroidJavaObject prefs = manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity))
            {
                gdprApplies = prefs.Call<int>("getInt", GdprAppliesKey, 0);
                purposes = prefs.Call<string>("getString", PurposeConsentsKey, string.Empty);
            }
#else
            // On iOS PlayerPrefs is NSUserDefaults, where UMP stores the same keys.
            gdprApplies = PlayerPrefs.GetInt(GdprAppliesKey, 0);
            purposes = PlayerPrefs.GetString(PurposeConsentsKey, string.Empty);
#endif
        }
    }
}
