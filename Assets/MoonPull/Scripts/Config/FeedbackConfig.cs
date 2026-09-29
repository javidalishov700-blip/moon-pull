using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "FeedbackConfig", menuName = "MoonPull/Config/Feedback")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Header("Camera shake (amplitude, seconds)")]
        [SerializeField] private Vector2 nearMissShake = new Vector2(0.06f, 0.15f);
        [SerializeField] private Vector2 launchShake = new Vector2(0.12f, 0.2f);
        [SerializeField] private Vector2 crashShake = new Vector2(0.35f, 0.4f);
        [SerializeField] private Vector2 bossHitShake = new Vector2(0.25f, 0.3f);
        [SerializeField] private Vector2 fullMoonShake = new Vector2(0.15f, 0.3f);

        [Header("Audio")]
        [Tooltip("Semitones added per consecutive star, capped.")]
        [SerializeField, Range(0, 24)] private int maxStarStreakSemitones = 12;
        [SerializeField, Range(0f, 1f)] private float nearMissDuckDepth = 0.6f;
        [SerializeField, Min(0f)] private float nearMissDuckHold = 0.2f;
        [SerializeField, Min(0.01f)] private float nearMissDuckRecover = 0.35f;
        [Tooltip("Landing impact speed that plays the loudest splash.")]
        [SerializeField, Min(0.1f)] private float splashImpactForMax = 12f;
        [SerializeField, Min(0f)] private float hapticLandingImpact = 6f;

        public Vector2 NearMissShake => nearMissShake;
        public Vector2 LaunchShake => launchShake;
        public Vector2 CrashShake => crashShake;
        public Vector2 BossHitShake => bossHitShake;
        public Vector2 FullMoonShake => fullMoonShake;
        public int MaxStarStreakSemitones => maxStarStreakSemitones;
        public float NearMissDuckDepth => nearMissDuckDepth;
        public float NearMissDuckHold => nearMissDuckHold;
        public float NearMissDuckRecover => nearMissDuckRecover;
        public float SplashImpactForMax => splashImpactForMax;
        public float HapticLandingImpact => hapticLandingImpact;
    }
}
