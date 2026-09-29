namespace MoonPull.Analytics
{
    /// <summary>Event and parameter names in one place. GA4 limits: 40 chars per name, 25 params per event.</summary>
    public static class AnalyticsEvents
    {
        public const string LevelStart = "level_start";
        public const string LevelComplete = "level_complete";
        public const string LevelFail = "level_fail";
        public const string NearMiss = "near_miss";
        public const string WaveLaunch = "wave_launch";
        public const string FullMoon = "full_moon";
        public const string PassengerDelivered = "passenger_delivered";
        public const string TreasureFound = "treasure_found";
        public const string RewindUsed = "rewind_used";
        public const string LighthouseStage = "lighthouse_stage";
        public const string IdleIncomeClaimed = "idle_income_claimed";
        public const string DailySpin = "daily_spin";
        public const string AdImpression = "ad_impression";
        public const string AdRevenue = "ad_revenue";
        public const string IapPurchase = "iap_purchase";
        public const string TutorialStep = "tutorial_step";
        public const string FunnelLevelPrefix = "funnel_level_";
        public const string EconomyEarn = "coins_earned";
        public const string EconomySpend = "coins_spent";

        public const string Level = "level";
        public const string Region = "region";
        public const string Boat = "boat";
        public const string Stars = "stars";
        public const string Score = "score";
        public const string Duration = "duration_sec";
        public const string Reason = "reason";
        public const string Progress = "progress_pct";
        public const string Chain = "chain";
        public const string Strength = "strength";
        public const string Count = "count";
        public const string Coins = "coins";
        public const string Stage = "stage";
        public const string Doubled = "doubled";
        public const string SpinType = "spin_type";
        public const string Reward = "reward";
        public const string AdFormat = "ad_format";
        public const string Placement = "placement";
        public const string AdSource = "ad_source";
        public const string AdUnit = "ad_unit";
        public const string Value = "value";
        public const string Currency = "currency";
        public const string Precision = "precision";
        public const string ProductId = "product_id";
        public const string Price = "price";
        public const string Step = "step";
        public const string Source = "source";
        public const string Amount = "amount";
        public const string UsedRewind = "used_rewind";
    }

    public static class RemoteConfigKeys
    {
        public const string InterstitialStartLevel = "interstitial_start_level";
        public const string InterstitialEveryLevels = "interstitial_every_levels";
        public const string FullscreenCooldownSeconds = "fullscreen_cooldown_seconds";
        public const string LifetimeGraceSeconds = "lifetime_grace_seconds";
        public const string AppOpenMinBackgroundSeconds = "app_open_min_background_seconds";
        public const string RewardedLevelMultiplier = "rewarded_level_multiplier";
        public const string RewardedIdleMultiplier = "rewarded_idle_multiplier";
        public const string DifficultyMultiplier = "difficulty_multiplier";
        public const string StarterPackIntroHours = "starter_pack_intro_hours";
    }
}
