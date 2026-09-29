using System.Collections;
using System.Collections.Generic;
using MoonPull.Analytics;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Boot;
using UnityEngine;
#if MOONPULL_FIREBASE_ANALYTICS || MOONPULL_FIREBASE_CRASHLYTICS || MOONPULL_FIREBASE_REMOTE_CONFIG
using Firebase;
using Firebase.Extensions;
#endif
#if MOONPULL_FIREBASE_REMOTE_CONFIG
using Firebase.RemoteConfig;
#endif

namespace MoonPull.Boot
{
    /// <summary>
    /// Initializes Firebase (Crashlytics as early as possible), registers analytics/crash/remote-config services and
    /// fetches Remote Config with local defaults. Analytics stays in denied consent mode until ConsentBootStep resolves.
    /// Give this step a short timeout (≈4 s): a slow network must never block the first session.
    /// </summary>
    public sealed class FirebaseBootStep : BootStep
    {
        [SerializeField] private AdConfig adConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private IapConfig iapConfig;
        [Tooltip("Minimum hours between Remote Config fetches in release builds (dev builds fetch every time).")]
        [SerializeField, Min(0f)] private float fetchIntervalHours = 12f;
        [SerializeField] private bool verboseDebugAnalytics = true;

        public override IEnumerator Run()
        {
            RegisterFallbacks();
#if MOONPULL_FIREBASE_ANALYTICS || MOONPULL_FIREBASE_CRASHLYTICS || MOONPULL_FIREBASE_REMOTE_CONFIG
            bool ready = false;
            bool available = false;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                available = task.Result == DependencyStatus.Available;
                if (!available)
                {
                    Debug.LogError($"[Firebase] Dependencies unavailable: {task.Result}");
                }

                ready = true;
            });

            while (!ready)
            {
                yield return null;
            }

            if (!available)
            {
                yield break;
            }

#if MOONPULL_FIREBASE_CRASHLYTICS
            Services.Register<ICrashReporter>(new FirebaseCrashReporter());
#endif
#if MOONPULL_FIREBASE_ANALYTICS
            var analytics = new FirebaseAnalyticsService();
            analytics.SetConsent(AnalyticsConsent.AllDenied);
            Services.Register<IAnalyticsService>(analytics);
#endif
#if MOONPULL_FIREBASE_REMOTE_CONFIG
            yield return FetchRemoteConfig();
#endif
#else
            yield break;
#endif
        }

        private void RegisterFallbacks()
        {
            Services.Register<IAnalyticsService>(new DebugAnalyticsService(verboseDebugAnalytics && AdConfig.IsDevelopmentBuild));
            Services.Register<ICrashReporter>(new DebugCrashReporter());
            Services.Register<IRemoteConfigService>(new LocalRemoteConfigService());
        }

        /// <summary>Local defaults mirror the ScriptableObjects, so a failed fetch behaves exactly like design intent.</summary>
        public Dictionary<string, object> BuildDefaults()
        {
            Ads.AdRulesSettings rules = adConfig.DefaultRules;
            return new Dictionary<string, object>
            {
                { RemoteConfigKeys.InterstitialStartLevel, rules.InterstitialStartLevel },
                { RemoteConfigKeys.InterstitialEveryLevels, rules.InterstitialEveryLevels },
                { RemoteConfigKeys.FullscreenCooldownSeconds, rules.FullscreenCooldownSeconds },
                { RemoteConfigKeys.LifetimeGraceSeconds, rules.LifetimeGraceSeconds },
                { RemoteConfigKeys.AppOpenMinBackgroundSeconds, rules.AppOpenMinBackgroundSeconds },
                { RemoteConfigKeys.RewardedLevelMultiplier, economyConfig.RewardedLevelMultiplier },
                { RemoteConfigKeys.RewardedIdleMultiplier, economyConfig.IdleRewardedMultiplier },
                { RemoteConfigKeys.DifficultyMultiplier, 1.0 },
                { RemoteConfigKeys.StarterPackIntroHours, iapConfig.StarterPackIntroHours }
            };
        }

#if MOONPULL_FIREBASE_REMOTE_CONFIG
        private IEnumerator FetchRemoteConfig()
        {
            FirebaseRemoteConfig remote = FirebaseRemoteConfig.DefaultInstance;
            bool done = false;
            ulong intervalMs = AdConfig.IsDevelopmentBuild ? 0UL : (ulong)(fetchIntervalHours * 3600000f);
            remote.SetConfigSettingsAsync(new ConfigSettings { MinimumFetchIntervalInMilliseconds = intervalMs })
                .ContinueWithOnMainThread(settingsTask =>
                {
                    remote.SetDefaultsAsync(BuildDefaults()).ContinueWithOnMainThread(defaultsTask =>
                    {
                        Services.Register<IRemoteConfigService>(new FirebaseRemoteConfigService());
                        remote.FetchAndActivateAsync().ContinueWithOnMainThread(fetch =>
                        {
                            if (fetch.IsFaulted)
                            {
                                Debug.LogWarning("[RemoteConfig] Fetch failed, using cached/default values.");
                            }

                            done = true;
                        });
                    });
                });

            while (!done)
            {
                yield return null;
            }
        }
#endif
    }
}
