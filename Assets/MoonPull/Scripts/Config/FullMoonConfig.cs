using UnityEngine;

namespace MoonPull.Config
{
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
}
