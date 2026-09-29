using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    public sealed class IdleIncomeService
    {
        private readonly ISaveService save;
        private readonly EconomyConfig config;
        private readonly IClock clock;
        private readonly Wallet wallet;
        private readonly Func<float> rateProvider;

        /// <param name="rateProvider">Current coins per hour (lighthouses × boat perk).</param>
        public IdleIncomeService(ISaveService save, EconomyConfig config, IClock clock, Wallet wallet, Func<float> rateProvider)
        {
            this.save = save;
            this.config = config;
            this.clock = clock;
            this.wallet = wallet;
            this.rateProvider = rateProvider;
        }

        public float CapHours => config.IdleCapHours;

        private DateTime LastCollectUtc => new DateTime(save.Data.LastIdleCollectUtcTicks, DateTimeKind.Utc);

        public long Pending => IdleIncomeCalculator.Pending(rateProvider(), LastCollectUtc, clock.UtcNow, config.IdleCapHours);

        public float Fill01 => IdleIncomeCalculator.Fill01(LastCollectUtc, clock.UtcNow, config.IdleCapHours);

        public bool ShouldShowCollectScreen => Pending >= config.IdleMinimumToShow;

        /// <summary>Starts the timer on first launch and repairs it if the device clock moved backwards.</summary>
        public void EnsureBaseline()
        {
            long now = clock.UtcNow.Ticks;
            if (save.Data.LastIdleCollectUtcTicks <= 0 || save.Data.LastIdleCollectUtcTicks > now)
            {
                save.Data.LastIdleCollectUtcTicks = now;
                save.MarkDirty();
            }
        }

        /// <summary>Pays out pending coins × <paramref name="multiplier"/> and restarts the timer. Returns coins paid.</summary>
        public long Collect(int multiplier)
        {
            long coins = Pending * Math.Max(1, multiplier);
            save.Data.LastIdleCollectUtcTicks = clock.UtcNow.Ticks;
            save.MarkDirty();
            wallet.AddCoins(coins, multiplier > 1 ? "idle_doubled" : "idle");
            return coins;
        }
    }
}
