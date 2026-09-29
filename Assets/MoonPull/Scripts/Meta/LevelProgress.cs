using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Meta
{
    /// <summary>Stars, best scores, level and region unlocks. Region gates on total stars pull players back to replay.</summary>
    public sealed class LevelProgress
    {
        private readonly ISaveService save;
        private readonly LevelGenConfig generation;
        private readonly RegionCatalog regions;
        private int totalStars = -1;

        public LevelProgress(ISaveService save, LevelGenConfig generation, RegionCatalog regions)
        {
            this.save = save;
            this.generation = generation;
            this.regions = regions;
        }

        public int LevelCount => generation.TotalLevels;
        public int LevelsPerRegion => generation.LevelsPerRegion;
        public int HighestUnlockedLevel => save.Data.HighestUnlockedLevel;

        public int TotalStars
        {
            get
            {
                if (totalStars < 0)
                {
                    totalStars = 0;
                    int[] stars = save.Data.LevelStars;
                    for (int i = 0; i < stars.Length; i++)
                    {
                        totalStars += stars[i];
                    }
                }

                return totalStars;
            }
        }

        public int StarsFor(int levelIndex) => InRange(levelIndex) ? save.Data.LevelStars[levelIndex] : 0;

        public int BestScoreFor(int levelIndex) => InRange(levelIndex) ? save.Data.LevelBestScores[levelIndex] : 0;

        public bool IsCompleted(int levelIndex) => StarsFor(levelIndex) > 0;

        public int RegionOf(int levelIndex) => levelIndex / Mathf.Max(1, generation.LevelsPerRegion);

        public bool IsRegionUnlocked(int regionIndex) =>
            regionIndex == 0 || (regionIndex < regions.Count && TotalStars >= regions[regionIndex].StarsToUnlock);

        public bool IsLevelUnlocked(int levelIndex) =>
            InRange(levelIndex) && levelIndex <= save.Data.HighestUnlockedLevel && IsRegionUnlocked(RegionOf(levelIndex));

        /// <summary>Level the Play button starts: the frontier, or the last playable level if the next region is star-gated.</summary>
        public int NextLevelToPlay()
        {
            int frontier = save.Data.HighestUnlockedLevel;
            for (int i = frontier; i >= 0; i--)
            {
                if (IsLevelUnlocked(i))
                {
                    return i;
                }
            }

            return 0;
        }

        public int StarsInRegion(int regionIndex)
        {
            int start = regionIndex * generation.LevelsPerRegion;
            int sum = 0;
            for (int i = start; i < start + generation.LevelsPerRegion && i < LevelCount; i++)
            {
                sum += StarsFor(i);
            }

            return sum;
        }

        /// <summary>Stores the result. Returns true if this was the first completion (not a replay).</summary>
        public bool Record(in LevelResult result)
        {
            int index = result.LevelIndex;
            if (!InRange(index))
            {
                return false;
            }

            SaveData data = save.Data;
            bool firstClear = data.LevelStars[index] == 0;
            if (result.Stars > data.LevelStars[index])
            {
                data.LevelStars[index] = result.Stars;
                totalStars = -1;
            }

            if (result.Score > data.LevelBestScores[index])
            {
                data.LevelBestScores[index] = result.Score;
            }

            if (index == data.HighestUnlockedLevel && index + 1 < LevelCount)
            {
                data.HighestUnlockedLevel = index + 1;
            }

            data.TotalLevelsCompleted++;
            save.MarkDirty();
            return firstClear;
        }

        private bool InRange(int index) => index >= 0 && index < save.Data.LevelStars.Length;
    }
}
