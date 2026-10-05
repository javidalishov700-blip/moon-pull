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
        public const int MaxLevel = 5;

        /// <summary>Visible building parts per building in the village (levels 4-5 grow the whole building).</summary>
        public const int VisualTiers = 3;
        public const int BuildingCount = 5;

        private static readonly int[,] Costs =
        {
            // Tuned so a player affords one or two upgrades a night early on, then saves up for the big ones.
            { 200, 650, 1500, 3400, 7000 },  // Shelter
            { 160, 550, 1300, 3000, 6200 },  // Restaurant
            { 260, 760, 1700, 3800, 7800 },  // Workshop
            { 320, 900, 2000, 4400, 9000 },  // Shipyard
            { 400, 1050, 2300, 4900, 10000 } // Market
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
        /// <summary>Homes never wait for workers: a full village must always be able to build room for more people.</summary>
        public static bool BlockedByWorkers(VillageBuilding b) => b != VillageBuilding.Shelter && NeedsWorkers;

        public static bool NeedsWorkers => VillageState.Population < TycoonState.JobsTotal + TycoonState.WorkersPerLevel;

        public static int WorkersNeeded => TycoonState.JobsTotal + TycoonState.WorkersPerLevel;

        public static bool TryUpgrade(VillageBuilding b, Wallet wallet)
        {
            int cost = NextCost(b);
            if (cost < 0 || IsCapped(b) || BlockedByWorkers(b) || wallet == null || !wallet.TrySpendCoins(cost, "village_" + b.ToString().ToLowerInvariant()))
            {
                return false;
            }

            TycoonState.Accrue(); // bank income at the old rate first
            PlayerPrefs.SetInt(Key(b), Level(b) + 1);
            PlayerPrefs.Save();
            VillageState.AddXp(40 + 30 * Level(b));
            return true;
        }

        /// <summary>True when the player can buy something right now (building, boat part or island): drives the menu badge.</summary>
        public static bool AnyAffordable(Wallet wallet)
        {
            if (wallet == null)
            {
                return false;
            }

            for (int b = 0; b < BuildingCount; b++)
            {
                var building = (VillageBuilding)b;
                int cost = NextCost(building);
                if (cost >= 0 && !IsCapped(building) && !BlockedByWorkers(building) && wallet.CanAfford(cost))
                {
                    return true;
                }
            }

            for (int p = 0; p < BoatUpgrades.PartCount; p++)
            {
                int cost = BoatUpgrades.NextCost((BoatPart)p);
                if (cost >= 0 && wallet.CanAfford(cost))
                {
                    return true;
                }
            }

            for (int i = 0; i < TycoonState.IslandCount; i++)
            {
                if (TycoonState.CanBuyNext(i) && wallet.CanAfford(TycoonState.IslandCost(i)))
                {
                    return true;
                }
            }

            return false;
        }

        // Effects used by NightRescue.
        public static int ExtraSeats => Level(VillageBuilding.Shelter) + VillageState.PerkSeats + (TycoonState.Owns(2) ? 1 : 0); // Fisher's Bay: a deckhand
        public static float CoinMultiplier => (1f + 0.3f * Level(VillageBuilding.Restaurant)) * VillageState.PerkCoinMultiplier * (TycoonState.Owns(1) ? 1.3f : 1f); // Coral Reef: pearls
        public static float NightMultiplier => (1f + 0.12f * Level(VillageBuilding.Workshop)) * (TycoonState.Owns(3) ? 1.2f : 1f); // Sky Peak: a lighthouse
        public static float SpeedBonus => 1.2f * Level(VillageBuilding.Shipyard);
        public static int DawnCoins(int population) =>
            Mathf.RoundToInt(Level(VillageBuilding.Market) * (10 + population / 2) * VillageState.Happiness / 100f);
    }
}
