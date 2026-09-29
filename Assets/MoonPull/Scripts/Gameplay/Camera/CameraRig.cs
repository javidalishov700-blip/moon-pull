using MoonPull.Boat;
using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Gameplay.CameraControl
{
    /// <summary>Follows the boat along X with a damped, mostly fixed horizon and supports additive trauma shake.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private BoatController boat;

        private float baseY;
        private float shakeAmplitude;
        private float shakeRemaining;
        private float shakeDuration;
        private Vector3 smoothed;

        private void Start()
        {
            baseY = config.Offset.y;
            smoothed = Target();
            transform.position = smoothed;
        }

        /// <summary>Adds screen shake. Stronger requests override weaker ones still running.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (amplitude < shakeAmplitude * (shakeDuration > 0f ? shakeRemaining / shakeDuration : 0f))
            {
                return;
            }

            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeRemaining = shakeDuration;
        }

        /// <summary>Jumps to the target instantly (level start, rewind) to avoid a long pan.</summary>
        public void Snap()
        {
            smoothed = Target();
            transform.position = smoothed;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            smoothed = Vector3.Lerp(smoothed, Target(), 1f - Mathf.Exp(-config.FollowSharpness * dt));

            Vector3 offset = Vector3.zero;
            if (shakeRemaining > 0f)
            {
                shakeRemaining -= dt;
                float strength = shakeAmplitude * Mathf.Clamp01(shakeRemaining / shakeDuration);
                float t = Time.unscaledTime * config.ShakeFrequency;
                offset.x = (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * 2f * strength;
                offset.y = (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f * strength;
            }

            transform.position = smoothed + offset;
        }

        private Vector3 Target()
        {
            Vector3 offset = config.Offset;
            float y = baseY + boat.Y * config.VerticalFollow;
            return new Vector3(boat.X + offset.x, y, offset.z);
        }
    }
}
