using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "NearMissConfig", menuName = "MoonPull/Config/Near Miss")]
    public sealed class NearMissConfig : ScriptableObject
    {
        [Tooltip("Max clearance (world units) between boat and obstacle that still counts as a near miss.")]
        [SerializeField, Min(0.01f)] private float clearanceThreshold = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float slowMotionScale = 0.3f;
        [SerializeField, Min(0f)] private float slowMotionHold = 0.2f;
        [SerializeField, Min(0f)] private float slowMotionRecover = 0.12f;

        public float ClearanceThreshold => clearanceThreshold;
        public float SlowMotionScale => slowMotionScale;
        public float SlowMotionHold => slowMotionHold;
        public float SlowMotionRecover => slowMotionRecover;
    }

    [CreateAssetMenu(fileName = "WaveLaunchConfig", menuName = "MoonPull/Config/Wave Launch")]
    public sealed class WaveLaunchConfig : ScriptableObject
    {
        [Tooltip("Normalized moon speed (heights per second) that triggers a launch. Normal steering stays below ~1.5.")]
        [SerializeField, Min(0.1f)] private float velocityThreshold = 2.6f;
        [SerializeField, Min(0.1f)] private float velocityForMaxLaunch = 6f;
        [SerializeField, Min(0f)] private float minLaunchVelocity = 7f;
        [SerializeField, Min(0f)] private float maxLaunchVelocity = 11f;
        [SerializeField, Min(0f)] private float pulseAmplitude = 0.6f;
        [SerializeField, Min(0f)] private float cooldown = 0.35f;

        public float VelocityThreshold => velocityThreshold;
        public float VelocityForMaxLaunch => velocityForMaxLaunch;
        public float MinLaunchVelocity => minLaunchVelocity;
        public float MaxLaunchVelocity => maxLaunchVelocity;
        public float PulseAmplitude => pulseAmplitude;
        public float Cooldown => cooldown;
    }

    [CreateAssetMenu(fileName = "FullMoonConfig", menuName = "MoonPull/Config/Full Moon")]
    public sealed class FullMoonConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int moonstonesRequired = 5;
        [SerializeField, Min(0.5f)] private float duration = 6f;
        [Tooltip("Invulnerability after Full Moon ends so the boat is never killed by an obstacle it is already inside.")]
        [SerializeField, Min(0f)] private float endGrace = 0.6f;
        [SerializeField, Min(0.01f)] private float glowFadeSeconds = 0.5f;

        public int MoonstonesRequired => moonstonesRequired;
        public float Duration => duration;
        public float EndGrace => endGrace;
        public float GlowFadeSeconds => glowFadeSeconds;
    }

    [CreateAssetMenu(fileName = "PickupConfig", menuName = "MoonPull/Config/Pickups")]
    public sealed class PickupConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float starRadius = 0.45f;
        [SerializeField, Min(0f)] private float coinRadius = 0.35f;
        [SerializeField, Min(0f)] private float moonstoneRadius = 0.5f;
        [Tooltip("How close the keel must get to the seabed to grab a chest.")]
        [SerializeField, Min(0f)] private float chestReach = 0.35f;
        [SerializeField, Min(1)] private int coinValue = 1;
        [Tooltip("Stars collected within this many seconds of each other build a pitch-rising streak.")]
        [SerializeField, Min(0.1f)] private float starStreakWindow = 1.2f;

        public float StarRadius => starRadius;
        public float CoinRadius => coinRadius;
        public float MoonstoneRadius => moonstoneRadius;
        public float ChestReach => chestReach;
        public int CoinValue => coinValue;
        public float StarStreakWindow => starStreakWindow;
    }

    [CreateAssetMenu(fileName = "LevelRunnerConfig", menuName = "MoonPull/Config/Level Runner")]
    public sealed class LevelRunnerConfig : ScriptableObject
    {
        [Tooltip("Placements spawn this far ahead of the boat. Must exceed the visible width in portrait.")]
        [SerializeField, Min(1f)] private float spawnAhead = 26f;
        [SerializeField, Min(1f)] private float despawnBehind = 10f;

        public float SpawnAhead => spawnAhead;
        public float DespawnBehind => despawnBehind;
    }
}
