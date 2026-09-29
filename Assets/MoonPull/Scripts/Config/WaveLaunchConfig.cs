using UnityEngine;

namespace MoonPull.Config
{
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
}
