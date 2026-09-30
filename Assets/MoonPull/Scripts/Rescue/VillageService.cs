using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>The village buildings you pay for with coins earned on rescue nights. Each one makes the next night better.</summary>
    public enum VillageBuilding
    {
        Shelter,     // more seats on the boat
        Restaurant,  // more coins per person rescued
        Workshop,    // lighthouse lamps burn longer: a longer night
        Shipyard,    // a faster boat
        Market       // the village pays out coins at dawn
    }

    public static class VillageService
    {
        public const int MaxLevel = 3;
        public const int BuildingCount = 5;

        private static readonly int[,] Costs =
        {
            { 150, 450, 1000 },  // Shelter
            { 120, 380, 850 },   // Restaurant
            { 200, 520, 1150 },  // Workshop
            { 250, 620, 1350 },  // Shipyard
            { 300, 700, 1500 }   // Market
        };

        private static string Key(VillageBuilding b) => "mp_village_" + b.ToString().ToLowerInvariant();

        public static int Level(VillageBuilding b) => Mathf.Clamp(PlayerPrefs.GetInt(Key(b), 0), 0, MaxLevel);

        /// <summary>Cost of the next level, or -1 when fully built.</summary>
        public static int NextCost(VillageBuilding b)
        {
            int level = Level(b);
            return level >= MaxLevel ? -1 : Costs[(int)b, level];
        }

        /// <summary>True when the next level exists but needs a higher village level first.</summary>
        public static bool IsCapped(VillageBuilding b) => Level(b) < MaxLevel && Level(b) >= VillageState.BuildingLevelCap;

        /// <summary>Building one more level needs enough people to staff every level (rescue more at sea).</summary>
        public static bool NeedsWorkers => VillageState.Population < TycoonState.JobsTotal + TycoonState.WorkersPerLevel;

        public static int WorkersNeeded => TycoonState.JobsTotal + TycoonState.WorkersPerLevel;

        public static bool TryUpgrade(VillageBuilding b, Wallet wallet)
        {
            int cost = NextCost(b);
            if (cost < 0 || IsCapped(b) || NeedsWorkers || wallet == null || !wallet.TrySpendCoins(cost, "village_" + b.ToString().ToLowerInvariant()))
            {
                return false;
            }

            TycoonState.Accrue(); // bank income at the old rate first
            PlayerPrefs.SetInt(Key(b), Level(b) + 1);
            PlayerPrefs.Save();
            VillageState.AddXp(40 + 30 * Level(b));
            return true;
        }

        // Effects used by NightRescue.
        public static int ExtraSeats => Level(VillageBuilding.Shelter);
        public static float CoinMultiplier => 1f + 0.3f * Level(VillageBuilding.Restaurant);
        public static float NightMultiplier => 1f + 0.12f * Level(VillageBuilding.Workshop);
        public static float SpeedBonus => 1.5f * Level(VillageBuilding.Shipyard);
        public static int DawnCoins(int population) =>
            Mathf.RoundToInt(Level(VillageBuilding.Market) * (10 + population / 2) * VillageState.Happiness / 100f);
    }
}
