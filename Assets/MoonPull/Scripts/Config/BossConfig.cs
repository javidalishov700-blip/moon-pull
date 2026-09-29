using UnityEngine;

namespace MoonPull.Config
{
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
