using System;
using MoonPull.Save;

namespace MoonPull.Ads
{
    public readonly struct AdRulesSettings
    {
        public readonly int InterstitialStartLevel;
        public readonly int InterstitialEveryLevels;
        public readonly float FullscreenCooldownSeconds;
        public readonly float LifetimeGraceSeconds;
        public readonly float AppOpenMinBackgroundSeconds;

        public AdRulesSettings(int interstitialStartLevel, int interstitialEveryLevels, float fullscreenCooldownSeconds,
            float lifetimeGraceSeconds, float appOpenMinBackgroundSeconds)
        {
            InterstitialStartLevel = interstitialStartLevel;
            InterstitialEveryLevels = Math.Max(1, interstitialEveryLevels);
            FullscreenCooldownSeconds = fullscreenCooldownSeconds;
            LifetimeGraceSeconds = lifetimeGraceSeconds;
            AppOpenMinBackgroundSeconds = appOpenMinBackgroundSeconds;
        }
    }

    public enum InterstitialDecision
    {
        Show,
        AdsRemoved,
        LifetimeGrace,
        BeforeStartLevel,
        NotEnoughLevels,
        Cooldown,
        SkippedAfterRewarded
    }

    /// <summary>
    /// Pure, testable ad pacing. All state lives in <see cref="AdsStateData"/> so pacing survives restarts
    /// (a player can't dodge the cooldown by relaunching, and isn't punished with back-to-back ads either).
    /// </summary>
    public sealed class AdFrequencyRules
    {
        public AdFrequencyRules(AdRulesSettings settings)
        {
            Settings = settings;
        }

        public AdRulesSettings Settings { get; set; }

        /// <summary>Counts a completed level toward the interstitial cadence.</summary>
        public void OnLevelCompleted(AdsStateData state) => state.LevelsSinceInterstitial++;

        /// <summary>
        /// Decides whether to show an interstitial after completing <paramref name="completedLevelIndex"/> (0-based).
        /// A "skip after rewarded" decision consumes the skip flag.
        /// </summary>
        public InterstitialDecision EvaluateInterstitial(AdsStateData state, int completedLevelIndex, DateTime nowUtc, bool adsRemoved)
        {
            if (adsRemoved)
            {
                return InterstitialDecision.AdsRemoved;
            }

            if (state.LifetimePlaySeconds < Settings.LifetimeGraceSeconds)
            {
                return InterstitialDecision.LifetimeGrace;
            }

            if (completedLevelIndex + 1 < Settings.InterstitialStartLevel)
            {
                return InterstitialDecision.BeforeStartLevel;
            }

            if (state.LevelsSinceInterstitial < Settings.InterstitialEveryLevels)
            {
                return InterstitialDecision.NotEnoughLevels;
            }

            if (SecondsSinceFullscreen(state, nowUtc) < Settings.FullscreenCooldownSeconds)
            {
                return InterstitialDecision.Cooldown;
            }

            if (state.SkipNextInterstitial)
            {
                state.SkipNextInterstitial = false;
                state.LevelsSinceInterstitial = 0;
                return InterstitialDecision.SkippedAfterRewarded;
            }

            return InterstitialDecision.Show;
        }

        public void OnInterstitialShown(AdsStateData state, DateTime nowUtc)
        {
            state.LevelsSinceInterstitial = 0;
            state.LastFullscreenAdUtcTicks = nowUtc.Ticks;
        }

        /// <summary>A watched rewarded ad starts the cooldown and cancels the next interstitial.</summary>
        public void OnRewardedShown(AdsStateData state, DateTime nowUtc)
        {
            state.LastFullscreenAdUtcTicks = nowUtc.Ticks;
            state.LastRewardedAdUtcTicks = nowUtc.Ticks;
            state.SkipNextInterstitial = true;
        }

        public void OnAppOpenShown(AdsStateData state, DateTime nowUtc) => state.LastFullscreenAdUtcTicks = nowUtc.Ticks;

        /// <summary>App open: never in the first session, only after a long enough background, never inside the cooldown.</summary>
        public bool ShouldShowAppOpen(AdsStateData state, float secondsInBackground, DateTime nowUtc, bool adsRemoved)
        {
            return !adsRemoved
                   && state.SessionCount > 1
                   && secondsInBackground >= Settings.AppOpenMinBackgroundSeconds
                   && SecondsSinceFullscreen(state, nowUtc) >= Settings.FullscreenCooldownSeconds;
        }

        // A device clock moved backwards yields a negative span; treat it as "long ago" rather than blocking ads forever.
        private static double SecondsSinceFullscreen(AdsStateData state, DateTime nowUtc)
        {
            if (state.LastFullscreenAdUtcTicks <= 0)
            {
                return double.MaxValue;
            }

            double seconds = (nowUtc - new DateTime(state.LastFullscreenAdUtcTicks, DateTimeKind.Utc)).TotalSeconds;
            return seconds < 0d ? double.MaxValue : seconds;
        }
    }
}
