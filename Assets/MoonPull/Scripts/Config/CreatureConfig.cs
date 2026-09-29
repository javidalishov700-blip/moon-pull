using UnityEngine;

namespace MoonPull.Config
{
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
}
