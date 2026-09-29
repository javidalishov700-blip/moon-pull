using System;
using System.Collections.Generic;

namespace MoonPull.Save
{
    [Serializable]
    public sealed class MissionProgress
    {
        public string MissionId = string.Empty;
        public int Progress;
        public bool Claimed;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public bool MusicOn = true;
        public bool SfxOn = true;
        public bool HapticsOn = true;
        /// <summary>Locale code (e.g. "tr"). Empty = follow device language.</summary>
        public string Language = string.Empty;
    }

    [Serializable]
    public sealed class AdsStateData
    {
        public long FirstLaunchUtcTicks;
        public double LifetimePlaySeconds;
        public long LastFullscreenAdUtcTicks;
        public long LastRewardedAdUtcTicks;
        public int LevelsSinceInterstitial;
        public bool SkipNextInterstitial;
        public int SessionCount;
        public string ExtraSpinsDate = string.Empty;
        public int ExtraSpinsToday;
    }

    /// <summary>
    /// Entire persistent state. Plain fields only so JsonUtility can serialize it. Bump <see cref="CurrentVersion"/>
    /// and add a step to <see cref="SaveMigrator"/> whenever the shape changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        // Economy
        public long Coins;
        public int Keys;
        public int PendingBossChests;

        // Progress
        public int HighestUnlockedLevel;
        public int[] LevelStars = Array.Empty<int>();
        public int[] LevelBestScores = Array.Empty<int>();
        public int TotalLevelsCompleted;
        public int TutorialFlags;

        // Boats
        public string SelectedBoatId = string.Empty;
        public List<string> OwnedBoats = new List<string>();

        // Lighthouses & idle income
        public int[] LighthouseStages = Array.Empty<int>();
        public long LastIdleCollectUtcTicks;

        // Dailies
        public string MissionsDate = string.Empty;
        public List<MissionProgress> Missions = new List<MissionProgress>();
        public string FreeSpinDate = string.Empty;
        public string StreakLastClaimDate = string.Empty;
        public int StreakDay;

        // Purchases
        public bool RemoveAds;
        public bool StarterPackPurchased;
        public List<string> ProcessedTransactions = new List<string>();

        public SettingsData Settings = new SettingsData();
        public AdsStateData Ads = new AdsStateData();
    }
}
