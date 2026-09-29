namespace MoonPull.Ads
{
    public enum AdFormat
    {
        Rewarded,
        Interstitial,
        AppOpen,
        Banner
    }

    /// <summary>Every place an ad can appear. Used for analytics and per-placement rules.</summary>
    public enum AdPlacement
    {
        RewindTide,
        TripleReward,
        DoubleIdle,
        ExtraSpin,
        OpenBossChest,
        TryBoat,
        LevelEndInterstitial,
        AppOpen,
        MenuBanner
    }

    public enum RewardedResult
    {
        Rewarded,
        ClosedEarly,
        Failed,
        NotReady
    }

    public readonly struct AdImpressionInfo
    {
        public readonly AdFormat Format;
        public readonly AdPlacement Placement;

        public AdImpressionInfo(AdFormat format, AdPlacement placement)
        {
            Format = format;
            Placement = placement;
        }
    }

    /// <summary>Impression-level revenue from the AdMob paid event. Value is in micros of <see cref="Currency"/>.</summary>
    public readonly struct AdPaidInfo
    {
        public readonly AdFormat Format;
        public readonly AdPlacement Placement;
        public readonly long ValueMicros;
        public readonly string Currency;
        public readonly int Precision;
        public readonly string AdUnitId;
        public readonly string AdSource;

        public AdPaidInfo(AdFormat format, AdPlacement placement, long valueMicros, string currency, int precision, string adUnitId, string adSource)
        {
            Format = format;
            Placement = placement;
            ValueMicros = valueMicros;
            Currency = currency;
            Precision = precision;
            AdUnitId = adUnitId;
            AdSource = adSource;
        }

        public double Value => ValueMicros / 1_000_000d;
    }
}
