using System;
using MoonPull.Config;
using MoonPull.Meta;
using NUnit.Framework;

namespace MoonPull.Tests
{
    public sealed class IdleIncomeTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Pending_ScalesWithTime()
        {
            Assert.AreEqual(90, IdleIncomeCalculator.Pending(60f, T0, T0.AddMinutes(90), 8f));
        }

        [Test]
        public void Pending_IsCappedAtCapHours()
        {
            Assert.AreEqual(480, IdleIncomeCalculator.Pending(60f, T0, T0.AddHours(30), 8f));
        }

        [Test]
        public void Pending_IsZeroForBackwardsClockOrZeroRate()
        {
            Assert.AreEqual(0, IdleIncomeCalculator.Pending(60f, T0, T0.AddHours(-3), 8f));
            Assert.AreEqual(0, IdleIncomeCalculator.Pending(0f, T0, T0.AddHours(3), 8f));
        }

        [Test]
        public void Pending_FloorsPartialCoins()
        {
            Assert.AreEqual(0, IdleIncomeCalculator.Pending(10f, T0, T0.AddMinutes(5), 8f));
        }

        [Test]
        public void Fill_ReachesOneAtCap()
        {
            Assert.AreEqual(0.5f, IdleIncomeCalculator.Fill01(T0, T0.AddHours(4), 8f), 1e-4f);
            Assert.AreEqual(1f, IdleIncomeCalculator.Fill01(T0, T0.AddHours(20), 8f), 1e-4f);
        }

        [Test]
        public void Service_BaselineCollectAndClockRepair()
        {
            var save = new FakeSaveService();
            var clock = new FakeClock(T0);
            EconomyConfig config = SoTestUtil.Create<EconomyConfig>();
            var wallet = new Wallet(save);
            var idle = new IdleIncomeService(save, config, clock, wallet, () => 120f);

            idle.EnsureBaseline();
            Assert.AreEqual(T0.Ticks, save.Data.LastIdleCollectUtcTicks);

            clock.Advance(TimeSpan.FromHours(2));
            Assert.AreEqual(240, idle.Pending);
            Assert.AreEqual(480, idle.Collect(2), "Doubled by the rewarded ad.");
            Assert.AreEqual(480, wallet.Coins);
            Assert.AreEqual(0, idle.Pending);

            clock.UtcNow = T0.AddDays(-1);
            idle.EnsureBaseline();
            Assert.AreEqual(clock.UtcNow.Ticks, save.Data.LastIdleCollectUtcTicks, "Clock moved back: baseline resets instead of freezing income.");
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
