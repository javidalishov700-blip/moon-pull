using System;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>
    /// The island-tycoon layer. Every building level earns coins every minute into the village Treasury (also while
    /// the game is closed, up to a few hours' worth), and villagers add a little each. Coins buy new islands around
    /// the harbor: each one adds homes and multiplies all income.
    /// The village always needs the boat: buildings burn Supplies that only rescue nights bring home (every rescued
    /// person and lantern), every building level needs 3 workers (rescued people), and islands need a population.
    /// A rich village that stops sailing runs out of supplies and goes quiet.
    /// </summary>
    public static class TycoonState
    {
        public const int IslandCount = 4;
        public const float StorageHours = 3f;

        private const string TreasuryKey = "mp_tycoon_treasury";
        private const string TickKey = "mp_tycoon_tick";
        private const string SuppliesKey = "mp_tycoon_supplies";
        public const int WorkersPerLevel = 3;
        public const float CoinsPerSupply = 8f;
        private static readonly int[] IslandPeople = { 10, 20, 35, 50 };

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

        public static int IslandRequiredPeople(int island) => IslandPeople[island];

        // ---- supplies: only the boat brings them

        public static float Supplies => PlayerPrefs.GetFloat(SuppliesKey, 30f);

        public static int SupplyCapacity => 60 + 30 * IslandsOwned;

        public static bool OutOfSupplies => Supplies < 0.5f;

        public static void AddSupplies(int amount)
        {
            Accrue();
            PlayerPrefs.SetFloat(SuppliesKey, Mathf.Min(SupplyCapacity, Supplies + amount));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Supplies a night brings home.</summary>
        public static int SuppliesFromNight(int rescued, int lanterns) => rescued * 3 + lanterns * 2;

        // ---- workers: rescued people run the buildings

        public static int JobsTotal
        {
            get
            {
                int levels = 0;
                for (int b = 0; b < VillageService.BuildingCount; b++)
                {
                    levels += VillageService.Level((VillageBuilding)b);
                }

                return levels * WorkersPerLevel;
            }
        }

        /// <summary>0..1: how many jobs have a worker.</summary>
        public static float Staffed => JobsTotal == 0 ? 1f : Mathf.Clamp01(VillageState.Population / (float)JobsTotal);

        /// <summary>Islands must be bought in order; the next one also needs a village level.</summary>
        public static bool CanBuyNext(int island) =>
            !Owns(island) && (island == 0 || Owns(island - 1)) && VillageState.Level >= IslandVillageLevel[island]
            && VillageState.Population >= IslandPeople[island];

        public static int ExtraHousing => 8 * IslandsOwned;

        public static float IncomeMultiplier => 1f + 0.25f * IslandsOwned;

        /// <summary>Coins per minute at the current happiness.</summary>
        public static float IncomePerMinute
        {
            get
            {
                float buildings = 0f;
                for (int b = 0; b < VillageService.BuildingCount; b++)
                {
                    buildings += BuildingIncome[b] * VillageService.Level((VillageBuilding)b);
                }

                float rate = 0.2f * VillageState.Population + buildings * Staffed;
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
            float room = Mathf.Max(0f, Capacity - stored);
            // Production burns supplies; with none left the village earns nothing until the boat brings more.
            float earned = Mathf.Min(room, Mathf.Min(IncomePerMinute * minutes, Supplies * CoinsPerSupply));
            PlayerPrefs.SetFloat(TreasuryKey, stored + earned);
            PlayerPrefs.SetFloat(SuppliesKey, Mathf.Max(0f, Supplies - earned / CoinsPerSupply));
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
            PlayerPrefs.SetFloat(SuppliesKey, 42f);
            PlayerPrefs.SetString(TickKey, DateTime.UtcNow.Ticks.ToString());
        }
    }
}
