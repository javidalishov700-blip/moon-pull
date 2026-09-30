using System;
using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Config
{
    /// <summary>All level-generation tuning. Easy/Hard pairs are lerped by the level's difficulty (0..1).</summary>
    [CreateAssetMenu(fileName = "LevelGenConfig", menuName = "MoonPull/Config/Level Generation")]
    public sealed class LevelGenConfig : ScriptableObject
    {
        [Serializable]
        public struct SegmentRule
        {
            public LevelSegment Segment;
            [Min(0f)] public float Weight;
            [Tooltip("0-based level index where this segment first appears (and is introduced).")]
            [Min(0)] public int UnlockLevelIndex;
        }

        [Header("Structure")]
        [SerializeField, Min(1)] private int levelsPerRegion = 20;
        [SerializeField, Min(1)] private int regionCount = 5;
        [SerializeField, Min(2)] private int bossInterval = 5;
        [Tooltip("Share of difficulty coming from position inside the region (sawtooth), the rest from overall progress.")]
        [SerializeField, Range(0f, 1f)] private float regionLocalDifficultyWeight = 0.35f;
        [SerializeField] private int seedSalt = 7919;

        [Header("Duration & speed")]
        [SerializeField, Min(5f)] private float minDurationSeconds = 25f;
        [SerializeField, Min(5f)] private float maxDurationSeconds = 45f;
        [SerializeField, Min(1f)] private float maxSpeedMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float startClearDistance = 9f;
        [SerializeField, Min(0f)] private float endClearDistance = 7f;

        [Header("Spacing (seconds of travel)")]
        [Tooltip("Travel time between obstacles that need opposite tides.")]
        [SerializeField] private Vector2 switchSeconds = new Vector2(1.15f, 0.7f);
        [SerializeField] private Vector2 sameDemandSeconds = new Vector2(0.9f, 0.45f);
        [SerializeField] private Vector2 restSeconds = new Vector2(1.6f, 0.7f);

        [Header("Obstacles (normalized tide)")]
        [Tooltip("Min tide needed to clear a low obstacle: x = easy range, y = hard range. Each Vector2 is (min, max).")]
        [SerializeField] private Vector2 lowRequiredEasy = new Vector2(0.3f, 0.55f);
        [SerializeField] private Vector2 lowRequiredHard = new Vector2(0.55f, 0.85f);
        [SerializeField] private Vector2 highAllowedEasy = new Vector2(0.5f, 0.7f);
        [SerializeField] private Vector2 highAllowedHard = new Vector2(0.15f, 0.4f);
        [SerializeField] private Vector2 gateBand = new Vector2(0.32f, 0.18f);
        [SerializeField] private Vector2 obstacleWidth = new Vector2(1.2f, 2.4f);
        [SerializeField, Range(0f, 1f)] private float signatureChance = 0.25f;

        [Header("Wave launch rock")]
        [SerializeField, Min(0f)] private float launchRockExtraHeight = 0.35f;
        [SerializeField, Min(0.1f)] private float launchRockWidth = 1.1f;
        [SerializeField, Min(0)] private int launchArcStars = 3;
        [Tooltip("Free travel time before and after a launch rock or Kraken so the player can launch and land.")]
        [SerializeField, Min(0f)] private float launchLeadSeconds = 0.6f;

        [Header("Pickups")]
        [SerializeField, Min(1)] private int starLineCount = 5;
        [SerializeField, Min(0.1f)] private float starSpacing = 1.1f;
        [SerializeField, Min(1)] private int coinLineCount = 6;
        [SerializeField, Min(0.1f)] private float coinSpacing = 0.9f;
        [SerializeField] private Vector2 pickupLineTide = new Vector2(0.25f, 0.8f);
        [Tooltip("Extra moonstones above the Full Moon requirement (easy, hard).")]
        [SerializeField] private Vector2Int spareMoonstones = new Vector2Int(2, 0);

        [Header("Treasure")]
        [SerializeField] private Vector2 shallowLength = new Vector2(4f, 6f);
        [Tooltip("Below this normalized tide the boat runs aground on a treasure bed.")]
        [SerializeField, Range(0f, 0.5f)] private float treasureDangerTide = 0.12f;
        [SerializeField] private Vector2Int chestCoins = new Vector2Int(30, 80);

        [Header("Depth layers")]
        [SerializeField, Min(0.5f)] private float dockLength = 3.5f;
        [SerializeField] private Vector2 dockTide = new Vector2(0.3f, 0.7f);
        [SerializeField] private Vector2Int passengersPerDock = new Vector2Int(1, 3);
        [SerializeField, Min(0.5f)] private float sharkZoneLength = 6f;
        [SerializeField, Range(0f, 1f)] private float sharkDangerTide = 0.28f;
        [SerializeField, Range(0f, 1f)] private float whaleSurfaceTide = 0.6f;
        [SerializeField, Min(0.5f)] private float creatureWidth = 2.5f;

        [Header("Weather")]
        [SerializeField, Range(0f, 1f)] private float weatherChance = 0.6f;
        [SerializeField, Min(0f)] private float weatherWarningSeconds = 2f;
        [SerializeField, Min(0.5f)] private float stormSeconds = 8f;
        [SerializeField, Min(0.5f)] private float fogSeconds = 10f;
        [SerializeField, Min(0.5f)] private float eclipseSeconds = 3f;
        [SerializeField] private int stormUnlockLevelIndex = 10;
        [SerializeField] private int fogUnlockLevelIndex = 11;
        [SerializeField] private int eclipseUnlockLevelIndex = 12;

        [Header("Features")]
        [SerializeField] private int moonstoneUnlockLevelIndex = 1;
        [SerializeField, Min(1)] private int moonstonesForFullMoon = 5;

        [Header("Boss")]
        [SerializeField, Min(1)] private int krakenSurfaceCount = 6;
        [SerializeField, Min(1)] private int krakenHitsRequired = 3;
        [SerializeField, Min(0.5f)] private float krakenWidth = 3f;
        [SerializeField, Range(0f, 1f)] private float bossDifficultyRelief = 0.2f;

        [Header("Star rating (fraction of estimated max score)")]
        [SerializeField, Range(0f, 1f)] private float twoStarFraction = 0.45f;
        [SerializeField, Range(0f, 1f)] private float threeStarFraction = 0.75f;

        [Header("Segments")]
        [SerializeField] private SegmentRule[] segments =
        {
            new SegmentRule { Segment = LevelSegment.SingleLow, Weight = 3f, UnlockLevelIndex = 0 },
            new SegmentRule { Segment = LevelSegment.SingleHigh, Weight = 3f, UnlockLevelIndex = 0 },
            new SegmentRule { Segment = LevelSegment.StarLine, Weight = 1.5f, UnlockLevelIndex = 0 },
            new SegmentRule { Segment = LevelSegment.CoinLine, Weight = 1.2f, UnlockLevelIndex = 0 },
            new SegmentRule { Segment = LevelSegment.ChainLowHigh, Weight = 2f, UnlockLevelIndex = 1 },
            new SegmentRule { Segment = LevelSegment.ChainHighLow, Weight = 2f, UnlockLevelIndex = 1 },
            new SegmentRule { Segment = LevelSegment.LaunchRock, Weight = 0f, UnlockLevelIndex = int.MaxValue }, // tide-only design
            new SegmentRule { Segment = LevelSegment.Treasure, Weight = 1f, UnlockLevelIndex = 3 },
            new SegmentRule { Segment = LevelSegment.ChainHighLowHigh, Weight = 1.5f, UnlockLevelIndex = 3 },
            new SegmentRule { Segment = LevelSegment.ChainLowHighLow, Weight = 1.5f, UnlockLevelIndex = 3 },
            new SegmentRule { Segment = LevelSegment.Dock, Weight = 1.2f, UnlockLevelIndex = 5 },
            new SegmentRule { Segment = LevelSegment.Gate, Weight = 1.2f, UnlockLevelIndex = 6 },
            new SegmentRule { Segment = LevelSegment.Dolphin, Weight = 0.8f, UnlockLevelIndex = 15 },
            new SegmentRule { Segment = LevelSegment.Whale, Weight = 0.8f, UnlockLevelIndex = 16 },
            new SegmentRule { Segment = LevelSegment.SharkZone, Weight = 1f, UnlockLevelIndex = 17 }
        };

        [Header("Tutorial (level index 0)")]
        [SerializeField] private LevelSegment[] tutorialSequence =
        {
            LevelSegment.SingleLow, LevelSegment.StarLine, LevelSegment.SingleHigh, LevelSegment.CoinLine,
            LevelSegment.SingleLow, LevelSegment.SingleHigh, LevelSegment.ChainLowHigh, LevelSegment.StarLine
        };

        public int LevelsPerRegion => levelsPerRegion;
        public int RegionCount => regionCount;
        public int TotalLevels => levelsPerRegion * regionCount;
        public int BossInterval => bossInterval;
        public float RegionLocalDifficultyWeight => regionLocalDifficultyWeight;
        public int SeedSalt => seedSalt;
        public float MinDurationSeconds => minDurationSeconds;
        public float MaxDurationSeconds => maxDurationSeconds;
        public float MaxSpeedMultiplier => maxSpeedMultiplier;
        public float StartClearDistance => startClearDistance;
        public float EndClearDistance => endClearDistance;
        public Vector2 SwitchSeconds => switchSeconds;
        public Vector2 SameDemandSeconds => sameDemandSeconds;
        public Vector2 RestSeconds => restSeconds;
        public Vector2 LowRequiredEasy => lowRequiredEasy;
        public Vector2 LowRequiredHard => lowRequiredHard;
        public Vector2 HighAllowedEasy => highAllowedEasy;
        public Vector2 HighAllowedHard => highAllowedHard;
        public Vector2 GateBand => gateBand;
        public Vector2 ObstacleWidth => obstacleWidth;
        public float SignatureChance => signatureChance;
        public float LaunchRockExtraHeight => launchRockExtraHeight;
        public float LaunchRockWidth => launchRockWidth;
        public int LaunchArcStars => launchArcStars;
        public float LaunchLeadSeconds => launchLeadSeconds;
        public int StarLineCount => starLineCount;
        public float StarSpacing => starSpacing;
        public int CoinLineCount => coinLineCount;
        public float CoinSpacing => coinSpacing;
        public Vector2 PickupLineTide => pickupLineTide;
        public Vector2Int SpareMoonstones => spareMoonstones;
        public Vector2 ShallowLength => shallowLength;
        public float TreasureDangerTide => treasureDangerTide;
        public Vector2Int ChestCoins => chestCoins;
        public float DockLength => dockLength;
        public Vector2 DockTide => dockTide;
        public Vector2Int PassengersPerDock => passengersPerDock;
        public float SharkZoneLength => sharkZoneLength;
        public float SharkDangerTide => sharkDangerTide;
        public float WhaleSurfaceTide => whaleSurfaceTide;
        public float CreatureWidth => creatureWidth;
        public float WeatherChance => weatherChance;
        public float WeatherWarningSeconds => weatherWarningSeconds;
        public float StormSeconds => stormSeconds;
        public float FogSeconds => fogSeconds;
        public float EclipseSeconds => eclipseSeconds;
        public int StormUnlockLevelIndex => stormUnlockLevelIndex;
        public int FogUnlockLevelIndex => fogUnlockLevelIndex;
        public int EclipseUnlockLevelIndex => eclipseUnlockLevelIndex;
        public int MoonstoneUnlockLevelIndex => moonstoneUnlockLevelIndex;
        public int MoonstonesForFullMoon => moonstonesForFullMoon;
        public int KrakenSurfaceCount => krakenSurfaceCount;
        public int KrakenHitsRequired => krakenHitsRequired;
        public float KrakenWidth => krakenWidth;
        public float BossDifficultyRelief => bossDifficultyRelief;
        public float TwoStarFraction => twoStarFraction;
        public float ThreeStarFraction => threeStarFraction;
        public SegmentRule[] Segments => segments;
        public LevelSegment[] TutorialSequence => tutorialSequence;

        public bool IsBossLevel(int levelIndex) => (levelIndex + 1) % bossInterval == 0;
    }
}
