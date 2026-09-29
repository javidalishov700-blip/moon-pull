using UnityEngine;

namespace MoonPull.Config
{
    /// <summary>Shared boat physics. Per-boat differences come from perks, not physics, so every boat feels fair.</summary>
    [CreateAssetMenu(fileName = "BoatConfig", menuName = "MoonPull/Config/Boat")]
    public sealed class BoatConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float baseSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float gravity = 22f;

        [Header("Buoyancy")]
        [Tooltip("X offsets (boat space) where water height is sampled.")]
        [SerializeField] private float[] buoyancyPoints = { -0.7f, -0.25f, 0.25f, 0.7f };
        [SerializeField, Range(0.2f, 6f)] private float buoyancyFrequency = 2.2f;
        [SerializeField, Range(0.1f, 1.5f)] private float buoyancyDamping = 0.55f;
        [Tooltip("Upward velocity carried over after landing (0 = dead stop, 1 = no loss).")]
        [SerializeField, Range(0f, 1f)] private float landingRetention = 0.25f;
        [Tooltip("Boat detaches from the water when it rises this far above the surface.")]
        [SerializeField, Min(0f)] private float detachHeight = 0.35f;

        [Header("Tilt")]
        [SerializeField, Range(0.2f, 8f)] private float tiltFrequency = 2.5f;
        [SerializeField, Range(0.1f, 1.5f)] private float tiltDamping = 0.5f;
        [SerializeField, Range(0f, 10f)] private float airPitchPerVelocity = 2.2f;
        [SerializeField, Range(0f, 60f)] private float maxTiltDegrees = 28f;

        [Header("Shape (origin at waterline)")]
        [SerializeField, Min(0.01f)] private float draft = 0.35f;
        [SerializeField] private Vector2 hullCenter = new Vector2(0f, 0f);
        [SerializeField] private Vector2 hullHalfExtents = new Vector2(0.8f, 0.3f);
        [SerializeField] private Vector2 mastCenter = new Vector2(0f, 1.05f);
        [SerializeField] private Vector2 mastHalfExtents = new Vector2(0.12f, 0.75f);

        [Header("Crash")]
        [Tooltip("Seconds the keel may touch the seabed before it counts as running aground (hides wave-trough flicker).")]
        [SerializeField, Min(0f)] private float groundingGrace = 0.12f;
        [Tooltip("Invulnerability after a shield absorbs a crash.")]
        [SerializeField, Min(0f)] private float shieldInvulnerability = 1.2f;

        public float BaseSpeed => baseSpeed;
        public float Gravity => gravity;
        public float[] BuoyancyPoints => buoyancyPoints;
        public float BuoyancyFrequency => buoyancyFrequency;
        public float BuoyancyDamping => buoyancyDamping;
        public float LandingRetention => landingRetention;
        public float DetachHeight => detachHeight;
        public float TiltFrequency => tiltFrequency;
        public float TiltDamping => tiltDamping;
        public float AirPitchPerVelocity => airPitchPerVelocity;
        public float MaxTiltDegrees => maxTiltDegrees;
        public float Draft => draft;
        public Vector2 HullCenter => hullCenter;
        public Vector2 HullHalfExtents => hullHalfExtents;
        public Vector2 MastCenter => mastCenter;
        public Vector2 MastHalfExtents => mastHalfExtents;
        public float GroundingGrace => groundingGrace;
        public float ShieldInvulnerability => shieldInvulnerability;
    }
}
