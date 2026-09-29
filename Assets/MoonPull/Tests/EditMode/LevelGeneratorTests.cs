using System.Collections.Generic;
using MoonPull.Config;
using MoonPull.Level;
using NUnit.Framework;
using UnityEngine;

namespace MoonPull.Tests
{
    public sealed class LevelGeneratorTests
    {
        private LevelGenConfig config;
        private ScoreConfig score;
        private LevelGenContext context;
        private readonly List<string> issues = new List<string>();

        [SetUp]
        public void SetUp()
        {
            config = SoTestUtil.Create<LevelGenConfig>();
            score = SoTestUtil.Create<ScoreConfig>();
            // Mirrors the default TideConfig and BoatConfig values.
            context = new LevelGenContext
            {
                MinLevel = -2.2f,
                MaxLevel = 2.2f,
                HullBottomOffset = 0.3f,
                MastTopOffset = 1.8f,
                Draft = 0.35f,
                BaseSpeed = 4.5f,
                LowVariantCount = 3,
                HighVariantCount = 3,
                HasSignature = true,
                SignatureKind = PlacementKind.LowObstacle,
                DifficultyScale = 1f
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(score);
        }

        private LevelPlan Generate(int level) => LevelGenerator.Generate(config, score, context, level);

        private int MoonstonesRequired(int level) => level >= config.MoonstoneUnlockLevelIndex ? config.MoonstonesForFullMoon : 0;

        [Test]
        public void SameLevel_IsIdenticalEveryTime()
        {
            LevelPlan a = Generate(37);
            LevelPlan b = Generate(37);

            Assert.AreEqual(a.Placements.Count, b.Placements.Count);
            for (int i = 0; i < a.Placements.Count; i++)
            {
                Assert.AreEqual(a.Placements[i].Kind, b.Placements[i].Kind);
                Assert.AreEqual(a.Placements[i].X, b.Placements[i].X, 1e-5f);
                Assert.AreEqual(a.Placements[i].Y, b.Placements[i].Y, 1e-5f);
            }
        }

        [Test]
        public void AllHundredLevels_PassValidation()
        {
            for (int level = 0; level < config.TotalLevels; level++)
            {
                LevelPlan plan = Generate(level);
                bool valid = LevelValidator.Validate(plan, config.StartClearDistance, MoonstonesRequired(level), issues);
                Assert.IsTrue(valid, $"Level {level + 1}: {string.Join(" | ", issues)}");
            }
        }

        [Test]
        public void ThousandLayouts_AcrossSaltsAndDifficulty_PassValidation()
        {
            float[] scales = { 0.5f, 1f, 1.5f };
            for (int salt = 0; salt < 4; salt++)
            {
                SoTestUtil.SetInt(config, "seedSalt", 1000 + salt * 7717);
                foreach (float scale in scales)
                {
                    context.DifficultyScale = scale;
                    for (int level = 0; level < config.TotalLevels; level++)
                    {
                        LevelPlan plan = Generate(level);
                        Assert.IsTrue(LevelValidator.Validate(plan, config.StartClearDistance, MoonstonesRequired(level), issues),
                            $"salt {salt} scale {scale} level {level + 1}: {string.Join(" | ", issues)}");
                    }
                }
            }
        }

        [Test]
        public void Durations_StayInsideTargetWindow()
        {
            for (int level = 0; level < config.TotalLevels; level++)
            {
                LevelPlan plan = Generate(level);
                float speed = context.BaseSpeed * plan.SpeedMultiplier;
                float seconds = (plan.HarborX - plan.StartX) / speed;
                Assert.GreaterOrEqual(seconds, config.MinDurationSeconds * 0.8f, $"Level {level + 1} too short");
                Assert.LessOrEqual(seconds, config.MaxDurationSeconds * 1.5f, $"Level {level + 1} too long");
            }
        }

        [Test]
        public void Tutorial_IsCalmAndMarksFirstObstacle()
        {
            LevelPlan plan = Generate(0);

            Assert.IsTrue(plan.IsTutorial);
            Assert.IsFalse(plan.IsBoss);
            Assert.AreEqual(0, plan.Weather.Count);
            Assert.AreEqual(0, plan.Count(PlacementKind.Moonstone));
            LevelPlacement firstObstacle = plan.Placements.Find(p => p.IsObstacle);
            Assert.IsTrue(firstObstacle.Has(PlacementFlags.Intro));
        }

        [Test]
        public void EveryFifthLevel_IsBossWithEnoughKrakenSurfaces()
        {
            for (int level = 0; level < config.TotalLevels; level++)
            {
                LevelPlan plan = Generate(level);
                bool expectedBoss = level > 0 && (level + 1) % config.BossInterval == 0;
                Assert.AreEqual(expectedBoss, plan.IsBoss, $"Level {level + 1}");
                if (expectedBoss)
                {
                    Assert.GreaterOrEqual(plan.Count(PlacementKind.KrakenSurface), config.KrakenHitsRequired);
                }
            }
        }

        [Test]
        public void Features_AppearOnlyAfterTheirUnlockLevel()
        {
            for (int level = 0; level < 5; level++)
            {
                Assert.AreEqual(0, Generate(level).Count(PlacementKind.Dock), $"Docks too early at level {level + 1}");
            }

            for (int level = 0; level < 15; level++)
            {
                LevelPlan plan = Generate(level);
                Assert.AreEqual(0, plan.Count(PlacementKind.Dolphin) + plan.Count(PlacementKind.Whale) + plan.Count(PlacementKind.Shark),
                    $"Creatures too early at level {level + 1}");
            }

            for (int level = 0; level < config.StormUnlockLevelIndex; level++)
            {
                Assert.AreEqual(0, Generate(level).Weather.Count, $"Weather too early at level {level + 1}");
            }
        }

        [Test]
        public void UnlockLevel_IntroducesItsMechanicFirst()
        {
            LevelPlan launch = Generate(2);
            Assert.AreEqual(LevelSegment.LaunchRock, launch.IntroducedSegment);
            Assert.IsTrue(launch.Placements.Exists(p => p.Has(PlacementFlags.RequiresLaunch) && p.Has(PlacementFlags.Intro)));

            LevelPlan storm = Generate(config.StormUnlockLevelIndex);
            Assert.AreEqual(Core.WeatherKind.Storm, storm.IntroducedWeather);
            Assert.AreEqual(Core.WeatherKind.Storm, storm.Weather[0].Kind);
        }

        [Test]
        public void EclipseWindow_ContainsOnlyOneTideDemand()
        {
            for (int level = config.EclipseUnlockLevelIndex; level < config.TotalLevels; level++)
            {
                LevelPlan plan = Generate(level);
                foreach (WeatherEvent weather in plan.Weather)
                {
                    if (weather.Kind != Core.WeatherKind.Eclipse)
                    {
                        continue;
                    }

                    PlacementKind? kind = null;
                    foreach (LevelPlacement p in plan.Placements)
                    {
                        if (!p.IsObstacle || p.MaxX < weather.StartX || p.MinX > weather.EndX)
                        {
                            continue;
                        }

                        Assert.IsTrue(kind == null || kind == p.Kind, $"Level {level + 1}: mixed obstacles during eclipse");
                        kind = p.Kind;
                    }
                }
            }
        }

        [Test]
        public void StarThresholds_AreOrderedAndPositive()
        {
            for (int level = 0; level < config.TotalLevels; level++)
            {
                LevelPlan plan = Generate(level);
                Assert.Greater(plan.TwoStarScore, 0);
                Assert.Greater(plan.ThreeStarScore, plan.TwoStarScore);
            }
        }

        [Test]
        public void Difficulty_RisesAcrossTheGame()
        {
            float early = LevelGenerator.DifficultyFor(config, 1, 1f);
            float late = LevelGenerator.DifficultyFor(config, 98, 1f);
            Assert.Less(early, late);
            Assert.LessOrEqual(LevelGenerator.DifficultyFor(config, 99, 5f), 1f);
        }
    }
}
