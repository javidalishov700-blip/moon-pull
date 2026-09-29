using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "TideConfig", menuName = "MoonPull/Config/Tide")]
    public sealed class TideConfig : ScriptableObject
    {
        [Tooltip("World Y of the sea level when the moon is at its lowest / highest.")]
        [SerializeField] private float minLevel = -2.2f;
        [SerializeField] private float maxLevel = 2.2f;
        [Tooltip("Hz. Higher = water follows the moon more tightly.")]
        [SerializeField, Range(0.2f, 5f)] private float responseFrequency = 1.6f;
        [SerializeField, Range(0.3f, 1.5f)] private float dampingRatio = 0.85f;

        public float MinLevel => minLevel;
        public float MaxLevel => maxLevel;
        public float ResponseFrequency => responseFrequency;
        public float DampingRatio => dampingRatio;
    }
}
