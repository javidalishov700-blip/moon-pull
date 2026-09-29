using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Localization;

namespace MoonPull.UI
{
    /// <summary>Formatting helpers shared by screens. Allocates strings, so call on events, never per frame.</summary>
    public static class UiText
    {
        public static string Get(string key) => Services.TryGet(out ILocalizationService loc) ? loc.Get(key) : key;

        public static string Format(string key, params object[] args) =>
            Services.TryGet(out ILocalizationService loc) ? loc.Format(key, args) : key;

        public static string Number(long value) =>
            Services.TryGet(out ILocalizationService loc) ? loc.Number(value) : value.ToString();

        /// <summary>"05:12" under an hour, "3:05:12" otherwise. Digits read the same in every supported language.</summary>
        public static string Duration(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
            {
                span = TimeSpan.Zero;
            }

            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes:00}:{span.Seconds:00}";
        }

        public static string Reward(RewardEntry reward) =>
            Format(reward.Type == RewardType.Keys ? LocKeys.RewardKeys : LocKeys.RewardCoins, reward.Amount);

        public static string RarityKey(BoatRarity rarity)
        {
            switch (rarity)
            {
                case BoatRarity.Rare: return LocKeys.RarityRare;
                case BoatRarity.Epic: return LocKeys.RarityEpic;
                case BoatRarity.Legendary: return LocKeys.RarityLegendary;
                default: return LocKeys.RarityCommon;
            }
        }

        public static string Perk(BoatDefinition boat)
        {
            float v = boat.PerkValue;
            switch (boat.Perk)
            {
                case BoatPerkType.CoinBonusPercent: return Format(LocKeys.PerkCoinBonus, v);
                case BoatPerkType.CrashShields: return Format(LocKeys.PerkShields, (int)v);
                case BoatPerkType.LaunchBonusPercent: return Format(LocKeys.PerkLaunch, v);
                case BoatPerkType.NearMissWindowPercent: return Format(LocKeys.PerkNearMiss, v);
                case BoatPerkType.FullMoonBonusSeconds: return Format(LocKeys.PerkFullMoon, v);
                case BoatPerkType.StartingMoonstones: return Format(LocKeys.PerkMoonstones, (int)v);
                case BoatPerkType.PassengerBonusPercent: return Format(LocKeys.PerkPassengers, v);
                case BoatPerkType.PickupRadiusBonus: return Format(LocKeys.PerkMagnet, v);
                case BoatPerkType.IdleIncomeBonusPercent: return Format(LocKeys.PerkIdle, v);
                default: return Get(LocKeys.PerkNone);
            }
        }

        public static string FailReasonKey(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.Bridge: return LocKeys.FailBridge;
                case FailReason.Aground: return LocKeys.FailAground;
                case FailReason.Shark: return LocKeys.FailShark;
                case FailReason.Kraken: return LocKeys.FailKraken;
                default: return LocKeys.FailRock;
            }
        }
    }
}
