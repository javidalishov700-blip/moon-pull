using System;
using System.Collections.Generic;
using MoonPull.Core;

namespace MoonPull.Level
{
    public enum PlacementKind : byte
    {
        LowObstacle,
        HighObstacle,
        Star,
        Coin,
        Moonstone,
        Chest,
        Sandbar,
        Dock,
        Dolphin,
        Whale,
        Shark,
        KrakenSurface,
        Lighthouse,
        Harbor
    }

    [Flags]
    public enum PlacementFlags : byte
    {
        None = 0,
        Signature = 1,
        RequiresLaunch = 2,
        Intro = 4,
        DuringEclipse = 8
    }

    /// <summary>What water level a placement demands. Used to guarantee reaction time between opposite demands.</summary>
    public enum TideDemand : byte
    {
        None,
        High,
        Low,
        Band
    }

    public enum LevelSegment : byte
    {
        SingleLow,
        SingleHigh,
        ChainLowHigh,
        ChainHighLow,
        ChainHighLowHigh,
        ChainLowHighLow,
        Gate,
        LaunchRock,
        StarLine,
        CoinLine,
        Treasure,
        Dock,
        Dolphin,
        Whale,
        SharkZone,
        Eclipse,
        KrakenSurface
    }

    /// <summary>
    /// One thing placed on the lane. <see cref="Y"/> meaning depends on kind: obstacle top (Low), obstacle bottom
    /// (High), pickup centre, seabed top (Sandbar/Chest), dock deck.
    /// </summary>
    [Serializable]
    public struct LevelPlacement
    {
        public PlacementKind Kind;
        public float X;
        public float Width;
        public float Y;
        /// <summary>Normalized tide that solves this placement (min for Low, max for High, target for Dock).</summary>
        public float Tide01;
        /// <summary>Kind-specific integer: coins for Chest, passengers for Dock.</summary>
        public int Value;
        /// <summary>Prefab variant index within the region's set.</summary>
        public int Variant;
        public PlacementFlags Flags;

        public float MinX => X - Width * 0.5f;
        public float MaxX => X + Width * 0.5f;

        public bool Has(PlacementFlags flag) => (Flags & flag) != 0;

        public bool IsObstacle => Kind == PlacementKind.LowObstacle || Kind == PlacementKind.HighObstacle;
    }

    [Serializable]
    public struct WeatherEvent
    {
        public WeatherKind Kind;
        public float WarningX;
        public float StartX;
        public float EndX;
    }

    [Serializable]
    public struct ShallowSpan
    {
        public float StartX;
        public float EndX;
        public float Height;
    }

    /// <summary>Fully resolved, deterministic description of one level. Produced by <see cref="LevelGenerator"/>.</summary>
    public sealed class LevelPlan
    {
        public int LevelIndex;
        public int RegionIndex;
        public int Seed;
        public bool IsTutorial;
        public bool IsBoss;
        public float Difficulty;
        public float SpeedMultiplier;
        public float TargetDurationSeconds;
        public float StartX;
        public float HarborX;
        public float MinSwitchDistance;
        public int TwoStarScore;
        public int ThreeStarScore;
        public int KrakenHitsRequired;
        public LevelSegment? IntroducedSegment;
        public WeatherKind IntroducedWeather;

        public readonly List<LevelPlacement> Placements = new List<LevelPlacement>(128);
        public readonly List<WeatherEvent> Weather = new List<WeatherEvent>(2);
        public readonly List<ShallowSpan> Shallows = new List<ShallowSpan>(8);
        public readonly List<LevelSegment> Segments = new List<LevelSegment>(32);

        public int Count(PlacementKind kind)
        {
            int count = 0;
            for (int i = 0; i < Placements.Count; i++)
            {
                if (Placements[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
