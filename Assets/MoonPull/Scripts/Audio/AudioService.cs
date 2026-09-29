using UnityEngine;

namespace MoonPull.Audio
{
    public interface IAudioService
    {
        bool MusicEnabled { get; }
        bool SfxEnabled { get; }
        void SetMusicEnabled(bool enabled);
        void SetSfxEnabled(bool enabled);
        void PlayMusic(AudioClip clip);
        void PlaySfx(SfxId id, float pitchMultiplier = 1f, float volumeMultiplier = 1f);

        /// <summary>Dips music volume and cutoff, holds, then recovers (near-miss "breath").</summary>
        void Duck(float depth01, float holdSeconds, float recoverSeconds);

        /// <summary>0 = normal, 1 = Full Moon layer at full volume.</summary>
        void SetMusicIntensity(float intensity01);
    }

    /// <summary>Crossfading music with an intensity layer, and a round-robin SFX voice pool (no per-play allocation).</summary>
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        [SerializeField] private SfxLibrary library;
        [SerializeField] private AudioSource musicA;
        [SerializeField] private AudioSource musicB;
        [SerializeField] private AudioSource intensityLayer;
        [SerializeField] private AudioLowPassFilter musicLowPass;
        [SerializeField] private AudioSource[] sfxVoices = new AudioSource[0];
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
        [SerializeField, Min(0.01f)] private float crossfadeSeconds = 1.2f;
        [SerializeField, Min(10f)] private float duckedCutoff = 900f;
        [SerializeField, Min(10f)] private float openCutoff = 22000f;

        private AudioSource activeMusic;
        private int nextVoice;
        private float crossfade = 1f;
        private float duck;
        private float duckDepth;
        private float duckHold;
        private float duckRecover;
        private float duckRecoverElapsed;
        private float intensity;
        private float intensityTarget;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;
        public SfxLibrary Library => library;

        private void Awake()
        {
            activeMusic = musicA;
        }

        public void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            ApplyMusicVolumes();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            if (!enabled)
            {
                for (int i = 0; i < sfxVoices.Length; i++)
                {
                    sfxVoices[i].Stop();
                }
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || activeMusic.clip == clip)
            {
                return;
            }

            AudioSource next = activeMusic == musicA ? musicB : musicA;
            next.clip = clip;
            next.loop = true;
            next.Play();
            activeMusic = next;
            crossfade = 0f;

            if (intensityLayer != null && library.FullMoonLayer != null)
            {
                intensityLayer.clip = library.FullMoonLayer;
                intensityLayer.loop = true;
                intensityLayer.timeSamples = 0;
                intensityLayer.Play();
            }
        }

        public void PlaySfx(SfxId id, float pitchMultiplier = 1f, float volumeMultiplier = 1f)
        {
            if (!SfxEnabled || sfxVoices.Length == 0 || !library.TryGet(id, out SfxLibrary.Entry entry))
            {
                return;
            }

            AudioSource voice = sfxVoices[nextVoice];
            nextVoice = (nextVoice + 1) % sfxVoices.Length;
            AudioClip clip = entry.Clips.Length == 1 ? entry.Clips[0] : entry.Clips[Random.Range(0, entry.Clips.Length)];
            voice.pitch = pitchMultiplier * (1f + Random.Range(-entry.PitchVariance, entry.PitchVariance));
            voice.PlayOneShot(clip, entry.Volume * volumeMultiplier);
        }

        public void Duck(float depth01, float holdSeconds, float recoverSeconds)
        {
            duckDepth = Mathf.Clamp01(depth01);
            duckHold = holdSeconds;
            duckRecover = Mathf.Max(0.01f, recoverSeconds);
            duckRecoverElapsed = 0f;
            duck = duckDepth;
        }

        public void SetMusicIntensity(float intensity01) => intensityTarget = Mathf.Clamp01(intensity01);

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (crossfade < 1f)
            {
                crossfade = Mathf.Min(1f, crossfade + dt / crossfadeSeconds);
                if (crossfade >= 1f)
                {
                    (activeMusic == musicA ? musicB : musicA).Stop();
                }
            }

            if (duckHold > 0f)
            {
                duckHold -= dt;
            }
            else if (duck > 0f)
            {
                duckRecoverElapsed += dt;
                duck = Mathf.Lerp(duckDepth, 0f, duckRecoverElapsed / duckRecover);
            }

            intensity = Mathf.MoveTowards(intensity, intensityTarget, dt * 2f);
            ApplyMusicVolumes();
        }

        private void ApplyMusicVolumes()
        {
            float master = MusicEnabled ? musicVolume * (1f - duck) : 0f;
            AudioSource inactive = activeMusic == musicA ? musicB : musicA;
            activeMusic.volume = master * crossfade;
            inactive.volume = master * (1f - crossfade);
            if (intensityLayer != null)
            {
                intensityLayer.volume = master * intensity;
            }

            if (musicLowPass != null)
            {
                musicLowPass.cutoffFrequency = Mathf.Lerp(openCutoff, duckedCutoff, duck);
            }
        }
    }
}
