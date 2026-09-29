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
}
