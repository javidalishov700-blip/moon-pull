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

        private float cooldown;

        public int LaunchCount { get; private set; }

        public void ResetForLevel()
        {
            cooldown = 0f;
            LaunchCount = 0;
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
            boat.Launch(Mathf.Lerp(config.MinLaunchVelocity, config.MaxLaunchVelocity, strength));
            water.EmitPulse(boat.X, config.PulseAmplitude * (0.5f + 0.5f * strength));
            cooldown = config.Cooldown;
            LaunchCount++;
            GameEvents.RaiseWaveLaunched(strength);
        }
    }
}
