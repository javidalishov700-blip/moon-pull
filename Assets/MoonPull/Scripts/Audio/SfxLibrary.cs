using System;
using UnityEngine;

namespace MoonPull.Audio
{
    public enum SfxId
    {
        UiTap,
        StarPickup,
        CoinPickup,
        MoonstonePickup,
        ChestOpen,
        NearMiss,
        WaveLaunch,
        Splash,
        Crash,
        ShieldHit,
        FullMoonStart,
        FullMoonEnd,
        PassengerBoard,
        PassengerDeliver,
        KrakenHit,
        KrakenDefeat,
        LevelComplete,
        LevelFail,
        Rewind,
        Purchase,
        SpinTick,
        Reward,
        WeatherWarning
    }

    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "MoonPull/Audio/Sfx Library")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SfxId Id;
            [Tooltip("One is picked at random per play to avoid repetition fatigue.")]
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume;
            [Range(0f, 0.5f)] public float PitchVariance;
        }

        [SerializeField] private Entry[] entries = new Entry[0];
        [SerializeField] private AudioClip menuMusic;
        [Tooltip("Layer played in sync with region music during Full Moon.")]
        [SerializeField] private AudioClip fullMoonLayer;
        [SerializeField] private AudioClip waveLoop;

        private Entry[] lookup;

        public AudioClip MenuMusic => menuMusic;
        public AudioClip FullMoonLayer => fullMoonLayer;
        public AudioClip WaveLoop => waveLoop;

        /// <summary>O(1) lookup by enum; built once so playback never searches or allocates.</summary>
        public bool TryGet(SfxId id, out Entry entry)
        {
            if (lookup == null)
            {
                lookup = new Entry[Enum.GetValues(typeof(SfxId)).Length];
                for (int i = 0; i < entries.Length; i++)
                {
                    lookup[(int)entries[i].Id] = entries[i];
                }
            }

            entry = lookup[(int)id];
            return entry.Clips != null && entry.Clips.Length > 0;
        }
    }
}
