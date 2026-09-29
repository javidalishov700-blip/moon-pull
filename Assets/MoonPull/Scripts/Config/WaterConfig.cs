using System;
using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "WaterConfig", menuName = "MoonPull/Config/Water")]
    public sealed class WaterConfig : ScriptableObject
    {
        [Serializable]
        public struct Wave
        {
            [Min(0f)] public float Amplitude;
            [Min(0.1f)] public float Wavelength;
            public float Speed;
            [Range(-180f, 180f)] public float DirectionDegrees;
            public float Phase;
        }

        [Tooltip("Exactly three waves; mirrored 1:1 in MoonPullWater.hlsl.")]
        [SerializeField] private Wave waveA = new Wave { Amplitude = 0.12f, Wavelength = 6f, Speed = 1.2f, DirectionDegrees = 0f };
        [SerializeField] private Wave waveB = new Wave { Amplitude = 0.07f, Wavelength = 3.1f, Speed = 1.6f, DirectionDegrees = 35f, Phase = 1.3f };
        [SerializeField] private Wave waveC = new Wave { Amplitude = 0.04f, Wavelength = 1.7f, Speed = 2.1f, DirectionDegrees = -60f, Phase = 2.1f };

        [Header("Launch pulse")]
        [SerializeField, Min(0.1f)] private float pulseWidth = 1.4f;
        [Tooltip("Per-second exponential decay of the launch swell.")]
        [SerializeField, Min(0.1f)] private float pulseDecay = 2.5f;

        [Header("Storm")]
        [Tooltip("Wave amplitude multiplier at full storm intensity.")]
        [SerializeField, Min(1f)] private float stormAmplitudeMultiplier = 3f;

        [Header("Surface mesh")]
        [Tooltip("Mesh follows the camera in steps of this size so vertices never swim.")]
        [SerializeField, Min(0.01f)] private float followSnap = 0.5f;

        public Wave WaveA => waveA;
        public Wave WaveB => waveB;
        public Wave WaveC => waveC;
        public float PulseWidth => pulseWidth;
        public float PulseDecay => pulseDecay;
        public float StormAmplitudeMultiplier => stormAmplitudeMultiplier;
        public float FollowSnap => followSnap;
    }
}
