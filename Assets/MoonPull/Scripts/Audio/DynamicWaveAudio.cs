using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Audio
{
    /// <summary>Ocean loop whose volume and pitch follow moon speed: the sea audibly answers the finger.</summary>
    public sealed class DynamicWaveAudio : MonoBehaviour
    {
        [SerializeField] private AudioService audioService;
        [SerializeField] private MoonController moon;
        [SerializeField] private AudioSource loop;
        [SerializeField] private Vector2 volumeRange = new Vector2(0.15f, 0.7f);
        [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.3f);
        [Tooltip("Normalized moon speed that maps to max volume/pitch.")]
        [SerializeField, Min(0.1f)] private float speedForMax = 3f;
        [SerializeField, Min(0.1f)] private float sharpness = 8f;

        private float level;

        private void Start()
        {
            if (audioService.Library.WaveLoop != null)
            {
                loop.clip = audioService.Library.WaveLoop;
                loop.loop = true;
                loop.Play();
            }
        }

        private void Update()
        {
            float target = Mathf.Clamp01(Mathf.Abs(moon.Velocity01) / speedForMax);
            level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime));
            loop.volume = audioService.SfxEnabled ? Mathf.Lerp(volumeRange.x, volumeRange.y, level) : 0f;
            loop.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, level);
        }
    }
}
