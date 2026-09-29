using System;
using MoonPull.Ads;
using UnityEngine;

namespace MoonPull.Config
{
    /// <summary>
    /// The one place real ad unit IDs live. Debug builds and the Editor always use Google's official test IDs, so a
    /// dev build can never generate invalid traffic on the live account.
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "MoonPull/Config/Ads")]
    public sealed class AdConfig : ScriptableObject
    {
        [Serializable]
        public struct UnitIds
        {
            public string Rewarded;
            public string Interstitial;
            public string AppOpen;
            public string Banner;
        }

        [Header("Production ad unit IDs")]
        [SerializeField] private UnitIds androidProduction;
        [SerializeField] private UnitIds iosProduction;

        [Header("Google official test IDs (do not edit)")]
        [SerializeField] private UnitIds androidTest = new UnitIds
        {
            Rewarded = "ca-app-pub-3940256099942544/5224354917",
            Interstitial = "ca-app-pub-3940256099942544/1033173712",
            AppOpen = "ca-app-pub-3940256099942544/9257395921",
            Banner = "ca-app-pub-3940256099942544/9214589741"
        };

        [SerializeField] private UnitIds iosTest = new UnitIds
        {
            Rewarded = "ca-app-pub-3940256099942544/1712485313",
            Interstitial = "ca-app-pub-3940256099942544/4411468910",
            AppOpen = "ca-app-pub-3940256099942544/5575463023",
            Banner = "ca-app-pub-3940256099942544/2435281174"
        };

        [Header("Build selection")]
        [Tooltip("Force test ads even in release builds (internal testing tracks).")]
        [SerializeField] private bool forceTestAds;
        [Tooltip("Hashed device IDs from logcat/Xcode console. Real ads on these devices are served as test ads.")]
        [SerializeField] private string[] testDeviceIds = new string[0];

        [Header("Loading")]
        [Tooltip("Seconds between retries after a failed load; the last value repeats.")]
        [SerializeField] private float[] retryDelays = { 2f, 4f, 8f, 16f, 32f };
        [Tooltip("App open ads expire after 4 hours per AdMob guidance.")]
        [SerializeField, Min(0.1f)] private float appOpenExpiryHours = 4f;

        [Header("Frequency rules (Remote Config can override)")]
        [Tooltip("1-based level from which interstitials may appear.")]
        [SerializeField, Min(1)] private int interstitialStartLevel = 4;
        [SerializeField, Min(1)] private int interstitialEveryLevels = 2;
        [SerializeField, Min(0f)] private float fullscreenCooldownSeconds = 45f;
        [Tooltip("No interstitials during the player's first N seconds of lifetime playtime.")]
        [SerializeField, Min(0f)] private float lifetimeGraceSeconds = 180f;
        [SerializeField, Min(0f)] private float appOpenMinBackgroundSeconds = 30f;
        [Tooltip("Ignore resumes this soon after our own full-screen ad closed (Android fires a resume when the ad activity ends).")]
        [SerializeField, Min(0f)] private float appOpenIgnoreAfterAdSeconds = 5f;

        [Header("Editor mock")]
        [SerializeField, Min(0f)] private float mockLoadSeconds = 1f;
        [SerializeField, Range(0f, 1f)] private float mockFillRate = 1f;

        public static bool IsDevelopmentBuild => Application.isEditor || Debug.isDebugBuild;

        public bool UseTestAds => forceTestAds || IsDevelopmentBuild;
        public string[] TestDeviceIds => testDeviceIds;
        public float AppOpenExpiryHours => appOpenExpiryHours;
        public float MockLoadSeconds => mockLoadSeconds;
        public float MockFillRate => mockFillRate;
        public float AppOpenIgnoreAfterAdSeconds => appOpenIgnoreAfterAdSeconds;

        public AdRulesSettings DefaultRules => new AdRulesSettings(
            interstitialStartLevel, interstitialEveryLevels, fullscreenCooldownSeconds, lifetimeGraceSeconds, appOpenMinBackgroundSeconds);

        public float RetryDelay(int attempt)
        {
            if (retryDelays.Length == 0)
            {
                return 30f;
            }

            return retryDelays[Mathf.Clamp(attempt, 0, retryDelays.Length - 1)];
        }

        public string UnitId(AdFormat format)
        {
#if UNITY_IOS
            UnitIds ids = UseTestAds ? iosTest : iosProduction;
#else
            UnitIds ids = UseTestAds ? androidTest : androidProduction;
#endif
            switch (format)
            {
                case AdFormat.Rewarded: return ids.Rewarded;
                case AdFormat.Interstitial: return ids.Interstitial;
                case AdFormat.AppOpen: return ids.AppOpen;
                default: return ids.Banner;
            }
        }
    }
}
