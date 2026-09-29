using System;
using System.IO;
using MoonPull.Audio;
using UnityEditor;
using UnityEngine;

namespace MoonPull.EditorTools
{
    /// <summary>
    /// Synthesizes every sound effect and the ambient music loops as WAV files. Soft sine and bell tones on a
    /// pentatonic scale suit the calm night-sea mood and keep the build free of licensed audio.
    /// </summary>
    internal static class Synth
    {
        private const int Rate = 22050;
        private static readonly System.Random Noise = new System.Random(1234);

        public static AudioClip Sfx(SfxId id)
        {
            switch (id)
            {
                case SfxId.UiTap: return Clip("sfx_tap", 0.06f, t => Tone(t, 1300f, 0.06f) * Env(t, 0.002f, 0.06f));
                case SfxId.StarPickup: return Clip("sfx_star", 0.35f, t => Bell(t, 1046.5f, 0.35f));
                case SfxId.CoinPickup: return Clip("sfx_coin", 0.18f, t => (t < 0.06f ? Tone(t, 1318.5f, 1f) : Tone(t, 1760f, 1f)) * Env(t, 0.003f, 0.18f) * 0.6f);
                case SfxId.MoonstonePickup: return Clip("sfx_moonstone", 0.6f, t => (Bell(t, 783.99f, 0.6f) + Bell(t, 1174.66f, 0.6f)) * 0.5f);
                case SfxId.ChestOpen: return Clip("sfx_chest", 0.7f, t => Thump(t, 90f, 0.2f) + Bell(Math.Max(0f, t - 0.1f), 1567.98f, 0.5f) * 0.5f);
                case SfxId.NearMiss: return Clip("sfx_nearmiss", 0.35f, t => Whoosh(t, 0.35f, 900f, 2400f) * 0.8f);
                case SfxId.WaveLaunch: return Clip("sfx_launch", 0.5f, t => Whoosh(t, 0.5f, 300f, 1400f) + Tone(t, 220f + 400f * t, 1f) * Env(t, 0.01f, 0.4f) * 0.3f);
                case SfxId.Splash: return Clip("sfx_splash", 0.45f, t => NoiseBurst(t, 0.45f) * 0.7f);
                case SfxId.Crash: return Clip("sfx_crash", 0.9f, t => NoiseBurst(t, 0.6f) * 0.8f + Thump(t, 55f, 0.8f));
                case SfxId.ShieldHit: return Clip("sfx_shield", 0.5f, t => Bell(t, 1975.5f, 0.5f) * 0.7f + Thump(t, 110f, 0.2f) * 0.5f);
                case SfxId.FullMoonStart: return Clip("sfx_fullmoon", 1.2f, t => Arpeggio(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.12f, 0.9f));
                case SfxId.FullMoonEnd: return Clip("sfx_fullmoon_end", 0.8f, t => Arpeggio(t, new[] { 1046.5f, 783.99f, 659.25f, 523.25f }, 0.1f, 0.6f));
                case SfxId.PassengerBoard: return Clip("sfx_board", 0.3f, t => Pluck(t, 659.25f, 0.3f));
                case SfxId.PassengerDeliver: return Clip("sfx_deliver", 0.8f, t => Arpeggio(t, new[] { 587.33f, 739.99f, 880f }, 0.1f, 0.6f));
                case SfxId.KrakenHit: return Clip("sfx_kraken_hit", 0.8f, t => Thump(t, 65f, 0.7f) + Whoosh(t, 0.4f, 200f, 800f) * 0.4f);
                case SfxId.KrakenDefeat: return Clip("sfx_kraken_defeat", 1.6f, t => Arpeggio(t, new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f, 1.2f));
                case SfxId.LevelComplete: return Clip("sfx_win", 1.4f, t => Arpeggio(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.14f, 1.1f));
                case SfxId.LevelFail: return Clip("sfx_fail", 1.1f, t => Arpeggio(t, new[] { 392f, 349.23f, 311.13f, 261.63f }, 0.18f, 0.8f) * 0.8f);
                case SfxId.Rewind: return Clip("sfx_rewind", 0.8f, t => Whoosh(0.8f - t, 0.8f, 400f, 2000f) + Tone(t, 900f - 600f * t, 1f) * 0.2f * Env(t, 0.02f, 0.8f));
                case SfxId.Purchase: return Clip("sfx_purchase", 0.7f, t => Arpeggio(t, new[] { 1046.5f, 1318.5f, 1567.98f, 2093f }, 0.07f, 0.5f));
                case SfxId.SpinTick: return Clip("sfx_tick", 0.03f, t => Tone(t, 2400f, 1f) * Env(t, 0.001f, 0.03f) * 0.5f);
                case SfxId.Reward: return Clip("sfx_reward", 0.9f, t => Arpeggio(t, new[] { 659.25f, 783.99f, 987.77f, 1318.5f }, 0.1f, 0.7f));
                default: return Clip("sfx_warning", 0.9f, t => (Tone(t, 196f, 1f) + Tone(t, 293.66f, 1f) * 0.6f) * Env(t, 0.08f, 0.9f) * 0.5f);
            }
        }

        /// <summary>16 s ambient loop: slow pad chord plus a soft pentatonic arpeggio. Root differs per region.</summary>
        public static AudioClip Music(string name, float root, bool bright)
        {
            float[] chord = { root, root * 1.25f, root * 1.5f, root * 1.875f };
            float[] scale = { 1f, 1.125f, 1.25f, 1.5f, 1.6875f, 2f };
            const float length = 16f;
            return Clip(name, length, t =>
            {
                float pad = 0f;
                for (int i = 0; i < chord.Length; i++)
                {
                    pad += Mathf.Sin(2f * Mathf.PI * chord[i] * 0.5f * t + i) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / length + i * 1.3f));
                }

                pad *= 0.06f;
                float step = 0.5f;
                int note = (int)(t / step);
                float local = t - note * step;
                float arpFreq = root * 2f * scale[(note * 3 + note / 4) % scale.Length];
                float arp = (bright ? 0.12f : 0.08f) * Pluck(local, arpFreq, step);
                return pad + arp;
            }, true);
        }

        /// <summary>Full Moon shimmer layer, same 16 s length as the region loops so it stays in sync.</summary>
        public static AudioClip FullMoonLayer() => Clip("music_fullmoon_layer", 16f, t =>
        {
            float step = 0.125f;
            int note = (int)(t / step);
            float local = t - note * step;
            float[] notes = { 1046.5f, 1318.5f, 1567.98f, 2093f };
            return Bell(local, notes[note % notes.Length], step) * 0.18f;
        }, true);

        /// <summary>Seamless 4 s surf loop for DynamicWaveAudio.</summary>
        public static AudioClip WaveLoop() => Clip("loop_waves", 4f, t =>
        {
            float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4f);
            return ((float)Noise.NextDouble() * 2f - 1f) * 0.25f * (0.4f + 0.6f * swell);
        }, true);

        // ---------------------------------------------------------------- building blocks

        private static float Tone(float t, float f, float _) => Mathf.Sin(2f * Mathf.PI * f * t);

        private static float Env(float t, float attack, float length)
        {
            if (t < attack) return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / Mathf.Max(0.0001f, length - attack));
        }

        private static float Bell(float t, float f, float length)
        {
            float decay = Mathf.Exp(-t * 6f / length);
            return (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 12f / length)) * decay * 0.5f;
        }

        private static float Pluck(float t, float f, float length)
        {
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 8f / length) * Mathf.Clamp01(t * 400f);
        }

        private static float Thump(float t, float f, float length)
        {
            return Mathf.Sin(2f * Mathf.PI * f * t * (1f - t)) * Mathf.Exp(-t * 7f / length) * 0.8f;
        }

        private static float NoiseBurst(float t, float length)
        {
            return ((float)Noise.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 6f / length);
        }

        // Band-limited sweep approximated by noise times a moving sine, cheap and whooshy.
        private static float Whoosh(float t, float length, float f0, float f1)
        {
            float n = t / length;
            float f = Mathf.Lerp(f0, f1, n);
            return ((float)Noise.NextDouble() * 2f - 1f) * Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(n)) * 0.6f;
        }

        private static float Arpeggio(float t, float[] notes, float step, float tail)
        {
            float sum = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float local = t - i * step;
                if (local >= 0f)
                {
                    sum += Bell(local, notes[i], tail);
                }
            }

            return sum * 0.5f;
        }

        // ---------------------------------------------------------------- WAV writing

        private static AudioClip Clip(string name, float seconds, Func<float, float> generator, bool music = false)
        {
            string dir = Gen.Root + "/Audio";
            Gen.Folder(dir);
            string path = dir + "/" + name + ".wav";
            if (!File.Exists(path))
            {
                int count = Mathf.CeilToInt(seconds * Rate);
                var samples = new short[count];
                float fade = music ? 0f : 0.004f;
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)Rate;
                    float v = generator(t);
                    if (fade > 0f && t > seconds - fade)
                    {
                        v *= (seconds - t) / fade;
                    }

                    samples[i] = (short)(Mathf.Clamp(v, -1f, 1f) * 30000f);
                }

                WriteWav(path, samples);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureImporter(path, music);
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Gen.Error("Audio failed to import: " + path);
            }

            return clip;
        }

        private static void ConfigureImporter(string path, bool music)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                return;
            }

            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.5f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void WriteWav(string path, short[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int dataSize = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(Rate);
                writer.Write(Rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);
                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write(samples[i]);
                }
            }
        }
    }
}
