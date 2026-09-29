using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "MoonConfig", menuName = "MoonPull/Config/Moon")]
    public sealed class MoonConfig : ScriptableObject
    {
        [Tooltip("Fraction of screen height a drag must cover to move the moon from bottom to top.")]
        [SerializeField, Range(0.2f, 1f)] private float dragRangeScreenFraction = 0.45f;
        [Tooltip("How fast the moon catches up to the finger. Higher = snappier.")]
        [SerializeField, Range(1f, 60f)] private float followSharpness = 22f;
        [Tooltip("Smoothing for the measured moon velocity used by Wave Launch and audio.")]
        [SerializeField, Range(1f, 60f)] private float velocitySharpness = 18f;
        [SerializeField, Range(0f, 1f)] private float startHeight01 = 0.5f;

        [Header("Visual")]
        [Tooltip("Moon local Y (relative to camera) at height 0 and 1.")]
        [SerializeField] private Vector2 visualLocalYRange = new Vector2(4.5f, 11f);
        [Tooltip("Normalized speed above which the star trail emits.")]
        [SerializeField, Range(0f, 5f)] private float trailSpeedThreshold = 0.4f;
        [SerializeField, Min(0f)] private float fullMoonBrightness = 1.8f;
        [SerializeField, Min(0f)] private float eclipseBrightness = 0.08f;

        public float DragRangeScreenFraction => dragRangeScreenFraction;
        public float FollowSharpness => followSharpness;
        public float VelocitySharpness => velocitySharpness;
        public float StartHeight01 => startHeight01;
        public Vector2 VisualLocalYRange => visualLocalYRange;
        public float TrailSpeedThreshold => trailSpeedThreshold;
        public float FullMoonBrightness => fullMoonBrightness;
        public float EclipseBrightness => eclipseBrightness;
    }
}
