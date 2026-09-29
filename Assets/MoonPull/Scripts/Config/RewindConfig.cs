using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "RewindConfig", menuName = "MoonPull/Config/Rewind")]
    public sealed class RewindConfig : ScriptableObject
    {
        [SerializeField, Min(0.5f)] private float rewindSeconds = 3f;
        [SerializeField, Range(10, 60)] private int samplesPerSecond = 30;
        [Tooltip("Real seconds the reverse playback takes on screen.")]
        [SerializeField, Min(0.1f)] private float playbackSeconds = 0.8f;
        [Tooltip("Crash immunity after resuming, so the player gets a fair moment to react.")]
        [SerializeField, Min(0f)] private float resumeInvulnerability = 1f;
        [Tooltip("Minimum recorded history before a rewind is offered.")]
        [SerializeField, Min(0f)] private float minimumHistorySeconds = 0.5f;
        [SerializeField, Min(0)] private int maxRewindsPerLevel = 1;

        public float RewindSeconds => rewindSeconds;
        public int SamplesPerSecond => samplesPerSecond;
        public int Capacity => Mathf.CeilToInt(rewindSeconds * samplesPerSecond);
        public float PlaybackSeconds => playbackSeconds;
        public float ResumeInvulnerability => resumeInvulnerability;
        public float MinimumHistorySeconds => minimumHistorySeconds;
        public int MaxRewindsPerLevel => maxRewindsPerLevel;
    }
}
