using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Mechanics
{
    public struct FullMoonState
    {
        public int Moonstones;
        public float Remaining;
    }

    /// <summary>Five moonstones → Full Moon: invincible boat, doubled score, silver water, brighter moon.</summary>
    public sealed class FullMoonMode : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private FullMoonConfig config;
        [SerializeField] private BoatController boat;
        [SerializeField] private ScoreSystem score;
        [SerializeField] private WaterSurface water;
        [SerializeField] private MoonView moonView;

        private FullMoonState state;
        private float glow;

        public bool IsActive => state.Remaining > 0f;
        public int Moonstones => state.Moonstones;
        public int Required => config.MoonstonesRequired;
        public float Remaining => state.Remaining;
        public int ActivationCount { get; private set; }

        public void ResetForLevel(int startingMoonstones)
        {
            state = new FullMoonState { Moonstones = Mathf.Min(startingMoonstones, config.MoonstonesRequired - 1) };
            glow = 0f;
            ActivationCount = 0;
            ApplyEffects(false);
        }

        public FullMoonState CaptureState() => state;

        public void RestoreState(FullMoonState restored)
        {
            bool wasActive = IsActive;
            state = restored;
            if (wasActive != IsActive)
            {
                ApplyEffects(IsActive);
            }
        }

        public void AddMoonstone()
        {
            if (IsActive)
            {
                return;
            }

            state.Moonstones++;
            GameEvents.RaiseMoonstoneCollected(state.Moonstones);
            if (state.Moonstones >= config.MoonstonesRequired)
            {
                Activate();
            }
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (IsActive)
            {
                state.Remaining -= deltaTime;
                if (state.Remaining <= 0f)
                {
                    state.Remaining = 0f;
                    ApplyEffects(false);
                    boat.GrantInvulnerability(config.EndGrace);
                    GameEvents.RaiseFullMoonEnded();
                }
            }

            float target = IsActive ? 1f : 0f;
            glow = Mathf.MoveTowards(glow, target, deltaTime / config.GlowFadeSeconds);
            water.SetFullMoonGlow(glow);
        }

        private void Activate()
        {
            state.Moonstones = 0;
            state.Remaining = config.Duration + boat.Modifiers.FullMoonBonusSeconds;
            ActivationCount++;
            ApplyEffects(true);
            GameEvents.RaiseFullMoonStarted(state.Remaining);
        }

        private void ApplyEffects(bool active)
        {
            boat.Invincible = active;
            score.SetFullMoon(active);
            moonView.FullMoon = active;
            if (!active)
            {
                water.SetFullMoonGlow(glow);
            }
        }
    }
}
