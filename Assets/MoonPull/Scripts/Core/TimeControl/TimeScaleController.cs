using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Core.TimeControl
{
    /// <summary>
    /// Sole owner of Time.timeScale. Slow motion and pause requests are merged here so a near-miss slow-mo can never
    /// un-pause an overlay or ad.
    /// </summary>
    public sealed class TimeScaleController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private float baseFixedDeltaTime;
        private float slowScale = 1f;
        private float holdRemaining;
        private float recoverDuration;
        private float recoverElapsed;
        private int pauseRequests;

        public bool IsPaused => pauseRequests > 0;

        private void Awake()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        /// <summary>Drops to <paramref name="scale"/> for <paramref name="holdSeconds"/> real seconds, then eases back.</summary>
        public void RequestSlowMotion(float scale, float holdSeconds, float recoverSeconds)
        {
            slowScale = Mathf.Clamp01(scale);
            holdRemaining = Mathf.Max(0f, holdSeconds);
            recoverDuration = Mathf.Max(0f, recoverSeconds);
            recoverElapsed = 0f;
        }

        public void PushPause()
        {
            pauseRequests++;
            Apply(CurrentSlowFactor());
        }

        public void PopPause()
        {
            pauseRequests = Mathf.Max(0, pauseRequests - 1);
            Apply(CurrentSlowFactor());
        }

        public void ResetSlowMotion()
        {
            slowScale = 1f;
            holdRemaining = 0f;
            recoverDuration = 0f;
            recoverElapsed = 0f;
            Apply(1f);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to != GameState.Playing)
            {
                ResetSlowMotion();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (holdRemaining > 0f)
            {
                holdRemaining -= dt;
            }
            else if (recoverElapsed < recoverDuration)
            {
                recoverElapsed += dt;
            }
            else
            {
                slowScale = 1f;
            }

            Apply(CurrentSlowFactor());
        }

        private float CurrentSlowFactor()
        {
            if (holdRemaining > 0f)
            {
                return slowScale;
            }

            if (recoverDuration > 0f && recoverElapsed < recoverDuration)
            {
                return Mathf.Lerp(slowScale, 1f, recoverElapsed / recoverDuration);
            }

            return 1f;
        }

        private void Apply(float slowFactor)
        {
            float scale = IsPaused ? 0f : slowFactor;
            Time.timeScale = scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * Mathf.Max(scale, config.MinFixedStepScale);
        }
    }
}
