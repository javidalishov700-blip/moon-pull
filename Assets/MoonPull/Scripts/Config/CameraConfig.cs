using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "MoonPull/Config/Camera")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Tooltip("Camera position relative to the boat. Positive X keeps the boat left of center so obstacles read early.")]
        [SerializeField] private Vector3 offset = new Vector3(2.6f, 2.3f, -15f);
        [SerializeField, Range(1f, 30f)] private float followSharpness = 8f;
        [Tooltip("0 = camera Y fixed, 1 = fully follows boat Y. Low values keep the horizon stable.")]
        [SerializeField, Range(0f, 1f)] private float verticalFollow = 0.25f;
        [SerializeField, Range(1f, 60f)] private float shakeFrequency = 25f;

        public Vector3 Offset => offset;
        public float FollowSharpness => followSharpness;
        public float VerticalFollow => verticalFollow;
        public float ShakeFrequency => shakeFrequency;
    }
}
