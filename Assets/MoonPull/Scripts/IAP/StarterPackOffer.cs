using System;
using MoonPull.Save;

namespace MoonPull.IAP
{
    /// <summary>Pure rules for the time-limited starter pack discount, measured from the player's first launch.</summary>
    public static class StarterPackOffer
    {
        public static bool IsAvailable(SaveData data) => !data.StarterPackPurchased;

        public static TimeSpan IntroRemaining(SaveData data, DateTime nowUtc, float introHours)
        {
            if (data.Ads.FirstLaunchUtcTicks <= 0)
            {
                return TimeSpan.Zero;
            }

            DateTime ends = new DateTime(data.Ads.FirstLaunchUtcTicks, DateTimeKind.Utc).AddHours(introHours);
            TimeSpan remaining = ends - nowUtc;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public static bool IsIntroActive(SaveData data, DateTime nowUtc, float introHours) =>
            IsAvailable(data) && IntroRemaining(data, nowUtc, introHours) > TimeSpan.Zero;
    }
}
