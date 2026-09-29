namespace MoonPull.Core
{
    /// <summary>Why a run ended. Values map 1:1 to the analytics level_fail reason.</summary>
    public enum FailReason
    {
        Rock,
        Bridge,
        Aground,
        Shark,
        Kraken
    }

    /// <summary>Parameters for starting a level.</summary>
    public readonly struct LevelStartArgs
    {
        public readonly int LevelIndex;
        public readonly string BoatId;
        public readonly bool IsTrialBoat;

        public LevelStartArgs(int levelIndex, string boatId, bool isTrialBoat)
        {
            LevelIndex = levelIndex;
            BoatId = boatId;
            IsTrialBoat = isTrialBoat;
        }
    }

    /// <summary>Everything the meta layer needs once a level is won.</summary>
    public readonly struct LevelResult
    {
        public readonly int LevelIndex;
        public readonly int Stars;
        public readonly int Score;
        public readonly float DurationSeconds;
        public readonly int StarsCollected;
        public readonly int CoinsCollected;
        public readonly int NearMisses;
        public readonly int PassengersDelivered;
        public readonly int TreasuresFound;
        public readonly bool BossDefeated;
        public readonly bool UsedRewind;

        public LevelResult(int levelIndex, int stars, int score, float durationSeconds, int starsCollected,
            int coinsCollected, int nearMisses, int passengersDelivered, int treasuresFound, bool bossDefeated, bool usedRewind)
        {
            LevelIndex = levelIndex;
            Stars = stars;
            Score = score;
            DurationSeconds = durationSeconds;
            StarsCollected = starsCollected;
            CoinsCollected = coinsCollected;
            NearMisses = nearMisses;
            PassengersDelivered = passengersDelivered;
            TreasuresFound = treasuresFound;
            BossDefeated = bossDefeated;
            UsedRewind = usedRewind;
        }
    }
}

namespace MoonPull.Core
{
    public enum WeatherKind
    {
        None,
        Storm,
        Fog,
        Eclipse
    }
}
