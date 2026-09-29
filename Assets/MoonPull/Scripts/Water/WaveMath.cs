using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>Precomputed parameters for one sine wave. Identical math runs in MoonPullWater.hlsl.</summary>
    public readonly struct WaveComponent
    {
        public readonly float Amplitude;
        public readonly float WaveNumber;
        public readonly float AngularSpeed;
        public readonly float DirX;
        public readonly float DirZ;
        public readonly float Phase;

        public WaveComponent(WaterConfig.Wave wave)
        {
            Amplitude = wave.Amplitude;
            WaveNumber = 2f * Mathf.PI / wave.Wavelength;
            AngularSpeed = WaveNumber * wave.Speed;
            float rad = wave.DirectionDegrees * Mathf.Deg2Rad;
            DirX = Mathf.Cos(rad);
            DirZ = Mathf.Sin(rad);
            Phase = wave.Phase;
        }

        public float Sample(float x, float z, float time) =>
            Amplitude * Mathf.Sin(WaveNumber * (DirX * x + DirZ * z) - AngularSpeed * time + Phase);
    }

    /// <summary>
    /// Single source of truth for surface displacement. CPU buoyancy samples z = 0 (the gameplay lane), the shader
    /// samples the full plane, so the boat always sits exactly on the rendered water.
    /// </summary>
    public static class WaveMath
    {
        public static float Height(float x, float z, float time, in WaveComponent a, in WaveComponent b, in WaveComponent c, float amplitudeScale)
        {
            return (a.Sample(x, z, time) + b.Sample(x, z, time) + c.Sample(x, z, time)) * amplitudeScale;
        }

        /// <summary>Gaussian swell centred on <paramref name="centerX"/>, decaying with age.</summary>
        public static float Pulse(float x, float centerX, float amplitude, float width, float age, float decay)
        {
            if (amplitude <= 0f)
            {
                return 0f;
            }

            float d = (x - centerX) / width;
            return amplitude * Mathf.Exp(-d * d) * Mathf.Exp(-decay * age);
        }
    }
}
