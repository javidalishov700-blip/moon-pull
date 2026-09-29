using System;
using MoonPull.Ads;
using MoonPull.Save;
using NUnit.Framework;

namespace MoonPull.Tests
{
    public sealed class AdFrequencyRulesTests
    {
        private static readonly DateTime Now = new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc);

        private AdFrequencyRules rules;
        private AdsStateData state;

        [SetUp]
        public void SetUp()
        {
            rules = new AdFrequencyRules(new AdRulesSettings(4, 2, 45f, 180f, 30f));
            state = new AdsStateData { LifetimePlaySeconds = 600, SessionCount = 3, LevelsSinceInterstitial = 2 };
        }

        [Test]
        public void ShowsWhenAllConditionsPass()
        {
            Assert.AreEqual(InterstitialDecision.Show, rules.EvaluateInterstitial(state, 5, Now, false));
        }

        [Test]
        public void NeverWithRemoveAds()
        {
            Assert.AreEqual(InterstitialDecision.AdsRemoved, rules.EvaluateInterstitial(state, 5, Now, true));
        }

        [Test]
        public void NeverInFirstThreeMinutesOfLifetime()
        {
            state.LifetimePlaySeconds = 179;
            Assert.AreEqual(InterstitialDecision.LifetimeGrace, rules.EvaluateInterstitial(state, 10, Now, false));
        }

        [Test]
        public void NeverBeforeLevelFour()
        {
            Assert.AreEqual(InterstitialDecision.BeforeStartLevel, rules.EvaluateInterstitial(state, 2, Now, false));
            Assert.AreEqual(InterstitialDecision.Show, rules.EvaluateInterstitial(state, 3, Now, false));
        }

        [Test]
        public void OnlyEverySecondLevel()
        {
            state.LevelsSinceInterstitial = 0;
            rules.OnLevelCompleted(state);
            Assert.AreEqual(InterstitialDecision.NotEnoughLevels, rules.EvaluateInterstitial(state, 6, Now, false));
            rules.OnLevelCompleted(state);
            Assert.AreEqual(InterstitialDecision.Show, rules.EvaluateInterstitial(state, 7, Now, false));
        }

        [Test]
        public void RespectsCooldownSinceAnyFullscreenAd()
        {
            state.LastFullscreenAdUtcTicks = Now.AddSeconds(-44).Ticks;
            Assert.AreEqual(InterstitialDecision.Cooldown, rules.EvaluateInterstitial(state, 6, Now, false));
            state.LastFullscreenAdUtcTicks = Now.AddSeconds(-46).Ticks;
            Assert.AreEqual(InterstitialDecision.Show, rules.EvaluateInterstitial(state, 6, Now, false));
        }

        [Test]
        public void SkipsNextInterstitialAfterRewarded()
        {
            rules.OnRewardedShown(state, Now.AddMinutes(-5));
            Assert.AreEqual(InterstitialDecision.SkippedAfterRewarded, rules.EvaluateInterstitial(state, 6, Now, false));
            Assert.IsFalse(state.SkipNextInterstitial, "Skip is consumed.");
            Assert.AreEqual(0, state.LevelsSinceInterstitial);
        }

        [Test]
        public void ShowingResetsCounterAndStartsCooldown()
        {
            rules.OnInterstitialShown(state, Now);
            Assert.AreEqual(0, state.LevelsSinceInterstitial);
            state.LevelsSinceInterstitial = 5;
            Assert.AreEqual(InterstitialDecision.Cooldown, rules.EvaluateInterstitial(state, 9, Now.AddSeconds(10), false));
        }

        [Test]
        public void ClockMovedBackwards_DoesNotBlockForever()
        {
            state.LastFullscreenAdUtcTicks = Now.AddHours(5).Ticks;
            Assert.AreEqual(InterstitialDecision.Show, rules.EvaluateInterstitial(state, 6, Now, false));
        }

        [Test]
        public void AppOpen_NeverInFirstSession()
        {
            state.SessionCount = 1;
            Assert.IsFalse(rules.ShouldShowAppOpen(state, 120f, Now, false));
        }

        [Test]
        public void AppOpen_NeedsThirtySecondsAway()
        {
            Assert.IsFalse(rules.ShouldShowAppOpen(state, 29f, Now, false));
            Assert.IsTrue(rules.ShouldShowAppOpen(state, 31f, Now, false));
        }

        [Test]
        public void AppOpen_BlockedByRemoveAdsAndCooldown()
        {
            Assert.IsFalse(rules.ShouldShowAppOpen(state, 120f, Now, true));
            state.LastFullscreenAdUtcTicks = Now.AddSeconds(-10).Ticks;
            Assert.IsFalse(rules.ShouldShowAppOpen(state, 120f, Now, false));
        }
    }
}
