using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Simulation;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Layers.Boss
{
    /// <summary>
    /// Every 5th level. The Kraken drags sea level toward its own oscillating target, fighting the player's tide.
    /// Three wave launches over its surfacing head defeat it and unlock the harbor.
    /// </summary>
    public sealed class KrakenBoss : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private BossConfig config;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private TideModel tide;
        [SerializeField] private ScoreSystem score;

        public bool IsActive { get; private set; }
        public bool IsDefeated { get; private set; }
        public int Hits { get; private set; }
        public int HitsRequired { get; private set; }

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            LevelPlan plan = runner.Plan;
            IsActive = plan != null && plan.IsBoss;
            IsDefeated = false;
            Hits = 0;
            HitsRequired = plan != null ? plan.KrakenHitsRequired : 0;
        }

        /// <summary>Rewind support. Hits never exceed the snapshot, so a replayed head cannot be counted twice.</summary>
        public void RestoreHits(int hits)
        {
            if (!IsActive || IsDefeated)
            {
                return;
            }

            Hits = Mathf.Clamp(hits, 0, HitsRequired);
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (!IsActive)
            {
                return;
            }

            if (!IsDefeated)
            {
                ApplyPull(levelTime);
            }

            Box2 hull = boat.HullBounds;
            for (int i = runner.WindowStart; i < runner.WindowEnd; i++)
            {
                LevelPlacement head = runner.GetPlacement(i);
                if (head.Kind != PlacementKind.KrakenSurface)
                {
                    continue;
                }

                ref PlacementRuntime state = ref runner.GetRuntime(i);
                if (state.Resolved)
                {
                    continue;
                }

                if (state.View != null)
                {
                    state.View.SetEngaged(!IsDefeated && boat.X >= head.MinX - config.EngageDistance);
                }

                if (IsDefeated)
                {
                    runner.Consume(i);
                    continue;
                }

                if (boat.X > head.MaxX)
                {
                    state.Resolved = true;
                    continue;
                }

                bool over = boat.X >= head.MinX && boat.X <= head.MaxX;
                if (over && boat.IsAirborne && hull.MinY - head.Y >= config.HitMinClearance)
                {
                    RegisterHit(i);
                }
            }
        }

        private void ApplyPull(float levelTime)
        {
            float relief = 1f - config.PullReliefPerHit * Hits;
            float strength = config.PullStrength * Mathf.Max(0f, relief);
            float wave = 0.5f + 0.5f * Mathf.Sin(levelTime * 2f * Mathf.PI / config.PullPeriodSeconds);
            float targetTide = Mathf.Lerp(config.PullTideRange.x, config.PullTideRange.y, wave);
            tide.SetExternalPull(tide.LevelFromNormalized(targetTide), strength);
        }

        private void RegisterHit(int index)
        {
            ref PlacementRuntime state = ref runner.GetRuntime(index);
            state.Resolved = true;
            if (state.View != null)
            {
                state.View.OnTriggered();
            }

            Hits++;
            score.Add(ScoreSource.BossHit);
            GameEvents.RaiseBossHit(Hits, HitsRequired);

            if (Hits >= HitsRequired)
            {
                IsDefeated = true;
                tide.SetExternalPull(0f, 0f);
                GameEvents.RaiseBossDefeated();
            }
        }
    }
}
