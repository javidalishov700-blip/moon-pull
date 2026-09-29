using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Mechanics
{
    /// <summary>A fast upward flick of the moon throws a swell that launches the boat. Faster flick, higher launch.</summary>
    public sealed class WaveLauncher : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private WaveLaunchConfig config;
        [SerializeField] private MoonController moon;
        [SerializeField] private BoatController boat;
        [SerializeField] private WaterSurface water;
        [SerializeField] private ScoreSystem score;

        // Perfect Crest: flick the moon while the boat rides the very top of a swell for a bigger, faster launch.
        // Consecutive perfect launches build a streak that raises the bonus, rewarding reading the sea's rhythm.
        private const float CrestProbe = 0.7f;
        private const float PerfectLaunchMultiplier = 1.22f;
        private const float PerfectBoost = 1.3f;
        private const float PerfectBoostSeconds = 1.2f;
        private const int PerfectPoints = 60;
        private int perfectStreak;

        private float cooldown;

        public int LaunchCount { get; private set; }

        public void ResetForLevel()
        {
            cooldown = 0f;
            LaunchCount = 0;
            perfectStreak = 0;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (cooldown > 0f)
            {
                cooldown -= deltaTime;
                return;
            }

            if (boat.IsAirborne || boat.IsCrashed || moon.ControlLocked)
            {
                return;
            }

            float velocity = moon.Velocity01;
            if (velocity < config.VelocityThreshold)
            {
                return;
            }

            float strength = Mathf.InverseLerp(config.VelocityThreshold, config.VelocityForMaxLaunch, velocity);
            float launch = Mathf.Lerp(config.MinLaunchVelocity, config.MaxLaunchVelocity, strength);
            float x = boat.X;
            float here = water.GetHeight(x);
            bool perfect = here >= water.GetHeight(x - CrestProbe) && here >= water.GetHeight(x + CrestProbe);
            if (perfect)
            {
                perfectStreak++;
                launch *= PerfectLaunchMultiplier;
                boat.ApplySpeedBoost(PerfectBoost, PerfectBoostSeconds);
                if (score != null)
                {
                    score.AddBonus(PerfectPoints * Mathf.Min(perfectStreak, 5));
                }
            }
            else
            {
                perfectStreak = 0;
            }

            boat.Launch(launch);
            water.EmitPulse(boat.X, config.PulseAmplitude * (0.5f + 0.5f * strength));
            cooldown = config.Cooldown;
            LaunchCount++;
            GameEvents.RaiseWaveLaunched(strength);
            if (perfect)
            {
                GameEvents.RaisePerfectCrest(perfectStreak);
            }
        }
    }
}
