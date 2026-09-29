using System.Collections.Generic;
using System;

namespace MoonPull.Save
{
    /// <summary>Upgrades old saves step by step and repairs structural damage (nulls, wrong array sizes, bad ranges).</summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data, int levelCount, int regionCount)
        {
            if (data.Version < 1)
            {
                // v0 (pre-release) stored no settings/ads blocks; Normalize creates them.
                data.Version = 1;
            }

            // Future: if (data.Version < 2) { ...; data.Version = 2; }
            return Normalize(data, levelCount, regionCount);
        }

        public static SaveData Normalize(SaveData data, int levelCount, int regionCount)
        {
            data.LevelStars = Resize(data.LevelStars, levelCount);
            data.LevelBestScores = Resize(data.LevelBestScores, levelCount);
            data.LighthouseStages = Resize(data.LighthouseStages, regionCount);
            data.OwnedBoats = data.OwnedBoats ?? new List<string>();
            data.Missions = data.Missions ?? new List<MissionProgress>();
            data.ProcessedTransactions = data.ProcessedTransactions ?? new List<string>();
            data.Settings = data.Settings ?? new SettingsData();
            data.Ads = data.Ads ?? new AdsStateData();
            data.SelectedBoatId = data.SelectedBoatId ?? string.Empty;
            data.MissionsDate = data.MissionsDate ?? string.Empty;
            data.FreeSpinDate = data.FreeSpinDate ?? string.Empty;
            data.StreakLastClaimDate = data.StreakLastClaimDate ?? string.Empty;
            data.Settings.Language = data.Settings.Language ?? string.Empty;
            data.Ads.ExtraSpinsDate = data.Ads.ExtraSpinsDate ?? string.Empty;

            data.Coins = Math.Max(0L, data.Coins);
            data.Keys = Math.Max(0, data.Keys);
            data.PendingBossChests = Math.Max(0, data.PendingBossChests);
            data.HighestUnlockedLevel = Clamp(data.HighestUnlockedLevel, 0, Math.Max(0, levelCount - 1));
            for (int i = 0; i < data.LevelStars.Length; i++)
            {
                data.LevelStars[i] = Clamp(data.LevelStars[i], 0, 3);
            }

            data.Missions.RemoveAll(m => m == null);
            return data;
        }

        private static int[] Resize(int[] source, int length)
        {
            if (source != null && source.Length == length)
            {
                return source;
            }

            var result = new int[length];
            if (source != null)
            {
                Array.Copy(source, result, Math.Min(source.Length, length));
            }

            return result;
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
