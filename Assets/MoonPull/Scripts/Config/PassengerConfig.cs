using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "PassengerConfig", menuName = "MoonPull/Config/Passengers")]
    public sealed class PassengerConfig : ScriptableObject
    {
        [Tooltip("Max normalized tide difference from the dock height that counts as aligned (gauge green).")]
        [SerializeField, Range(0.01f, 0.3f)] private float alignmentTolerance = 0.07f;
        [Tooltip("Seconds of alignment beside the dock needed to board everyone.")]
        [SerializeField, Min(0.05f)] private float boardSeconds = 0.35f;
        [Tooltip("The HUD gauge appears when a dock is this close ahead.")]
        [SerializeField, Min(0f)] private float gaugeLookAhead = 7f;

        public float AlignmentTolerance => alignmentTolerance;
        public float BoardSeconds => boardSeconds;
        public float GaugeLookAhead => gaugeLookAhead;
    }
}
