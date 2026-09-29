using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Meta;
using NUnit.Framework;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class EconomyTests
    {
        private EconomyConfig economy;

        [SetUp]
        public void SetUp() => economy = SoTestUtil.Create<EconomyConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(economy);

        private static LevelResult Result(int level, int stars, int coins = 0, int passengers = 0, bool boss = false) =>
            new LevelResult(level, stars, 1000, 30f, 5, coins, 3, passengers, 0, boss, false);

        [Test]
        public void FirstClear_PaysBasePlusStarsPickupsPassengersBoss()
        {
            LevelReward reward = EconomyCalculator.ForLevel(economy, Result(10, 3, coins: 12, passengers: 2, boss: true), false, 1f, 1f);

            int expectedBase = Mathf.RoundToInt(economy.BaseLevelCoins + economy.CoinsPerLevelIndex * 10);
            Assert.AreEqual(expectedBase, reward.Base);
            Assert.AreEqual(3 * economy.CoinsPerStar, reward.StarBonus);
            Assert.AreEqual(12, reward.Pickups);
            Assert.AreEqual(2 * economy.CoinsPerPassenger, reward.Passengers);
            Assert.AreEqual(economy.BossBonusCoins, reward.Boss);
            Assert.AreEqual(reward.Base + reward.StarBonus + reward.Pickups + reward.Passengers + reward.Boss, reward.Total);
        }

        [Test]
        public void Replay_PaysReducedBaseButFullPickups()
        {
            LevelReward first = EconomyCalculator.ForLevel(economy, Result(0, 1, coins: 20), false, 1f, 1f);
            LevelReward replay = EconomyCalculator.ForLevel(economy, Result(0, 1, coins: 20), true, 1f, 1f);

            Assert.Less(replay.Base, first.Base);
            Assert.AreEqual(first.Pickups, replay.Pickups);
        }

        [Test]
        public void CoinPerk_MultipliesTotal()
        {
            LevelReward plain = EconomyCalculator.ForLevel(economy, Result(5, 2), false, 1f, 1f);
            LevelReward boosted = EconomyCalculator.ForLevel(economy, Result(5, 2), false, 1.2f, 1f);

            Assert.AreEqual(Mathf.RoundToInt(plain.Total * 1.2f), boosted.Total);
        }

        [Test]
        public void TripleReward_GrantsTwoExtraShares()
        {
            Assert.AreEqual(200, EconomyCalculator.RewardedBonus(100, 3));
            Assert.AreEqual(0, EconomyCalculator.RewardedBonus(100, 1));
        }

        [Test]
        public void Wallet_RejectsOverspendAndTracksSources()
        {
            var save = new FakeSaveService();
            var wallet = new Wallet(save);
            string lastSource = null;
            wallet.CoinsEarned += (amount, source) => lastSource = source;

            wallet.AddCoins(50, "level");
            Assert.AreEqual("level", lastSource);
            Assert.IsFalse(wallet.TrySpendCoins(60, "boat"));
            Assert.AreEqual(50, wallet.Coins);
            Assert.IsTrue(wallet.TrySpendCoins(50, "boat"));
            Assert.AreEqual(0, wallet.Coins);
            Assert.IsFalse(wallet.TrySpendCoins(-5, "exploit"));
        }

        [Test]
        public void BoatModifiers_MapPerksToMultipliers()
        {
            Assert.AreEqual(1.1f, BoatModifiers.From(BoatPerkType.CoinBonusPercent, 10f).CoinMultiplier, 1e-4f);
            Assert.AreEqual(2, BoatModifiers.From(BoatPerkType.CrashShields, 2f).Shields);
            Assert.AreEqual(1f, BoatModifiers.From(BoatPerkType.None, 50f).CoinMultiplier);
            Assert.AreEqual(1.5f, BoatModifiers.From(BoatPerkType.IdleIncomeBonusPercent, 50f).IdleIncomeMultiplier, 1e-4f);
        }

        [Test]
        public void LevelProgress_UnlocksNextAndKeepsBestStars()
        {
            var save = new FakeSaveService();
            LevelGenConfig generation = SoTestUtil.Create<LevelGenConfig>();
            RegionCatalog regions = SoTestUtil.Create<RegionCatalog>();
            var progress = new LevelProgress(save, generation, regions);

            Assert.IsTrue(progress.Record(Result(0, 2)));
            Assert.AreEqual(1, progress.HighestUnlockedLevel);
            Assert.IsFalse(progress.Record(Result(0, 1)), "Second clear is a replay.");
            Assert.AreEqual(2, progress.StarsFor(0), "Lower star replays never downgrade.");
            Assert.AreEqual(2, progress.TotalStars);

            Object.DestroyImmediate(generation);
            Object.DestroyImmediate(regions);
        }

        [Test]
        public void RegionGate_RequiresStars()
        {
            var save = new FakeSaveService();
            LevelGenConfig generation = SoTestUtil.Create<LevelGenConfig>();
            RegionDefinition lagoon = SoTestUtil.Create<RegionDefinition>();
            RegionDefinition north = SoTestUtil.Create<RegionDefinition>();
            SoTestUtil.SetInt(north, "starsToUnlock", 30);
            RegionCatalog regions = SoTestUtil.Create<RegionCatalog>();
            SoTestUtil.SetObjectArray(regions, "regions", new Object[] { lagoon, north });
            var progress = new LevelProgress(save, generation, regions);

            save.Data.HighestUnlockedLevel = 20;
            Assert.IsFalse(progress.IsLevelUnlocked(20));
            Assert.AreEqual(19, progress.NextLevelToPlay());

            for (int i = 0; i < 10; i++)
            {
                save.Data.LevelStars[i] = 3;
            }

            progress = new LevelProgress(save, generation, regions);
            Assert.IsTrue(progress.IsLevelUnlocked(20));

            Object.DestroyImmediate(generation);
            Object.DestroyImmediate(regions);
            Object.DestroyImmediate(lagoon);
            Object.DestroyImmediate(north);
        }

        [Test]
        public void Lighthouse_BuildsStagesAndScalesIdleRate()
        {
            var save = new FakeSaveService();
            var wallet = new Wallet(save);
            RegionDefinition region = SoTestUtil.Create<RegionDefinition>();
            RegionCatalog regions = SoTestUtil.Create<RegionCatalog>();
            SoTestUtil.SetObjectArray(regions, "regions", new Object[] { region });
            var lighthouses = new LighthouseService(save, regions, wallet);

            int firstCost = lighthouses.NextStageCost(0);
            Assert.IsFalse(lighthouses.TryBuildNext(0), "Can't build without coins.");
            wallet.AddCoins(firstCost, "test");
            Assert.IsTrue(lighthouses.TryBuildNext(0));
            Assert.AreEqual(1, lighthouses.StageOf(0));
            Assert.AreEqual(0, wallet.Coins);
            Assert.AreEqual(region.IdleCoinsPerHour / (float)region.LighthouseStageCount, lighthouses.IdleCoinsPerHour(1f), 1e-3f);

            Object.DestroyImmediate(region);
            Object.DestroyImmediate(regions);
        }

        [Test]
        public void LoginStreak_ContinuesOnlyFromYesterdayAndLoops()
        {
            Assert.AreEqual(1, LoginStreakService.NextDay(string.Empty, "20260101", 0));
            Assert.AreEqual(4, LoginStreakService.NextDay("20260101", "20260101", 3));
            Assert.AreEqual(1, LoginStreakService.NextDay("20251230", "20260101", 3), "A missed day resets.");
            Assert.AreEqual(1, LoginStreakService.NextDay("20260101", "20260101", 7), "Day 7 loops to day 1.");
        }

        [Test]
        public void SpinWheel_PicksByWeight()
        {
            var segments = new[]
            {
                new SpinSegment { Weight = 1f },
                new SpinSegment { Weight = 3f },
                new SpinSegment { Weight = 0f },
                new SpinSegment { Weight = 1f }
            };

            Assert.AreEqual(0, MoonPull.Meta.DailySpinService.PickSegment(segments, 0.0f));
            Assert.AreEqual(1, MoonPull.Meta.DailySpinService.PickSegment(segments, 0.3f));
            Assert.AreEqual(3, MoonPull.Meta.DailySpinService.PickSegment(segments, 0.9f));
            Assert.AreEqual(3, MoonPull.Meta.DailySpinService.PickSegment(segments, 0.99999f));
        }

        [Test]
        public void DailySpin_FreeOncePerDayThenLimitedExtras()
        {
            var save = new FakeSaveService();
            var clock = new FakeClock(new System.DateTime(2026, 5, 1, 12, 0, 0, System.DateTimeKind.Utc));
            EconomyConfig config = SoTestUtil.Create<EconomyConfig>();
            var spin = new DailySpinService(save, config, clock, new Wallet(save));

            Assert.IsFalse(spin.CanExtraSpin, "Extras only after the free spin.");
            Assert.GreaterOrEqual(spin.SpinFree(), 0);
            Assert.AreEqual(-1, spin.SpinFree());
            for (int i = 0; i < config.MaxExtraSpinsPerDay; i++)
            {
                Assert.GreaterOrEqual(spin.SpinExtra(), 0);
            }

            Assert.AreEqual(-1, spin.SpinExtra());
            clock.Advance(System.TimeSpan.FromDays(1));
            Assert.IsTrue(spin.FreeSpinAvailable);
            Object.DestroyImmediate(config);
        }
    }
}
