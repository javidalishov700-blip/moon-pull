using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Mechanics
{
    public enum ScoreSource
    {
        Star,
        Coin,
        Moonstone,
        Chest,
        NearMiss,
        Passenger,
        BossHit
    }

    /// <summary>Pure scoring and rating math, kept free of MonoBehaviours so it is unit-testable.</summary>
    public static class ScoreRules
    {
        public static int BasePoints(ScoreConfig config, ScoreSource source)
        {
            switch (source)
            {
                case ScoreSource.Star: return config.StarPoints;
                case ScoreSource.Coin: return config.CoinPoints;
                case ScoreSource.Moonstone: return config.MoonstonePoints;
                case ScoreSource.Chest: return config.ChestPoints;
                case ScoreSource.NearMiss: return config.NearMissPoints;
                case ScoreSource.Passenger: return config.PassengerPoints;
                case ScoreSource.BossHit: return config.BossHitPoints;
                default: return 0;
            }
        }

        public static int Apply(int basePoints, int chainMultiplier, bool fullMoon, int fullMoonMultiplier) =>
            basePoints * Mathf.Max(1, chainMultiplier) * (fullMoon ? Mathf.Max(1, fullMoonMultiplier) : 1);

        /// <summary>1 star for finishing, 2 and 3 at the plan's score thresholds.</summary>
        public static int StarRating(int score, int twoStarScore, int threeStarScore)
        {
            if (score >= threeStarScore)
            {
                return 3;
            }

            return score >= twoStarScore ? 2 : 1;
        }
    }

    /// <summary>Consecutive near-miss chain. Multiplier rises with each near miss and resets on a clean pass or hit.</summary>
    public sealed class NearMissChain
    {
        private readonly int[] multipliers;

        public NearMissChain(int[] multipliers)
        {
            this.multipliers = multipliers;
        }

        public int Length { get; private set; }

        public int Multiplier => Length == 0 || multipliers.Length == 0
            ? 1
            : multipliers[Mathf.Min(Length, multipliers.Length) - 1];

        public int Register()
        {
            Length++;
            return Multiplier;
        }

        /// <summary>Resets the chain. Returns true if a chain was active (so UI can show "chain lost").</summary>
        public bool Break()
        {
            bool wasActive = Length > 0;
            Length = 0;
            return wasActive;
        }
    }
}
