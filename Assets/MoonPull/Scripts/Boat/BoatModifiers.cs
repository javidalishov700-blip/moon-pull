using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Boat
{
    /// <summary>Flattened perk values consumed by gameplay and economy. Pure data so economy tests need no assets.</summary>
    public readonly struct BoatModifiers
    {
        public readonly float CoinMultiplier;
        public readonly int Shields;
        public readonly float LaunchMultiplier;
        public readonly float NearMissWindowMultiplier;
        public readonly float FullMoonBonusSeconds;
        public readonly int StartingMoonstones;
        public readonly float PassengerCoinMultiplier;
        public readonly float PickupRadiusBonus;
        public readonly float IdleIncomeMultiplier;

        public BoatModifiers(float coinMultiplier, int shields, float launchMultiplier, float nearMissWindowMultiplier,
            float fullMoonBonusSeconds, int startingMoonstones, float passengerCoinMultiplier, float pickupRadiusBonus,
            float idleIncomeMultiplier)
        {
            CoinMultiplier = coinMultiplier;
            Shields = shields;
            LaunchMultiplier = launchMultiplier;
            NearMissWindowMultiplier = nearMissWindowMultiplier;
            FullMoonBonusSeconds = fullMoonBonusSeconds;
            StartingMoonstones = startingMoonstones;
            PassengerCoinMultiplier = passengerCoinMultiplier;
            PickupRadiusBonus = pickupRadiusBonus;
            IdleIncomeMultiplier = idleIncomeMultiplier;
        }

        public static BoatModifiers Default => new BoatModifiers(1f, 0, 1f, 1f, 0f, 0, 1f, 0f, 1f);

        public static BoatModifiers From(BoatPerkType perk, float value)
        {
            float percent = 1f + value / 100f;
            switch (perk)
            {
                case BoatPerkType.CoinBonusPercent: return new BoatModifiers(percent, 0, 1f, 1f, 0f, 0, 1f, 0f, 1f);
                case BoatPerkType.CrashShields: return new BoatModifiers(1f, Mathf.RoundToInt(value), 1f, 1f, 0f, 0, 1f, 0f, 1f);
                case BoatPerkType.LaunchBonusPercent: return new BoatModifiers(1f, 0, percent, 1f, 0f, 0, 1f, 0f, 1f);
                case BoatPerkType.NearMissWindowPercent: return new BoatModifiers(1f, 0, 1f, percent, 0f, 0, 1f, 0f, 1f);
                case BoatPerkType.FullMoonBonusSeconds: return new BoatModifiers(1f, 0, 1f, 1f, value, 0, 1f, 0f, 1f);
                case BoatPerkType.StartingMoonstones: return new BoatModifiers(1f, 0, 1f, 1f, 0f, Mathf.RoundToInt(value), 1f, 0f, 1f);
                case BoatPerkType.PassengerBonusPercent: return new BoatModifiers(1f, 0, 1f, 1f, 0f, 0, percent, 0f, 1f);
                case BoatPerkType.PickupRadiusBonus: return new BoatModifiers(1f, 0, 1f, 1f, 0f, 0, 1f, value, 1f);
                case BoatPerkType.IdleIncomeBonusPercent: return new BoatModifiers(1f, 0, 1f, 1f, 0f, 0, 1f, 0f, percent);
                default: return Default;
            }
        }

        public static BoatModifiers From(BoatDefinition definition) =>
            definition == null ? Default : From(definition.Perk, definition.PerkValue);
    }
}
