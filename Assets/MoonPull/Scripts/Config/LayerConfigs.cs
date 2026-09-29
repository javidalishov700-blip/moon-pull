using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "PassengerConfig", menuName = "MoonPull/Config/Passengers")]
    public sealed class PassengerConfig : ScriptableObject
    {
        [Tooltip("Max normalized tide difference from the dock height that counts as aligned (gauge green).")]
        [SerializeField, Range(0.01f, 0.3f)] private float alignmentTolerance = 0.07f;
        [Tooltip("Seconds of alignment beside the dock needed to board everyone.")]
        [SerializeField, Min(0.05f)] private float boardSeconds = 0.35f;
        [Tooltip("The HUD gauge appears when a dock is this close ahead.")]
        [SerializeField, Min(0f)] private float gaugeLookAhead = 7f;

        public float AlignmentTolerance => alignmentTolerance;
        public float BoardSeconds => boardSeconds;
        public float GaugeLookAhead => gaugeLookAhead;
    }

    [CreateAssetMenu(fileName = "WeatherConfig", menuName = "MoonPull/Config/Weather")]
    public sealed class WeatherConfig : ScriptableObject
    {
        [Tooltip("Distance over which storm and fog fade in and out.")]
        [SerializeField, Min(0.1f)] private float rampDistance = 4f;
        [Tooltip("World units the storm swell pushes sea level up and down.")]
        [SerializeField, Min(0f)] private float stormLevelAmplitude = 0.35f;
        [SerializeField] private Vector2 stormFrequencies = new Vector2(0.9f, 1.7f);
        [SerializeField, Min(0f)] private float fogDensity = 0.07f;

        public float RampDistance => rampDistance;
        public float StormLevelAmplitude => stormLevelAmplitude;
        public Vector2 StormFrequencies => stormFrequencies;
        public float FogDensity => fogDensity;
    }

    [CreateAssetMenu(fileName = "CreatureConfig", menuName = "MoonPull/Config/Creatures")]
    public sealed class CreatureConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float dolphinBoostMultiplier = 1.35f;
        [SerializeField, Min(0.1f)] private float dolphinBoostSeconds = 2.5f;
        [SerializeField, Min(0f)] private float whaleLaunchVelocity = 10f;
        [Tooltip("Seconds in shark water below the danger tide before it bites.")]
        [SerializeField, Min(0f)] private float sharkBiteDelay = 0.35f;
        [Tooltip("Creatures start reacting when the boat is this close.")]
        [SerializeField, Min(0f)] private float engageDistance = 8f;

        public float DolphinBoostMultiplier => dolphinBoostMultiplier;
        public float DolphinBoostSeconds => dolphinBoostSeconds;
        public float WhaleLaunchVelocity => whaleLaunchVelocity;
        public float SharkBiteDelay => sharkBiteDelay;
        public float EngageDistance => engageDistance;
    }

    [CreateAssetMenu(fileName = "BossConfig", menuName = "MoonPull/Config/Boss")]
    public sealed class BossConfig : ScriptableObject
    {
        [Tooltip("How strongly the Kraken drags sea level toward its own target (0..1).")]
        [SerializeField, Range(0f, 1f)] private float pullStrength = 0.35f;
        [Tooltip("Pull strength removed per successful hit, as a fraction.")]
        [SerializeField, Range(0f, 1f)] private float pullReliefPerHit = 0.25f;
        [Tooltip("Normalized tide range the Kraken's pull oscillates across.")]
        [SerializeField] private Vector2 pullTideRange = new Vector2(0.2f, 0.6f);
        [SerializeField, Min(0.5f)] private float pullPeriodSeconds = 6f;
        [Tooltip("Hull must be at least this far above the Kraken's surface line to count as a hit.")]
        [SerializeField, Min(0f)] private float hitMinClearance = 0.2f;
        [SerializeField, Min(0f)] private float engageDistance = 9f;

        public float PullStrength => pullStrength;
        public float PullReliefPerHit => pullReliefPerHit;
        public Vector2 PullTideRange => pullTideRange;
        public float PullPeriodSeconds => pullPeriodSeconds;
        public float HitMinClearance => hitMinClearance;
        public float EngageDistance => engageDistance;
    }
}
