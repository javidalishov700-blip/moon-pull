using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;

namespace MoonPull.Meta
{
    /// <summary>7-day escalating rewards. Missing a day restarts at day 1; day 7 loops back to day 1.</summary>
    public sealed class LoginStreakService
    {
        public const int CycleLength = 7;

        private readonly ISaveService save;
        private readonly EconomyConfig config;
        private readonly IClock clock;
        private readonly Wallet wallet;

        public LoginStreakService(ISaveService save, EconomyConfig config, IClock clock, Wallet wallet)
        {
            this.save = save;
            this.config = config;
            this.clock = clock;
            this.wallet = wallet;
        }

        public bool CanClaim => save.Data.StreakLastClaimDate != DateKeys.Today(clock);

        /// <summary>1-based day that is claimable today, or the day already claimed today.</summary>
        public int CurrentDay => CanClaim
            ? NextDay(save.Data.StreakLastClaimDate, DateKeys.Yesterday(clock), save.Data.StreakDay)
            : save.Data.StreakDay;

        public RewardEntry RewardForDay(int day) => config.StreakRewards[(day - 1) % config.StreakRewards.Length];

        public bool Claim()
        {
            if (!CanClaim)
            {
                return false;
            }

            int day = CurrentDay;
            save.Data.StreakDay = day;
            save.Data.StreakLastClaimDate = DateKeys.Today(clock);
            wallet.Grant(RewardForDay(day), "login_streak");
            save.MarkDirty();
            return true;
        }

        /// <summary>Pure rule: continue the streak only if the last claim was yesterday.</summary>
        public static int NextDay(string lastClaimDate, string yesterday, int lastDay)
        {
            if (lastClaimDate != yesterday || lastDay <= 0)
            {
                return 1;
            }

            return lastDay % CycleLength + 1;
        }
    }
}
