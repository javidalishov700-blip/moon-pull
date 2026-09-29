using System;
using UnityEngine;

namespace MoonPull.Config
{
    public enum RewardType
    {
        Coins,
        Keys
    }

    [Serializable]
    public struct RewardEntry
    {
        public RewardType Type;
        [Min(0)] public int Amount;
    }

    [Serializable]
    public struct SpinSegment
    {
        public RewardEntry Reward;
        [Min(0f)] public float Weight;
        public Color Color;
    }

    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "MoonPull/Config/Economy")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Level rewards")]
        [SerializeField, Min(0)] private int baseLevelCoins = 20;
        [SerializeField, Min(0f)] private float coinsPerLevelIndex = 2f;
        [SerializeField, Min(0)] private int coinsPerStar = 10;
        [SerializeField, Min(0)] private int coinsPerPassenger = 15;
        [SerializeField, Min(0)] private int bossBonusCoins = 100;
        [Tooltip("Replaying a completed level pays this fraction of the base reward (pickups still pay in full).")]
        [SerializeField, Range(0f, 1f)] private float replayBaseFraction = 0.5f;
        [SerializeField, Min(1)] private int rewardedLevelMultiplier = 3;

        [Header("Idle income")]
        [SerializeField, Min(0.5f)] private float idleCapHours = 8f;
        [Tooltip("Minimum pending coins before the 'Collect your earnings' screen appears.")]
        [SerializeField, Min(0)] private int idleMinimumToShow = 10;
        [SerializeField, Min(1)] private int idleRewardedMultiplier = 2;

        [Header("Boss chest")]
        [SerializeField] private Vector2Int bossChestCoins = new Vector2Int(150, 300);
        [SerializeField, Min(0)] private int bossChestKeyCost = 1;

        [Header("Daily spin")]
        [SerializeField, Min(0)] private int maxExtraSpinsPerDay = 3;
        [SerializeField] private SpinSegment[] spinSegments =
        {
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 50 }, Weight = 30f, Color = new Color(0.55f, 0.75f, 1f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 100 }, Weight = 25f, Color = new Color(0.75f, 0.6f, 1f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 200 }, Weight = 15f, Color = new Color(0.55f, 0.75f, 1f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Keys, Amount = 1 }, Weight = 10f, Color = new Color(1f, 0.85f, 0.5f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 75 }, Weight = 25f, Color = new Color(0.75f, 0.6f, 1f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 500 }, Weight = 4f, Color = new Color(1f, 0.7f, 0.8f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Coins, Amount = 150 }, Weight = 15f, Color = new Color(0.55f, 0.75f, 1f) },
            new SpinSegment { Reward = new RewardEntry { Type = RewardType.Keys, Amount = 2 }, Weight = 3f, Color = new Color(1f, 0.85f, 0.5f) }
        };

        [Header("Login streak (7-day cycle)")]
        [SerializeField] private RewardEntry[] streakRewards =
        {
            new RewardEntry { Type = RewardType.Coins, Amount = 50 },
            new RewardEntry { Type = RewardType.Coins, Amount = 80 },
            new RewardEntry { Type = RewardType.Keys, Amount = 1 },
            new RewardEntry { Type = RewardType.Coins, Amount = 150 },
            new RewardEntry { Type = RewardType.Coins, Amount = 200 },
            new RewardEntry { Type = RewardType.Keys, Amount = 2 },
            new RewardEntry { Type = RewardType.Coins, Amount = 500 }
        };

        public int BaseLevelCoins => baseLevelCoins;
        public float CoinsPerLevelIndex => coinsPerLevelIndex;
        public int CoinsPerStar => coinsPerStar;
        public int CoinsPerPassenger => coinsPerPassenger;
        public int BossBonusCoins => bossBonusCoins;
        public float ReplayBaseFraction => replayBaseFraction;
        public int RewardedLevelMultiplier => rewardedLevelMultiplier;
        public float IdleCapHours => idleCapHours;
        public int IdleMinimumToShow => idleMinimumToShow;
        public int IdleRewardedMultiplier => idleRewardedMultiplier;
        public Vector2Int BossChestCoins => bossChestCoins;
        public int BossChestKeyCost => bossChestKeyCost;
        public int MaxExtraSpinsPerDay => maxExtraSpinsPerDay;
        public SpinSegment[] SpinSegments => spinSegments;
        public RewardEntry[] StreakRewards => streakRewards;
    }
}
