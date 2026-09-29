using System.Collections;
using MoonPull.Ads;
using MoonPull.Analytics;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Level;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>
    /// Pushes Remote Config values into the systems they tune. Every value is clamped to a sane range, so a typo in
    /// the Firebase console can never ship "interstitial every 0 levels" to live players.
    /// </summary>
    public sealed class RemoteConfigApplyStep : BootStep
    {
        [SerializeField] private AdConfig adConfig;
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private IapConfig iapConfig;
        [SerializeField] private AdsCoordinator adsCoordinator;
        [SerializeField] private MetaGame meta;
        [SerializeField] private LevelSession levelSession;

        public override IEnumerator Run()
        {
            IRemoteConfigService remote = Services.Get<IRemoteConfigService>();
            AdRulesSettings defaults = adConfig.DefaultRules;

            adsCoordinator.ApplyRules(new AdRulesSettings(
                (int)Clamp(remote.GetLong(RemoteConfigKeys.InterstitialStartLevel, defaults.InterstitialStartLevel), 2, 20),
                (int)Clamp(remote.GetLong(RemoteConfigKeys.InterstitialEveryLevels, defaults.InterstitialEveryLevels), 1, 10),
                (float)Clamp(remote.GetDouble(RemoteConfigKeys.FullscreenCooldownSeconds, defaults.FullscreenCooldownSeconds), 30, 600),
                (float)Clamp(remote.GetDouble(RemoteConfigKeys.LifetimeGraceSeconds, defaults.LifetimeGraceSeconds), 180, 1800),
                (float)Clamp(remote.GetDouble(RemoteConfigKeys.AppOpenMinBackgroundSeconds, defaults.AppOpenMinBackgroundSeconds), 30, 3600)));

            meta.RewardedLevelMultiplier = (int)Clamp(remote.GetLong(RemoteConfigKeys.RewardedLevelMultiplier, economyConfig.RewardedLevelMultiplier), 2, 5);
            meta.IdleRewardedMultiplier = (int)Clamp(remote.GetLong(RemoteConfigKeys.RewardedIdleMultiplier, economyConfig.IdleRewardedMultiplier), 2, 4);
            meta.StarterPackIntroHours = (float)Clamp(remote.GetDouble(RemoteConfigKeys.StarterPackIntroHours, iapConfig.StarterPackIntroHours), 0, 168);
            levelSession.DifficultyScale = (float)Clamp(remote.GetDouble(RemoteConfigKeys.DifficultyMultiplier, 1.0), 0.5, 1.5);
            yield break;
        }

        private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
    }
}
