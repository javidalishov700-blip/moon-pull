using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "SeabedConfig", menuName = "MoonPull/Config/Seabed")]
    public sealed class SeabedConfig : ScriptableObject
    {
        [Tooltip("Seabed Y outside shallows. Keep below TideConfig.MinLevel minus boat draft.")]
        [SerializeField] private float defaultHeight = -4f;
        [Tooltip("Horizontal length of the slope into and out of a shallow span.")]
        [SerializeField, Min(0.01f)] private float rampWidth = 1.5f;

        public float DefaultHeight => defaultHeight;
        public float RampWidth => rampWidth;
    }
}
