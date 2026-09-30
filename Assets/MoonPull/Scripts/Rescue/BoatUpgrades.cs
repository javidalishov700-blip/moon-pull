using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Rescue
{
    /// <summary>Upgrades for the rescue boat itself, bought with coins in the Village screen's Boat tab.</summary>
    public enum BoatPart
    {
        Sail,  // higher cruising speed
        Hull,  // rocks and belly flops hurt less; passengers hold on
        Lamp   // a wider reach for castaways and sky lanterns
    }

    public static class BoatUpgrades
    {
        public const int MaxLevel = 5;
        public const int PartCount = 3;

        private static readonly int[] BaseCost = { 120, 160, 140 };
        private static readonly float[] LevelScale = { 1f, 2.5f, 5f, 9f, 15f };

        private static string Key(BoatPart p) => "mp_boat_" + p.ToString().ToLowerInvariant();

        public static int Level(BoatPart p) => Mathf.Clamp(PlayerPrefs.GetInt(Key(p), 0), 0, MaxLevel);

        public static int NextCost(BoatPart p)
        {
            int level = Level(p);
            return level >= MaxLevel ? -1 : Mathf.RoundToInt(BaseCost[(int)p] * LevelScale[level] / 10f) * 10;
        }

        public static bool TryUpgrade(BoatPart p, Wallet wallet)
        {
            int cost = NextCost(p);
            if (cost < 0 || wallet == null || !wallet.TrySpendCoins(cost, "boat_" + p.ToString().ToLowerInvariant()))
            {
                return false;
            }

            PlayerPrefs.SetInt(Key(p), Level(p) + 1);
            PlayerPrefs.Save();
            return true;
        }

        public static float CruiseBonus => 1.2f * Level(BoatPart.Sail);

        /// <summary>Fraction of speed kept after a hit.</summary>
        public static float HitSpeedKept => 0.4f + 0.08f * Level(BoatPart.Hull);

        /// <summary>Chance that nobody falls overboard on a hit.</summary>
        public static float HoldOnChance => 0.18f * Level(BoatPart.Hull);

        public static float ReachBonus => 0.25f * Level(BoatPart.Lamp);
    }
}
