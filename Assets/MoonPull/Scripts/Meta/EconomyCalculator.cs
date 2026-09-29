using System;
using MoonPull.Config;
using MoonPull.Core;

namespace MoonPull.Meta
{
    public readonly struct LevelReward
    {
        public readonly int Base;
        public readonly int StarBonus;
        public readonly int Pickups;
        public readonly int Passengers;
        public readonly int Boss;
        public readonly int Total;

        public LevelReward(int baseCoins, int starBonus, int pickups, int passengers, int boss, int total)
        {
            Base = baseCoins;
            StarBonus = starBonus;
            Pickups = pickups;
            Passengers = passengers;
            Boss = boss;
            Total = total;
        }
    }

    /// <summary>All coin math in one pure class so balance changes are unit-tested, never eyeballed.</summary>
    public static class EconomyCalculator
    {
        public static LevelReward ForLevel(EconomyConfig config, in LevelResult result, bool isReplay, float coinMultiplier, float passengerMultiplier)
        {
            float baseRaw = config.BaseLevelCoins + config.CoinsPerLevelIndex * result.LevelIndex;
            int baseCoins = (int)Math.Round(baseRaw * (isReplay ? config.ReplayBaseFraction : 1f));
            int starBonus = result.Stars * config.CoinsPerStar;
            int pickups = result.CoinsCollected;
            int passengers = (int)Math.Round(result.PassengersDelivered * config.CoinsPerPassenger * passengerMultiplier);
            int boss = result.BossDefeated ? config.BossBonusCoins : 0;
            int subtotal = baseCoins + starBonus + pickups + passengers + boss;
            int total = (int)Math.Round(subtotal * Math.Max(0f, coinMultiplier));
            return new LevelReward(baseCoins, starBonus, pickups, passengers, boss, total);
        }

        /// <summary>Extra coins granted by "Triple Your Reward" on top of the already-paid total.</summary>
        public static int RewardedBonus(int alreadyPaid, int multiplier) => alreadyPaid * Math.Max(0, multiplier - 1);
    }

    /// <summary>Offline lighthouse earnings. Clock going backwards yields zero, never negative coins.</summary>
    public static class IdleIncomeCalculator
    {
        public static long Pending(float coinsPerHour, DateTime lastCollectUtc, DateTime nowUtc, float capHours)
        {
            if (coinsPerHour <= 0f || nowUtc <= lastCollectUtc)
            {
                return 0L;
            }

            double hours = Math.Min((nowUtc - lastCollectUtc).TotalHours, capHours);
            return (long)Math.Floor(coinsPerHour * hours);
        }

        /// <summary>Fraction of the idle cap currently filled, for the "storage full" UI.</summary>
        public static float Fill01(DateTime lastCollectUtc, DateTime nowUtc, float capHours)
        {
            if (nowUtc <= lastCollectUtc || capHours <= 0f)
            {
                return 0f;
            }

            return (float)Math.Min(1d, (nowUtc - lastCollectUtc).TotalHours / capHours);
        }
    }

    /// <summary>Player-facing calendar days (local time) as "yyyyMMdd" keys, which survive JSON and compare cheaply.</summary>
    public static class DateKeys
    {
        public static string ToKey(DateTime localDate) => localDate.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

        public static string Today(IClock clock) => ToKey(clock.UtcNow.ToLocalTime().Date);

        public static string Yesterday(IClock clock) => ToKey(clock.UtcNow.ToLocalTime().Date.AddDays(-1));

        public static int Hash(string key)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < key.Length; i++)
                {
                    hash = hash * 31 + key[i];
                }

                return hash;
            }
        }
    }
}
