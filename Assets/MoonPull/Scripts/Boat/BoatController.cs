using System;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Simulation;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Boat
{
    /// <summary>
    /// Kinematic boat: constant forward speed, spring buoyancy from multiple sample points, ballistic flight after a
    /// launch. No PhysX, so state is a copyable struct and rewind is exact.
    /// </summary>
    public sealed class BoatController : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private BoatConfig config;
        [SerializeField] private WaterSurface water;
        [SerializeField] private Seabed seabed;

        private BoatState state;
        private BoatModifiers modifiers = BoatModifiers.Default;
        private float levelSpeedMultiplier = 1f;
        private float buoyancyMeanOffset;
        private float buoyancySlopeDenominator;

        /// <summary>Raised on touchdown with the downward impact speed.</summary>
        public event Action<float> Landed;

        /// <summary>Raised when the boat leaves the water from a launch.</summary>
        public event Action<float> Launched;

        /// <summary>Raised when a crash actually ends the run (after shields and invincibility).</summary>
        public event Action<FailReason> Crashed;

        public BoatState State => state;
        public BoatModifiers Modifiers => modifiers;
        public BoatConfig Config => config;
        public float X => state.X;
        public float Y => state.Y;
        public bool IsAirborne => state.Airborne;
        public bool IsCrashed => state.Crashed;
        public float Speed => config.BaseSpeed * levelSpeedMultiplier * (state.BoostRemaining > 0f ? state.BoostMultiplier : 1f);

        /// <summary>Water surface under the hull, sampled this tick.</summary>
        public float SurfaceHeight { get; private set; }

        /// <summary>Distance between water surface and seabed under the boat.</summary>
        public float WaterDepth { get; private set; }

        /// <summary>Set by Full Moon mode. Obstacles pass harmlessly while true.</summary>
        public bool Invincible { get; set; }

        public bool IsProtected => Invincible || state.InvulnerableRemaining > 0f;

        public Box2 HullBounds => Box2.FromCenter(new Vector2(state.X + config.HullCenter.x, state.Y + config.HullCenter.y), config.HullHalfExtents);

        public Box2 MastBounds => Box2.FromCenter(new Vector2(state.X + config.MastCenter.x, state.Y + config.MastCenter.y), config.MastHalfExtents);

        private void Awake()
        {
            PrecomputeBuoyancy();
        }

        public void ResetForLevel(float startX, BoatModifiers boatModifiers, float speedMultiplier)
        {
            modifiers = boatModifiers;
            levelSpeedMultiplier = speedMultiplier;
            Invincible = false;
            float surface = water.GetHeight(startX);
            state = new BoatState
            {
                X = startX,
                Y = surface,
                BoostMultiplier = 1f,
                ShieldsLeft = boatModifiers.Shields
            };
            SurfaceHeight = surface;
            WaterDepth = surface - seabed.GetHeight(startX);
        }

        public BoatState CaptureState() => state;

        public void RestoreState(BoatState restored)
        {
            state = restored;
            state.Crashed = false;
        }

        /// <summary>Throws the boat upward. Perk launch bonus is applied here so every launch source benefits.</summary>
        public void Launch(float upwardVelocity)
        {
            if (state.Crashed)
            {
                return;
            }

            float velocity = upwardVelocity * modifiers.LaunchMultiplier;
            state.VelocityY = Mathf.Max(state.VelocityY, velocity);
            state.Airborne = true;
            state.AirTime = 0f;
            Launched?.Invoke(velocity);
        }

        public void ApplySpeedBoost(float multiplier, float duration)
        {
            state.BoostMultiplier = multiplier;
            state.BoostRemaining = Mathf.Max(state.BoostRemaining, duration);
        }

        /// <summary>
        /// Attempts to end the run. Returns true only if the boat actually crashed; protection and shields absorb hits.
        /// </summary>
        public bool TryCrash(FailReason reason)
        {
            if (state.Crashed || IsProtected)
            {
                return false;
            }

            if (state.ShieldsLeft > 0)
            {
                state.ShieldsLeft--;
                state.InvulnerableRemaining = config.ShieldInvulnerability;
                GameEvents.RaiseShieldConsumed();
                return false;
            }

            state.Crashed = true;
            Crashed?.Invoke(reason);
            GameEvents.RaiseRunFailed(reason);
            return true;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (state.Crashed)
            {
                return;
            }

            TickTimers(deltaTime);
            state.X += Speed * deltaTime;

            SampleBuoyancy(out float surface, out float slope);
            SurfaceHeight = surface;
            float seabedHeight = seabed.GetHeight(state.X);
            WaterDepth = surface - seabedHeight;

            if (state.Airborne)
            {
                TickAirborne(deltaTime, surface);
            }
            else
            {
                TickFloating(deltaTime, surface);
            }

            TickTilt(deltaTime, slope);
            TickGrounding(deltaTime, seabedHeight);
        }

        private void TickTimers(float deltaTime)
        {
            if (state.BoostRemaining > 0f)
            {
                state.BoostRemaining -= deltaTime;
            }

            if (state.InvulnerableRemaining > 0f)
            {
                state.InvulnerableRemaining -= deltaTime;
            }
        }

        private void TickAirborne(float deltaTime, float surface)
        {
            state.AirTime += deltaTime;
            state.VelocityY -= config.Gravity * deltaTime;
            state.Y += state.VelocityY * deltaTime;

            if (state.Y <= surface && state.VelocityY <= 0f)
            {
                float impact = -state.VelocityY;
                state.Y = surface;
                state.VelocityY *= -config.LandingRetention;
                state.Airborne = false;
                Landed?.Invoke(impact);
            }
        }

        private void TickFloating(float deltaTime, float surface)
        {
            Spring.Step(ref state.Y, ref state.VelocityY, surface, config.BuoyancyFrequency, config.BuoyancyDamping, deltaTime);

            // A fast-rising swell can throw the boat on its own.
            if (state.Y > surface + config.DetachHeight && state.VelocityY > 0f)
            {
                state.Airborne = true;
                state.AirTime = 0f;
            }
        }

        private void TickTilt(float deltaTime, float slope)
        {
            float target = state.Airborne
                ? state.VelocityY * config.AirPitchPerVelocity
                : Mathf.Atan(slope) * Mathf.Rad2Deg;
            target = Mathf.Clamp(target, -config.MaxTiltDegrees, config.MaxTiltDegrees);
            Spring.Step(ref state.Tilt, ref state.TiltVelocity, target, config.TiltFrequency, config.TiltDamping, deltaTime);
        }

        private void TickGrounding(float deltaTime, float seabedHeight)
        {
            float keel = state.Y - config.Draft;
            if (keel > seabedHeight)
            {
                state.GroundedTime = 0f;
                return;
            }

            // Protected boats scrape along the sand instead of sinking into it.
            state.Y = seabedHeight + config.Draft;
            if (state.VelocityY < 0f)
            {
                state.VelocityY = 0f;
            }

            state.GroundedTime += deltaTime;
            if (state.GroundedTime >= config.GroundingGrace)
            {
                TryCrash(FailReason.Aground);
            }
        }

        private void PrecomputeBuoyancy()
        {
            float[] points = config.BuoyancyPoints;
            float sum = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                sum += points[i];
            }

            buoyancyMeanOffset = points.Length > 0 ? sum / points.Length : 0f;
            buoyancySlopeDenominator = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                float d = points[i] - buoyancyMeanOffset;
                buoyancySlopeDenominator += d * d;
            }
        }

        // Least-squares fit through the sample points gives a stable mean height and slope.
        private void SampleBuoyancy(out float meanHeight, out float slope)
        {
            float[] points = config.BuoyancyPoints;
            if (points.Length == 0)
            {
                meanHeight = water.GetHeight(state.X);
                slope = 0f;
                return;
            }

            float sum = 0f;
            float weighted = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                float h = water.GetHeight(state.X + points[i]);
                sum += h;
                weighted += (points[i] - buoyancyMeanOffset) * h;
            }

            meanHeight = sum / points.Length;
            slope = buoyancySlopeDenominator > 0f ? weighted / buoyancySlopeDenominator : 0f;
        }
    }
}
