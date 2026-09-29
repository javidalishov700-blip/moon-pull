using System.Collections.Generic;
using MoonPull.Config;
using MoonPull.Core;
using UnityEngine;

namespace MoonPull.Level
{
    /// <summary>
    /// Pure, deterministic level generator. The same level index always yields the same layout, so players can learn
    /// and replay levels for stars. Spacing rules are enforced while placing, so every layout is solvable by construction.
    /// </summary>
    public static class LevelGenerator
    {
        public static int SeedFor(int levelIndex, int salt) => unchecked(((levelIndex + 1) * 73856093) ^ (salt * 19349663));

        /// <summary>0..1 difficulty mixing overall progress with a per-region sawtooth, scaled by remote config.</summary>
        public static float DifficultyFor(LevelGenConfig config, int levelIndex, float scale)
        {
            int total = Mathf.Max(2, config.TotalLevels);
            int perRegion = Mathf.Max(2, config.LevelsPerRegion);
            float global = Mathf.Clamp01(levelIndex / (float)(total - 1));
            float local = (levelIndex % perRegion) / (float)(perRegion - 1);
            float difficulty = Mathf.Lerp(global, local, config.RegionLocalDifficultyWeight);
            if (config.IsBossLevel(levelIndex))
            {
                difficulty -= config.BossDifficultyRelief;
            }

            return Mathf.Clamp01(difficulty * Mathf.Max(0f, scale));
        }

        public static LevelPlan Generate(LevelGenConfig config, ScoreConfig score, in LevelGenContext context, int levelIndex)
        {
            var plan = new LevelPlan();
            Generate(config, score, context, levelIndex, plan);
            return plan;
        }

        /// <summary>Fills <paramref name="plan"/> in place so lists are reused between levels.</summary>
        public static void Generate(LevelGenConfig config, ScoreConfig score, in LevelGenContext context, int levelIndex, LevelPlan plan)
        {
            new Builder(config, score, context, levelIndex, plan).Build();
        }

        private sealed class Builder
        {
            private readonly LevelGenConfig cfg;
            private readonly ScoreConfig score;
            private readonly LevelGenContext ctx;
            private readonly LevelPlan plan;
            private readonly int levelIndex;
            private readonly List<LevelSegment> introQueue = new List<LevelSegment>(4);
            private readonly List<float> krakenSchedule = new List<float>(8);

            private SeededRandom rng;
            private float d;
            private float speed;
            private float cursor;
            private float endLimit;
            private float minSwitch;
            private float sameGap;
            private float rest;
            private float lead;
            private TideDemand lastDemand;
            private float lastDemandMaxX;
            private PlacementFlags segmentFlags;
            private int krakenPlaced;

            public Builder(LevelGenConfig cfg, ScoreConfig score, in LevelGenContext ctx, int levelIndex, LevelPlan plan)
            {
                this.cfg = cfg;
                this.score = score;
                this.ctx = ctx;
                this.levelIndex = levelIndex;
                this.plan = plan;
            }

            public void Build()
            {
                Setup();
                WeatherKind weather = ChooseWeather();

                if (plan.IsTutorial)
                {
                    LevelSegment[] sequence = cfg.TutorialSequence;
                    for (int i = 0; i < sequence.Length; i++)
                    {
                        segmentFlags = i == 0 ? PlacementFlags.Intro : PlacementFlags.None;
                        EmitSegment(sequence[i]);
                        cursor += rest;
                    }

                    segmentFlags = PlacementFlags.None;
                }
                else
                {
                    LayoutSegments(weather);
                }

                if (plan.IsBoss)
                {
                    while (krakenPlaced < cfg.KrakenHitsRequired)
                    {
                        EmitSegment(LevelSegment.KrakenSurface);
                        cursor += rest;
                    }
                }

                if (levelIndex >= cfg.MoonstoneUnlockLevelIndex)
                {
                    PlaceMoonstones();
                }

                FinalizeHarbor();
                PlaceRangeWeather(weather);
                plan.Placements.Sort(CompareByX);
                ComputeStarThresholds();
            }

            private void Setup()
            {
                plan.Placements.Clear();
                plan.Weather.Clear();
                plan.Shallows.Clear();
                plan.Segments.Clear();

                plan.LevelIndex = levelIndex;
                plan.RegionIndex = Mathf.Min(levelIndex / Mathf.Max(1, cfg.LevelsPerRegion), cfg.RegionCount - 1);
                plan.Seed = SeedFor(levelIndex, cfg.SeedSalt);
                plan.IsTutorial = levelIndex == 0;
                plan.IsBoss = !plan.IsTutorial && cfg.IsBossLevel(levelIndex);
                plan.KrakenHitsRequired = plan.IsBoss ? cfg.KrakenHitsRequired : 0;
                plan.IntroducedSegment = null;
                plan.IntroducedWeather = WeatherKind.None;

                rng = new SeededRandom(plan.Seed);
                d = plan.IsTutorial ? 0f : DifficultyFor(cfg, levelIndex, ctx.DifficultyScale <= 0f ? 1f : ctx.DifficultyScale);
                plan.Difficulty = d;

                plan.SpeedMultiplier = Mathf.Lerp(1f, cfg.MaxSpeedMultiplier, d);
                speed = ctx.BaseSpeed * plan.SpeedMultiplier;
                plan.TargetDurationSeconds = Mathf.Lerp(cfg.MinDurationSeconds, cfg.MaxDurationSeconds, d);

                plan.StartX = 0f;
                plan.HarborX = plan.StartX + plan.TargetDurationSeconds * speed;
                endLimit = plan.HarborX - cfg.EndClearDistance;
                cursor = plan.StartX + cfg.StartClearDistance;

                minSwitch = speed * Lerp(cfg.SwitchSeconds);
                sameGap = speed * Lerp(cfg.SameDemandSeconds);
                rest = speed * Lerp(cfg.RestSeconds);
                lead = speed * cfg.LaunchLeadSeconds;
                plan.MinSwitchDistance = minSwitch;

                lastDemand = TideDemand.None;
                lastDemandMaxX = float.MinValue;
                segmentFlags = PlacementFlags.None;
                krakenPlaced = 0;

                introQueue.Clear();
                LevelGenConfig.SegmentRule[] rules = cfg.Segments;
                for (int i = 0; i < rules.Length; i++)
                {
                    if (rules[i].UnlockLevelIndex == levelIndex && rules[i].Weight > 0f && levelIndex > 0)
                    {
                        // Introduce twice: once to teach, once to confirm.
                        introQueue.Add(rules[i].Segment);
                        introQueue.Add(rules[i].Segment);
                        if (!plan.IntroducedSegment.HasValue)
                        {
                            plan.IntroducedSegment = rules[i].Segment;
                        }
                    }
                }

                krakenSchedule.Clear();
                if (plan.IsBoss)
                {
                    int count = cfg.KrakenSurfaceCount;
                    float span = endLimit - cursor;
                    for (int i = 0; i < count; i++)
                    {
                        krakenSchedule.Add(cursor + span * (i + 0.5f) / count);
                    }
                }
            }

            private WeatherKind ChooseWeather()
            {
                if (plan.IsTutorial || plan.IsBoss)
                {
                    return WeatherKind.None;
                }

                if (levelIndex == cfg.StormUnlockLevelIndex) return Introduce(WeatherKind.Storm);
                if (levelIndex == cfg.FogUnlockLevelIndex) return Introduce(WeatherKind.Fog);
                if (levelIndex == cfg.EclipseUnlockLevelIndex) return Introduce(WeatherKind.Eclipse);

                if (!rng.Chance(cfg.WeatherChance))
                {
                    return WeatherKind.None;
                }

                int available = 0;
                if (levelIndex >= cfg.StormUnlockLevelIndex) available++;
                if (levelIndex >= cfg.FogUnlockLevelIndex) available++;
                if (levelIndex >= cfg.EclipseUnlockLevelIndex) available++;
                if (available == 0)
                {
                    return WeatherKind.None;
                }

                switch (rng.Range(0, available))
                {
                    case 0: return WeatherKind.Storm;
                    case 1: return WeatherKind.Fog;
                    default: return WeatherKind.Eclipse;
                }
            }

            private WeatherKind Introduce(WeatherKind kind)
            {
                plan.IntroducedWeather = kind;
                return kind;
            }

            private void LayoutSegments(WeatherKind weather)
            {
                bool eclipsePending = weather == WeatherKind.Eclipse;
                float eclipseAfter = plan.StartX + (plan.HarborX - plan.StartX) * 0.4f;
                int krakenIndex = 0;
                int guard = 0;

                while (cursor < endLimit && guard++ < 256)
                {
                    LevelSegment segment;
                    if (krakenIndex < krakenSchedule.Count && cursor >= krakenSchedule[krakenIndex])
                    {
                        segment = LevelSegment.KrakenSurface;
                        krakenIndex++;
                    }
                    else if (introQueue.Count > 0)
                    {
                        segment = introQueue[0];
                        introQueue.RemoveAt(0);
                        segmentFlags = PlacementFlags.Intro;
                    }
                    else if (eclipsePending && cursor >= eclipseAfter)
                    {
                        segment = LevelSegment.Eclipse;
                        eclipsePending = false;
                    }
                    else
                    {
                        segment = PickWeighted();
                    }

                    if (cursor + EstimateLength(segment) > endLimit)
                    {
                        if (segment != LevelSegment.KrakenSurface && segment != LevelSegment.Eclipse)
                        {
                            segmentFlags = PlacementFlags.None;
                            break;
                        }
                    }

                    EmitSegment(segment);
                    segmentFlags = PlacementFlags.None;
                    cursor += rest;
                }
            }

            private LevelSegment PickWeighted()
            {
                LevelGenConfig.SegmentRule[] rules = cfg.Segments;
                float total = 0f;
                for (int i = 0; i < rules.Length; i++)
                {
                    if (IsAllowed(rules[i]))
                    {
                        total += rules[i].Weight;
                    }
                }

                float roll = rng.NextFloat() * total;
                for (int i = 0; i < rules.Length; i++)
                {
                    if (!IsAllowed(rules[i]))
                    {
                        continue;
                    }

                    roll -= rules[i].Weight;
                    if (roll <= 0f)
                    {
                        return rules[i].Segment;
                    }
                }

                return LevelSegment.SingleLow;
            }

            private bool IsAllowed(LevelGenConfig.SegmentRule rule) =>
                rule.Weight > 0f && levelIndex >= rule.UnlockLevelIndex;

            private float EstimateLength(LevelSegment segment)
            {
                float obstacle = minSwitch + cfg.ObstacleWidth.y;
                switch (segment)
                {
                    case LevelSegment.ChainLowHigh:
                    case LevelSegment.ChainHighLow:
                        return obstacle * 2f;
                    case LevelSegment.ChainHighLowHigh:
                    case LevelSegment.ChainLowHighLow:
                        return obstacle * 3f;
                    case LevelSegment.LaunchRock:
                        return minSwitch + lead * 2f + cfg.LaunchRockWidth;
                    case LevelSegment.StarLine:
                        return cfg.StarLineCount * cfg.StarSpacing;
                    case LevelSegment.CoinLine:
                        return cfg.CoinLineCount * cfg.CoinSpacing;
                    case LevelSegment.Treasure:
                        return minSwitch + cfg.ShallowLength.y;
                    case LevelSegment.Dock:
                        return minSwitch + cfg.DockLength;
                    case LevelSegment.Dolphin:
                    case LevelSegment.Whale:
                        return cfg.CreatureWidth + cfg.StarSpacing * 4f;
                    case LevelSegment.SharkZone:
                        return minSwitch + cfg.SharkZoneLength;
                    case LevelSegment.Eclipse:
                        return minSwitch + speed * (cfg.WeatherWarningSeconds + cfg.EclipseSeconds);
                    case LevelSegment.KrakenSurface:
                        return lead * 2f + cfg.KrakenWidth;
                    default:
                        return obstacle;
                }
            }

            private void EmitSegment(LevelSegment segment)
            {
                plan.Segments.Add(segment);
                switch (segment)
                {
                    case LevelSegment.SingleLow:
                        EmitLow(RandomLowRequired(), RandomWidth());
                        break;
                    case LevelSegment.SingleHigh:
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        break;
                    case LevelSegment.ChainLowHigh:
                        EmitLow(RandomLowRequired(), RandomWidth());
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        break;
                    case LevelSegment.ChainHighLow:
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        EmitLow(RandomLowRequired(), RandomWidth());
                        break;
                    case LevelSegment.ChainHighLowHigh:
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        EmitLow(RandomLowRequired(), RandomWidth());
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        break;
                    case LevelSegment.ChainLowHighLow:
                        EmitLow(RandomLowRequired(), RandomWidth());
                        EmitHigh(RandomHighAllowed(), RandomWidth());
                        EmitLow(RandomLowRequired(), RandomWidth());
                        break;
                    case LevelSegment.Gate:
                        EmitGate();
                        break;
                    case LevelSegment.LaunchRock:
                        EmitLaunchRock();
                        break;
                    case LevelSegment.StarLine:
                        EmitLine(PlacementKind.Star, cfg.StarLineCount, cfg.StarSpacing, rng.Range(cfg.PickupLineTide.x, cfg.PickupLineTide.y));
                        break;
                    case LevelSegment.CoinLine:
                        EmitLine(PlacementKind.Coin, cfg.CoinLineCount, cfg.CoinSpacing, rng.Range(cfg.PickupLineTide.x, cfg.PickupLineTide.y));
                        break;
                    case LevelSegment.Treasure:
                        EmitTreasure();
                        break;
                    case LevelSegment.Dock:
                        EmitDock();
                        break;
                    case LevelSegment.Dolphin:
                        EmitDolphin();
                        break;
                    case LevelSegment.Whale:
                        EmitWhale();
                        break;
                    case LevelSegment.SharkZone:
                        EmitShark();
                        break;
                    case LevelSegment.Eclipse:
                        EmitEclipse();
                        break;
                    case LevelSegment.KrakenSurface:
                        EmitKraken();
                        break;
                }
            }

            // Returns the centre X of a new element, pushed forward until reaction-time rules hold.
            private float Reserve(TideDemand demand, float width)
            {
                float minX = cursor;
                if (demand != TideDemand.None && lastDemand != TideDemand.None)
                {
                    bool conflicting = demand != lastDemand || demand == TideDemand.Band;
                    minX = Mathf.Max(minX, lastDemandMaxX + (conflicting ? minSwitch : sameGap));
                }

                float center = minX + width * 0.5f;
                cursor = center + width * 0.5f;
                if (demand != TideDemand.None)
                {
                    lastDemand = demand;
                    lastDemandMaxX = cursor;
                }

                return center;
            }

            private void EmitLow(float required01, float width, PlacementFlags extra = PlacementFlags.None)
            {
                float x = Reserve(TideDemand.High, width);
                AddObstacle(PlacementKind.LowObstacle, x, width, ctx.Level(required01) - ctx.HullBottomOffset, required01, extra);
            }

            private void EmitHigh(float allowed01, float width, PlacementFlags extra = PlacementFlags.None)
            {
                float x = Reserve(TideDemand.Low, width);
                AddObstacle(PlacementKind.HighObstacle, x, width, ctx.Level(allowed01) + ctx.MastTopOffset, allowed01, extra);
            }

            private void AddObstacle(PlacementKind kind, float x, float width, float y, float tide01, PlacementFlags extra)
            {
                PlacementFlags flags = segmentFlags | extra;
                int variantCount = kind == PlacementKind.LowObstacle ? ctx.LowVariantCount : ctx.HighVariantCount;
                if (ctx.HasSignature && ctx.SignatureKind == kind && rng.Chance(cfg.SignatureChance))
                {
                    flags |= PlacementFlags.Signature;
                }

                Add(new LevelPlacement
                {
                    Kind = kind,
                    X = x,
                    Width = width,
                    Y = y,
                    Tide01 = tide01,
                    Variant = variantCount > 0 ? rng.Range(0, variantCount) : 0,
                    Flags = flags
                });
            }

            private void EmitGate()
            {
                float band = Lerp(cfg.GateBand);
                float low = rng.Range(0.25f, 0.95f - band);
                float width = RandomWidth();
                float x = Reserve(TideDemand.Band, width);
                AddObstacle(PlacementKind.LowObstacle, x, width, ctx.Level(low) - ctx.HullBottomOffset, low, PlacementFlags.None);
                AddObstacle(PlacementKind.HighObstacle, x, width, ctx.Level(low + band) + ctx.MastTopOffset, low + band, PlacementFlags.None);
            }

            private void EmitLaunchRock()
            {
                cursor += lead;
                float width = cfg.LaunchRockWidth;
                float x = Reserve(TideDemand.High, width);
                float top = ctx.Level(1f) - ctx.HullBottomOffset + cfg.LaunchRockExtraHeight;
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.LowObstacle,
                    X = x,
                    Width = width,
                    Y = top,
                    Tide01 = 1f,
                    Variant = ctx.LowVariantCount > 0 ? rng.Range(0, ctx.LowVariantCount) : 0,
                    Flags = segmentFlags | PlacementFlags.RequiresLaunch
                });

                EmitArc(x, top + ctx.HullBottomOffset + 0.4f, cfg.LaunchArcStars);
                cursor += lead;
            }

            private void EmitArc(float centerX, float apexY, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = count == 1 ? 0f : i / (float)(count - 1) * 2f - 1f;
                    Add(new LevelPlacement
                    {
                        Kind = PlacementKind.Star,
                        X = centerX + t * cfg.StarSpacing,
                        Width = 0f,
                        Y = apexY - t * t * 0.5f,
                        Tide01 = 1f,
                        Flags = segmentFlags
                    });
                }
            }

            private void EmitLine(PlacementKind kind, int count, float spacing, float tide01)
            {
                float length = count * spacing;
                float start = Reserve(TideDemand.None, length) - length * 0.5f;
                float y = ctx.Level(tide01);
                for (int i = 0; i < count; i++)
                {
                    Add(new LevelPlacement
                    {
                        Kind = kind,
                        X = start + (i + 0.5f) * spacing,
                        Y = y,
                        Tide01 = tide01,
                        Flags = segmentFlags
                    });
                }
            }

            private void EmitTreasure()
            {
                float length = rng.Range(cfg.ShallowLength.x, cfg.ShallowLength.y);
                float x = Reserve(TideDemand.Low, length);
                float bedTop = ctx.Level(cfg.TreasureDangerTide) - ctx.Draft;
                plan.Shallows.Add(new ShallowSpan { StartX = x - length * 0.5f, EndX = x + length * 0.5f, Height = bedTop });
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Sandbar,
                    X = x,
                    Width = length,
                    Y = bedTop,
                    Tide01 = cfg.TreasureDangerTide
                });
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Chest,
                    X = x,
                    Width = 0.8f,
                    Y = bedTop,
                    Tide01 = cfg.TreasureDangerTide,
                    Value = rng.Range(cfg.ChestCoins.x, cfg.ChestCoins.y + 1),
                    Flags = segmentFlags
                });
            }

            private void EmitDock()
            {
                float tide = rng.Range(cfg.DockTide.x, cfg.DockTide.y);
                float x = Reserve(TideDemand.Band, cfg.DockLength);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Dock,
                    X = x,
                    Width = cfg.DockLength,
                    Y = ctx.Level(tide),
                    Tide01 = tide,
                    Value = rng.Range(cfg.PassengersPerDock.x, cfg.PassengersPerDock.y + 1),
                    Flags = segmentFlags
                });
            }

            private void EmitDolphin()
            {
                float x = Reserve(TideDemand.None, cfg.CreatureWidth);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Dolphin,
                    X = x,
                    Width = cfg.CreatureWidth,
                    Y = ctx.Level(0.5f),
                    Tide01 = 0.5f,
                    Flags = segmentFlags
                });
                EmitLine(PlacementKind.Coin, 4, cfg.CoinSpacing * 1.5f, 0.5f);
            }

            private void EmitWhale()
            {
                float x = Reserve(TideDemand.None, cfg.CreatureWidth);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Whale,
                    X = x,
                    Width = cfg.CreatureWidth,
                    Y = ctx.Level(cfg.WhaleSurfaceTide),
                    Tide01 = cfg.WhaleSurfaceTide,
                    Flags = segmentFlags
                });
                EmitArc(x + cfg.CreatureWidth + cfg.StarSpacing, ctx.Level(1f) + 1.2f, cfg.LaunchArcStars);
                cursor = Mathf.Max(cursor, x + cfg.CreatureWidth + cfg.StarSpacing * 3f);
            }

            private void EmitShark()
            {
                float x = Reserve(TideDemand.High, cfg.SharkZoneLength);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Shark,
                    X = x,
                    Width = cfg.SharkZoneLength,
                    Y = ctx.Level(cfg.SharkDangerTide),
                    Tide01 = cfg.SharkDangerTide,
                    Flags = segmentFlags
                });
            }

            // Control is lost for the eclipse window, so it only contains obstacles that one pre-set tide can clear.
            private void EmitEclipse()
            {
                float warningX = Mathf.Max(cursor, lastDemandMaxX + minSwitch * 0.5f);
                float startX = warningX + speed * cfg.WeatherWarningSeconds;
                float endX = startX + speed * cfg.EclipseSeconds;
                plan.Weather.Add(new WeatherEvent { Kind = WeatherKind.Eclipse, WarningX = warningX, StartX = startX, EndX = endX });

                // Reset demand tracking: the warning zone itself provides the switch time.
                lastDemand = TideDemand.None;
                cursor = startX + speed * 0.5f;
                bool lowObstacles = rng.Chance(0.5f);
                int count = rng.Range(1, 3);
                for (int i = 0; i < count && cursor < endX - cfg.ObstacleWidth.x; i++)
                {
                    if (lowObstacles)
                    {
                        EmitLow(RandomLowRequired(), RandomWidth(), PlacementFlags.DuringEclipse);
                    }
                    else
                    {
                        EmitHigh(RandomHighAllowed(), RandomWidth(), PlacementFlags.DuringEclipse);
                    }
                }

                cursor = Mathf.Max(cursor, endX);
                // The tide cannot move until the eclipse ends, so the next switch is timed from its end.
                lastDemand = lowObstacles ? TideDemand.High : TideDemand.Low;
                lastDemandMaxX = Mathf.Max(lastDemandMaxX, endX);
            }

            private void EmitKraken()
            {
                cursor += lead;
                float x = Reserve(TideDemand.None, cfg.KrakenWidth);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.KrakenSurface,
                    X = x,
                    Width = cfg.KrakenWidth,
                    Y = ctx.Level(0.5f),
                    Tide01 = 0.5f,
                    Value = krakenPlaced,
                    Flags = segmentFlags | PlacementFlags.RequiresLaunch
                });
                krakenPlaced++;
                cursor += lead;
            }

            private void PlaceMoonstones()
            {
                int count = cfg.MoonstonesForFullMoon + Mathf.RoundToInt(Mathf.Lerp(cfg.SpareMoonstones.x, cfg.SpareMoonstones.y, d));
                float start = plan.StartX + cfg.StartClearDistance;
                float span = Mathf.Max(1f, plan.HarborX - cfg.EndClearDistance - start);
                for (int i = 0; i < count; i++)
                {
                    float x = start + span * (i + rng.Range(0.3f, 0.7f)) / count;
                    x = FindFreeX(x, 0.9f);
                    float tide = SuggestedTideAt(x);
                    Add(new LevelPlacement
                    {
                        Kind = PlacementKind.Moonstone,
                        X = x,
                        Y = ctx.Level(tide),
                        Tide01 = tide
                    });
                }
            }

            private float FindFreeX(float x, float clearance)
            {
                for (int attempt = 0; attempt < 32; attempt++)
                {
                    bool blocked = false;
                    for (int i = 0; i < plan.Placements.Count; i++)
                    {
                        LevelPlacement p = plan.Placements[i];
                        bool solid = p.IsObstacle || p.Kind == PlacementKind.KrakenSurface || p.Kind == PlacementKind.Dock;
                        if (solid && x > p.MinX - clearance && x < p.MaxX + clearance)
                        {
                            x = p.MaxX + clearance + 0.01f;
                            blocked = true;
                        }
                    }

                    if (!blocked)
                    {
                        break;
                    }
                }

                return x;
            }

            // Moonstones sit on the tide the nearest previous obstacle demands, rewarding players who hold it.
            private float SuggestedTideAt(float x)
            {
                float bestX = float.MinValue;
                float tide = rng.Range(0.3f, 0.7f);
                for (int i = 0; i < plan.Placements.Count; i++)
                {
                    LevelPlacement p = plan.Placements[i];
                    if (!p.IsObstacle || p.MaxX > x || p.MaxX < bestX || p.Has(PlacementFlags.RequiresLaunch))
                    {
                        continue;
                    }

                    bestX = p.MaxX;
                    tide = p.Kind == PlacementKind.LowObstacle
                        ? Mathf.Min(0.95f, p.Tide01 + 0.1f)
                        : Mathf.Max(0.05f, p.Tide01 - 0.1f);
                }

                return tide;
            }

            private void FinalizeHarbor()
            {
                float lastX = plan.StartX;
                for (int i = 0; i < plan.Placements.Count; i++)
                {
                    lastX = Mathf.Max(lastX, plan.Placements[i].MaxX);
                }

                plan.HarborX = Mathf.Max(plan.HarborX, lastX + cfg.EndClearDistance);
                Add(new LevelPlacement
                {
                    Kind = PlacementKind.Harbor,
                    X = plan.HarborX,
                    Width = 0f,
                    Y = ctx.Level(0.5f),
                    Tide01 = 0.5f
                });
            }

            private void PlaceRangeWeather(WeatherKind weather)
            {
                if (weather != WeatherKind.Storm && weather != WeatherKind.Fog)
                {
                    return;
                }

                float seconds = weather == WeatherKind.Storm ? cfg.StormSeconds : cfg.FogSeconds;
                float length = Mathf.Min(seconds * speed, (plan.HarborX - plan.StartX) * 0.6f);
                float startX = plan.StartX + (plan.HarborX - plan.StartX - length) * 0.5f;
                float endX = startX + length;
                plan.Weather.Add(new WeatherEvent
                {
                    Kind = weather,
                    WarningX = startX - speed * cfg.WeatherWarningSeconds,
                    StartX = startX,
                    EndX = endX
                });

                if (weather != WeatherKind.Fog)
                {
                    return;
                }

                int count = plan.Placements.Count;
                int obstacleIndex = 0;
                for (int i = 0; i < count; i++)
                {
                    LevelPlacement p = plan.Placements[i];
                    if (!p.IsObstacle || p.X < startX || p.X > endX)
                    {
                        continue;
                    }

                    if (obstacleIndex++ % 2 == 0)
                    {
                        Add(new LevelPlacement
                        {
                            Kind = PlacementKind.Lighthouse,
                            X = p.X - 3f,
                            Y = ctx.Level(1f),
                            Tide01 = p.Tide01
                        });
                    }
                }
            }

            private void ComputeStarThresholds()
            {
                float estimate = 0f;
                int passengers = 0;
                for (int i = 0; i < plan.Placements.Count; i++)
                {
                    LevelPlacement p = plan.Placements[i];
                    switch (p.Kind)
                    {
                        case PlacementKind.Star: estimate += score.StarPoints; break;
                        case PlacementKind.Coin: estimate += score.CoinPoints; break;
                        case PlacementKind.Moonstone: estimate += score.MoonstonePoints; break;
                        case PlacementKind.Chest: estimate += score.ChestPoints; break;
                        case PlacementKind.Dock: passengers += p.Value; break;
                        case PlacementKind.LowObstacle:
                        case PlacementKind.HighObstacle:
                            estimate += score.NearMissPoints * score.EstimatedAverageMultiplier;
                            break;
                    }
                }

                estimate += passengers * score.PassengerPoints;
                estimate += plan.KrakenHitsRequired * score.BossHitPoints;
                plan.TwoStarScore = Mathf.RoundToInt(estimate * cfg.TwoStarFraction);
                plan.ThreeStarScore = Mathf.Max(plan.TwoStarScore + 1, Mathf.RoundToInt(estimate * cfg.ThreeStarFraction));
            }

            private void Add(LevelPlacement placement) => plan.Placements.Add(placement);

            private float Lerp(Vector2 easyHard) => Mathf.Lerp(easyHard.x, easyHard.y, d);

            private float RandomInLerpedRange(Vector2 easy, Vector2 hard)
            {
                float min = Mathf.Lerp(easy.x, hard.x, d);
                float max = Mathf.Lerp(easy.y, hard.y, d);
                return rng.Range(min, max);
            }

            private float RandomLowRequired() => RandomInLerpedRange(cfg.LowRequiredEasy, cfg.LowRequiredHard);

            private float RandomHighAllowed() => RandomInLerpedRange(cfg.HighAllowedEasy, cfg.HighAllowedHard);

            private float RandomWidth() => rng.Range(cfg.ObstacleWidth.x, cfg.ObstacleWidth.y);

            private static int CompareByX(LevelPlacement a, LevelPlacement b) => a.X.CompareTo(b.X);
        }
    }
}
