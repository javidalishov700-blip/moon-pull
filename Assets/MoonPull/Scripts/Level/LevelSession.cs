using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Gameplay.CameraControl;
using MoonPull.Mechanics;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Level
{
    /// <summary>
    /// Builds and resets every gameplay system for a level, detects arrival at the harbor, and assembles the result.
    /// The only class that knows the full reset order.
    /// </summary>
    public sealed class LevelSession : MonoBehaviour, ISimulationTickable
    {
        [Header("Config")]
        [SerializeField] private LevelGenConfig generation;
        [SerializeField] private ScoreConfig scoreConfig;
        [SerializeField] private TideConfig tideConfig;
        [SerializeField] private BoatConfig boatConfig;
        [SerializeField] private RegionCatalog regions;
        [SerializeField] private BoatCatalog boats;

        [Header("Systems")]
        [SerializeField] private SimulationLoop loop;
        [SerializeField] private MoonController moon;
        [SerializeField] private TideModel tide;
        [SerializeField] private WaterSurface water;
        [SerializeField] private BoatController boat;
        [SerializeField] private BoatView boatView;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private ObstacleInteractionSystem obstacles;
        [SerializeField] private PickupSystem pickups;
        [SerializeField] private FullMoonMode fullMoon;
        [SerializeField] private WaveLauncher launcher;
        [SerializeField] private ScoreSystem score;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private MoonPull.Sail.MoonlightSail sail;

        private readonly LevelPlan plan = new LevelPlan();
        private bool completed;
        private bool usedRewind;
        private bool bossDefeated;
        private int passengersDelivered;

        public LevelPlan Plan => plan;
        public RegionDefinition Region { get; private set; }
        public BoatDefinition Boat { get; private set; }
        public LevelStartArgs Args { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>Remote-config difficulty multiplier applied to generation.</summary>
        public float DifficultyScale { get; set; } = 1f;

        /// <summary>While true, reaching the harbor means the Kraken wins. Set by the boss system.</summary>
        public bool HarborLocked { get; set; }

        /// <summary>0..1 progress toward the harbor for the HUD bar.</summary>
        public float Progress => sail != null ? sail.Progress : plan.HarborX > plan.StartX
            ? Mathf.Clamp01((boat.X - plan.StartX) / (plan.HarborX - plan.StartX))
            : 0f;

        private void OnEnable()
        {
            GameEvents.LevelStartRequested += StartLevel;
            GameEvents.RewindStarted += OnRewindStarted;
            GameEvents.PassengersDelivered += OnPassengersDelivered;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.LevelStartRequested -= StartLevel;
            GameEvents.RewindStarted -= OnRewindStarted;
            GameEvents.PassengersDelivered -= OnPassengersDelivered;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.StateChanged -= OnStateChanged;
        }

        public LevelGenContext BuildContext(RegionDefinition region)
        {
            Vector2 hullCenter = boatConfig.HullCenter;
            Vector2 mastCenter = boatConfig.MastCenter;
            return new LevelGenContext
            {
                MinLevel = tideConfig.MinLevel,
                MaxLevel = tideConfig.MaxLevel,
                HullBottomOffset = -(hullCenter.y - boatConfig.HullHalfExtents.y),
                MastTopOffset = mastCenter.y + boatConfig.MastHalfExtents.y,
                Draft = boatConfig.Draft,
                BaseSpeed = boatConfig.BaseSpeed,
                LowVariantCount = region != null ? region.LowObstacles.Length : 0,
                HighVariantCount = region != null ? region.HighObstacles.Length : 0,
                HasSignature = region != null && region.SignatureObstacle != null,
                SignatureKind = region != null ? region.SignatureKind : PlacementKind.LowObstacle,
                DifficultyScale = DifficultyScale
            };
        }

        private void StartLevel(LevelStartArgs args)
        {
            Args = args;
            Region = regions.ForLevel(args.LevelIndex, generation.LevelsPerRegion);
            Boat = boats.Find(args.BoatId);
            BoatModifiers modifiers = BoatModifiers.From(Boat);

            LevelGenerator.Generate(generation, scoreConfig, BuildContext(Region), args.LevelIndex, plan);

            if (sail != null)
            {
                // Moonlight Sail drives the whole run; the legacy tide systems only provide the sea and the score.
                tide.ResetForLevel();
                water.ResetForLevel();
                if (Region != null)
                {
                    water.ApplyPalette(Region.WaterShallow, Region.WaterDeep, Region.Foam);
                }

                score.ResetForLevel();
                fullMoon.ResetForLevel(0);
                loop.SetLevelTime(0f);
                sail.Begin(args, Boat);
                completed = false;
                usedRewind = false;
                HarborLocked = false;
                IsActive = true;
                GameEvents.RaiseLevelStarted(args);
                return;
            }

            moon.ResetForLevel();
            tide.ResetForLevel();
            water.ResetForLevel();
            if (Region != null)
            {
                water.ApplyPalette(Region.WaterShallow, Region.WaterDeep, Region.Foam);
            }

            boat.ResetForLevel(plan.StartX, modifiers, plan.SpeedMultiplier);
            boatView.SetBoat(Boat);
            runner.Load(plan, Region);
            obstacles.ResetForLevel();
            pickups.ResetForLevel();
            score.ResetForLevel();
            fullMoon.ResetForLevel(modifiers.StartingMoonstones);
            launcher.ResetForLevel();
            loop.SetLevelTime(0f);
            cameraRig.Snap();

            completed = false;
            usedRewind = false;
            bossDefeated = false;
            passengersDelivered = 0;
            HarborLocked = plan.IsBoss;
            IsActive = true;

            GameEvents.RaiseLevelStarted(args);
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (!IsActive || completed || boat.X < plan.HarborX)
            {
                return;
            }

            if (HarborLocked)
            {
                boat.ForceCrash(FailReason.Kraken);
                return;
            }

            Complete(levelTime);
        }

        private void Complete(float levelTime)
        {
            completed = true;
            int stars = ScoreRules.StarRating(score.Score, plan.TwoStarScore, plan.ThreeStarScore);
            var result = new LevelResult(
                Args.LevelIndex,
                stars,
                score.Score,
                levelTime,
                pickups.StarsCollected,
                pickups.CoinsCollected,
                obstacles.NearMissCount,
                passengersDelivered,
                pickups.TreasuresFound,
                bossDefeated,
                usedRewind);
            GameEvents.RaiseLevelCompleted(result);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu || to == GameState.Shop)
            {
                IsActive = false;
                runner.Unload();
            }
        }

        private void OnRewindStarted() => usedRewind = true;

        private void OnPassengersDelivered(int count) => passengersDelivered += count;

        private void OnBossDefeated()
        {
            bossDefeated = true;
            HarborLocked = false;
        }
    }
}
