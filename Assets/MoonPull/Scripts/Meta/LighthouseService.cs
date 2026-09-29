using System;
using MoonPull.Config;
using MoonPull.Save;

namespace MoonPull.Meta
{
    /// <summary>
    /// Eight construction stages per region, bought with coins. Each built stage adds its share of the region's idle
    /// rate, so offline income starts on the first purchase instead of after days of saving.
    /// </summary>
    public sealed class LighthouseService
    {
        private readonly ISaveService save;
        private readonly RegionCatalog regions;
        private readonly Wallet wallet;

        public LighthouseService(ISaveService save, RegionCatalog regions, Wallet wallet)
        {
            this.save = save;
            this.regions = regions;
            this.wallet = wallet;
        }

        /// <summary>(regionIndex, newStage) after a successful build.</summary>
        public event Action<int, int> StageBuilt;

        public int RegionCount => regions.Count;

        public int StageOf(int regionIndex) =>
            regionIndex >= 0 && regionIndex < save.Data.LighthouseStages.Length ? save.Data.LighthouseStages[regionIndex] : 0;

        public int StageCount(int regionIndex) => regions[regionIndex].LighthouseStageCount;

        public bool IsComplete(int regionIndex) => StageOf(regionIndex) >= StageCount(regionIndex);

        /// <summary>Cost of the next stage, or -1 when complete.</summary>
        public int NextStageCost(int regionIndex)
        {
            int stage = StageOf(regionIndex);
            int[] costs = regions[regionIndex].LighthouseStageCosts;
            return stage < costs.Length ? costs[stage] : -1;
        }

        public bool TryBuildNext(int regionIndex)
        {
            int cost = NextStageCost(regionIndex);
            if (cost < 0 || !wallet.TrySpendCoins(cost, "lighthouse"))
            {
                return false;
            }

            int stage = ++save.Data.LighthouseStages[regionIndex];
            save.MarkDirty();
            StageBuilt?.Invoke(regionIndex, stage);
            return true;
        }

        public float IdleCoinsPerHour(float multiplier)
        {
            float rate = 0f;
            for (int r = 0; r < regions.Count; r++)
            {
                int count = StageCount(r);
                if (count > 0)
                {
                    rate += regions[r].IdleCoinsPerHour * StageOf(r) / (float)count;
                }
            }

            return rate * multiplier;
        }
    }
}
