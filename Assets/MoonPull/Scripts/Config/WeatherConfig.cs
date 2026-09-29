using UnityEngine;

namespace MoonPull.Config
{
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
}
