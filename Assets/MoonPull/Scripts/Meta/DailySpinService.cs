using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    public sealed class DailySpinService
    {
        private readonly ISaveService save;
        private readonly EconomyConfig config;
        private readonly IClock clock;
        private readonly Wallet wallet;
        private SeededRandom rng;

        public DailySpinService(ISaveService save, EconomyConfig config, IClock clock, Wallet wallet)
        {
            this.save = save;
            this.config = config;
            this.clock = clock;
            this.wallet = wallet;
            rng = new SeededRandom((int)(clock.UtcNow.Ticks & 0x7FFFFFFF));
        }

        public SpinSegment[] Segments => config.SpinSegments;

        public bool FreeSpinAvailable => save.Data.FreeSpinDate != DateKeys.Today(clock);

        public int ExtraSpinsRemaining
        {
            get
            {
                int used = save.Data.Ads.ExtraSpinsDate == DateKeys.Today(clock) ? save.Data.Ads.ExtraSpinsToday : 0;
                return System.Math.Max(0, config.MaxExtraSpinsPerDay - used);
            }
        }

        /// <summary>Extra (rewarded) spins unlock only after today's free spin is used.</summary>
        public bool CanExtraSpin => !FreeSpinAvailable && ExtraSpinsRemaining > 0;

        /// <summary>Returns the winning segment index, or -1 if no free spin is available.</summary>
        public int SpinFree()
        {
            if (!FreeSpinAvailable)
            {
                return -1;
            }

            save.Data.FreeSpinDate = DateKeys.Today(clock);
            return SpinAndGrant("spin_free");
        }

        /// <summary>Call only after the rewarded ad paid out.</summary>
        public int SpinExtra()
        {
            if (!CanExtraSpin)
            {
                return -1;
            }

            string today = DateKeys.Today(clock);
            if (save.Data.Ads.ExtraSpinsDate != today)
            {
                save.Data.Ads.ExtraSpinsDate = today;
                save.Data.Ads.ExtraSpinsToday = 0;
            }

            save.Data.Ads.ExtraSpinsToday++;
            return SpinAndGrant("spin_extra");
        }

        /// <summary>Weighted pick. Pure, so the odds can be unit-tested.</summary>
        public static int PickSegment(SpinSegment[] segments, float roll01)
        {
            float total = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                total += segments[i].Weight;
            }

            float target = roll01 * total;
            for (int i = 0; i < segments.Length; i++)
            {
                target -= segments[i].Weight;
                if (target < 0f)
                {
                    return i;
                }
            }

            return segments.Length - 1;
        }

        private int SpinAndGrant(string source)
        {
            int index = PickSegment(config.SpinSegments, rng.NextFloat());
            wallet.Grant(config.SpinSegments[index].Reward, source);
            save.MarkDirty();
            return index;
        }
    }
}
