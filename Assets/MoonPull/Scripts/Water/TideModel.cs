using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Simulation;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>
    /// Sea level follows the moon through a damped spring, which gives the water weight: the player must lead
    /// obstacles slightly, and that anticipation is the core skill.
    /// </summary>
    public sealed class TideModel : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private TideConfig config;
        [SerializeField] private MoonController moon;

        private TideState state;
        private float pullTarget;
        private float pullStrength;
        private float disturbance;

        /// <summary>Current sea level in world Y including weather disturbance.</summary>
        public float Level => state.Level + disturbance;

        public float Velocity => state.Velocity;

        /// <summary>Sea level mapped to 0..1 across the tide range.</summary>
        public float Level01 => Mathf.InverseLerp(config.MinLevel, config.MaxLevel, Level);

        public float MinLevel => config.MinLevel;
        public float MaxLevel => config.MaxLevel;

        /// <summary>Level the moon is currently asking for, before spring and modifiers.</summary>
        public float MoonTargetLevel => Mathf.Lerp(config.MinLevel, config.MaxLevel, moon.Height01);

        public void ResetForLevel()
        {
            state = new TideState { Level = MoonTargetLevel, Velocity = 0f };
            pullStrength = 0f;
            disturbance = 0f;
        }

        public TideState CaptureState() => state;

        public void RestoreState(TideState restored) => state = restored;

        /// <summary>Biases the target toward <paramref name="level"/> (Kraken). Strength 0 = none, 1 = full override.</summary>
        public void SetExternalPull(float level, float strength01)
        {
            pullTarget = level;
            pullStrength = Mathf.Clamp01(strength01);
        }

        /// <summary>Additive offset applied on top of the simulated level (storm swell).</summary>
        public void SetDisturbance(float offset)
        {
            disturbance = offset;
        }

        public float LevelFromNormalized(float value01) => Mathf.Lerp(config.MinLevel, config.MaxLevel, value01);

        public void SimulationTick(float deltaTime, float levelTime)
        {
            float target = MoonTargetLevel;
            if (pullStrength > 0f)
            {
                target = Mathf.Lerp(target, pullTarget, pullStrength);
            }

            Spring.Step(ref state.Level, ref state.Velocity, target, config.ResponseFrequency, config.DampingRatio, deltaTime);
        }
    }
}
