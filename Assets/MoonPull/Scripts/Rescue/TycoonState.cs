using System;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>
    /// The island-tycoon layer. Every building level earns coins every minute into the village Treasury (also while
    /// the game is closed, up to a few hours' worth), and villagers add a little each. Coins buy new islands around
    /// the harbor: each one adds homes and multiplies all income. Rescue nights bring the people who make it all run.
    /// </summary>
    public static class TycoonState
    {
        public const int IslandCount = 4;
        public const float StorageHours = 3f;

        private const string TreasuryKey = "mp_tycoon_treasury";
        private const string TickKey = "mp_tycoon_tick";

        // Per-level coins per minute for Shelter, Restaurant, Workshop, Shipyard, Market.
        private static readonly float[] BuildingIncome = { 1f, 3f, 2f, 2f, 5f };

        private static readonly int[] IslandCosts = { 1500, 4000, 9000, 20000 };
        private static readonly int[] IslandVillageLevel = { 2, 4, 6, 8 };
        public static readonly string[] IslandIds = { "palm_cove", "coral_reef", "fisher_bay", "sky_peak" };

        public static event Action Changed;

        public static bool Owns(int island) => PlayerPrefs.GetInt("mp_island_" + IslandIds[island], 0) == 1;

        public static int IslandsOwned
        {
            get
            {
                int n = 0;
                for (int i = 0; i < IslandCount; i++)
                {
                    n += Owns(i) ? 1 : 0;
                }

                return n;
            }
        }

        public static int IslandCost(int island) => IslandCosts[island];

        public static int IslandRequiredLevel(int island) => IslandVillageLevel[island];

        /// <summary>Islands must be bought in order; the next one also needs a village level.</summary>
        public static bool CanBuyNext(int island) =>
            !Owns(island) && (island == 0 || Owns(island - 1)) && VillageState.Level >= IslandVillageLevel[island];

        public static int ExtraHousing => 8 * IslandsOwned;

        public static float IncomeMultiplier => 1f + 0.25f * IslandsOwned;

        /// <summary>Coins per minute at the current happiness.</summary>
        public static float IncomePerMinute
        {
            get
            {
                float rate = 0.2f * VillageState.Population;
                for (int b = 0; b < VillageService.BuildingCount; b++)
                {
                    rate += BuildingIncome[b] * VillageService.Level((VillageBuilding)b);
                }

                return rate * IncomeMultiplier * (0.5f + VillageState.Happiness / 200f);
            }
        }

        public static float Capacity => Mathf.Max(100f, IncomePerMinute * 60f * StorageHours);

        public static int Treasury
        {
            get
            {
                Accrue();
                return Mathf.FloorToInt(PlayerPrefs.GetFloat(TreasuryKey, 0f));
            }
        }

        public static bool IsFull => Treasury >= Mathf.FloorToInt(Capacity);

        /// <summary>Adds income earned since the last call (capped by storage).</summary>
        public static void Accrue()
        {
            long now = DateTime.UtcNow.Ticks;
            long last = long.TryParse(PlayerPrefs.GetString(TickKey, "0"), out long parsed) ? parsed : 0;
            PlayerPrefs.SetString(TickKey, now.ToString());
            if (last <= 0 || now <= last)
            {
                return;
            }

            float minutes = Mathf.Min(StorageHours * 60f, (float)TimeSpan.FromTicks(now - last).TotalMinutes);
            float stored = PlayerPrefs.GetFloat(TreasuryKey, 0f);
            PlayerPrefs.SetFloat(TreasuryKey, Mathf.Min(Capacity, stored + IncomePerMinute * minutes));
        }

        public static int Collect(Wallet wallet)
        {
            int amount = Treasury;
            if (amount <= 0 || wallet == null)
            {
                return 0;
            }

            PlayerPrefs.SetFloat(TreasuryKey, PlayerPrefs.GetFloat(TreasuryKey, 0f) - amount);
            PlayerPrefs.Save();
            wallet.AddCoins(amount, "village_treasury");
            Changed?.Invoke();
            return amount;
        }

        public static bool TryBuyIsland(int island, Wallet wallet)
        {
            Accrue(); // bank income at the old rate first
            if (!CanBuyNext(island) || wallet == null || !wallet.TrySpendCoins(IslandCosts[island], "island_" + IslandIds[island]))
            {
                return false;
            }

            PlayerPrefs.SetInt("mp_island_" + IslandIds[island], 1);
            PlayerPrefs.Save();
            VillageState.AddXp(120 + 80 * island);
            Changed?.Invoke();
            return true;
        }

        /// <summary>For the store-capture player only.</summary>
        public static void SeedForCapture(int islands, float treasury)
        {
            for (int i = 0; i < IslandCount; i++)
            {
                PlayerPrefs.SetInt("mp_island_" + IslandIds[i], i < islands ? 1 : 0);
            }

            PlayerPrefs.SetFloat(TreasuryKey, treasury);
            PlayerPrefs.SetString(TickKey, DateTime.UtcNow.Ticks.ToString());
        }
    }
}
