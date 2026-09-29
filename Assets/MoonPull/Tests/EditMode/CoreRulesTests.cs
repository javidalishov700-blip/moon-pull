using MoonPull.Analytics;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Mechanics;
using MoonPull.Rewind;
using NUnit.Framework;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class CoreRulesTests
    {
        [Test]
        public void StateMachine_AllowsDesignedFlowAndBlocksIllegalOnes()
        {
            var machine = new GameStateMachine();
            Assert.IsTrue(machine.TryTransition(GameState.Consent));
            Assert.IsTrue(machine.TryTransition(GameState.Menu));
            Assert.IsTrue(machine.TryTransition(GameState.Playing));
            Assert.IsTrue(machine.TryTransition(GameState.Fail));
            Assert.IsTrue(machine.TryTransition(GameState.Rewinding));
            Assert.IsTrue(machine.TryTransition(GameState.Playing));
            Assert.IsTrue(machine.TryTransition(GameState.Win));
            Assert.IsFalse(machine.TryTransition(GameState.Rewinding), "Can't rewind a win.");
            Assert.AreEqual(GameState.Win, machine.Current);
        }

        [Test]
        public void RingBuffer_OverwritesOldestAndIndexesFromNewest()
        {
            var buffer = new RingBuffer<int>(3);
            for (int i = 1; i <= 5; i++)
            {
                buffer.Push(i);
            }

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(5, buffer.FromNewest(0));
            Assert.AreEqual(3, buffer.Oldest);
            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void NearMissChain_ClimbsTwoThreeFiveThenResets()
        {
            var chain = new NearMissChain(new[] { 2, 3, 5 });
            Assert.AreEqual(1, chain.Multiplier);
            Assert.AreEqual(2, chain.Register());
            Assert.AreEqual(3, chain.Register());
            Assert.AreEqual(5, chain.Register());
            Assert.AreEqual(5, chain.Register());
            Assert.IsTrue(chain.Break());
            Assert.AreEqual(1, chain.Multiplier);
            Assert.IsFalse(chain.Break());
        }

        [Test]
        public void Score_AppliesChainAndFullMoon()
        {
            Assert.AreEqual(100, ScoreRules.Apply(100, 1, false, 2));
            Assert.AreEqual(300, ScoreRules.Apply(100, 3, false, 2));
            Assert.AreEqual(600, ScoreRules.Apply(100, 3, true, 2));
        }

        [Test]
        public void StarRating_UsesThresholds()
        {
            Assert.AreEqual(1, ScoreRules.StarRating(10, 100, 200));
            Assert.AreEqual(2, ScoreRules.StarRating(100, 100, 200));
            Assert.AreEqual(3, ScoreRules.StarRating(250, 100, 200));
        }

        [Test]
        public void SeededRandom_IsDeterministicAndInRange()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 1000; i++)
            {
                int value = a.Range(3, 9);
                Assert.AreEqual(value, b.Range(3, 9));
                Assert.GreaterOrEqual(value, 3);
                Assert.Less(value, 9);
            }

            var zero = new SeededRandom(0);
            Assert.AreNotEqual(0u, zero.NextUInt(), "Seed 0 must not lock xorshift at zero.");
        }

        [Test]
        public void Box2_GapAndOverlap()
        {
            var hull = new Box2(0f, 1f, 2f, 2f);
            var rock = new Box2(1f, -5f, 3f, 0.8f);
            Assert.IsTrue(hull.OverlapsX(rock));
            Assert.IsFalse(hull.Overlaps(rock));
            Assert.AreEqual(0.2f, hull.VerticalGap(rock), 1e-5f);
        }

        [Test]
        public void TcfConsent_MapsPurposesToConsentMode()
        {
            AnalyticsConsent none = TcfConsentReader.FromTcf(0, string.Empty);
            Assert.IsTrue(none.AdPersonalization, "GDPR not applicable: everything granted.");

            AnalyticsConsent rejected = TcfConsentReader.FromTcf(1, "0000000000");
            Assert.IsFalse(rejected.AdStorage);
            Assert.IsFalse(rejected.AnalyticsStorage);

            AnalyticsConsent partial = TcfConsentReader.FromTcf(1, "1000001000");
            Assert.IsTrue(partial.AdStorage);
            Assert.IsTrue(partial.AdUserData);
            Assert.IsFalse(partial.AdPersonalization);

            AnalyticsConsent full = TcfConsentReader.FromTcf(1, "1111111111");
            Assert.IsTrue(full.AdPersonalization);
        }

        [Test]
        public void Spring_ConvergesWithoutOvershootWhenCriticallyDamped()
        {
            float value = 0f;
            float velocity = 0f;
            for (int i = 0; i < 600; i++)
            {
                Spring.Step(ref value, ref velocity, 1f, 2f, 1f, 1f / 60f);
                Assert.LessOrEqual(value, 1.001f);
            }

            Assert.AreEqual(1f, value, 1e-3f);
        }

        [Test]
        public void StarterPack_IntroWindowCountsFromFirstLaunch()
        {
            var data = new Save.SaveData();
            var first = new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            data.Ads.FirstLaunchUtcTicks = first.Ticks;

            Assert.IsTrue(IAP.StarterPackOffer.IsIntroActive(data, first.AddHours(47), 48f));
            Assert.IsFalse(IAP.StarterPackOffer.IsIntroActive(data, first.AddHours(49), 48f));
            data.StarterPackPurchased = true;
            Assert.IsFalse(IAP.StarterPackOffer.IsAvailable(data));
        }

        [Test]
        public void GameEvents_ClearAllRemovesSubscribers()
        {
            int calls = 0;
            GameEvents.NearMiss += (chain, multiplier) => calls++;
            GameEvents.ClearAll();
            GameEvents.RaiseNearMiss(1, 2);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void ScoreConfigDefaults_MatchDesign()
        {
            ScoreConfig config = ScriptableObject.CreateInstance<ScoreConfig>();
            CollectionAssert.AreEqual(new[] { 2, 3, 5 }, config.ChainMultipliers);
            Assert.AreEqual(2, config.FullMoonMultiplier);
            Object.DestroyImmediate(config);
        }
    }
}
