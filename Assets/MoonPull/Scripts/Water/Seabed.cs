using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>
    /// Seabed height along the lane. Flat and deep by default; the level adds shallow spans (sandbars, treasure beds)
    /// that make very low tide dangerous.
    /// </summary>
    public sealed class Seabed : MonoBehaviour
    {
        private const int MaxSpans = 64;

        private struct Span
        {
            public float Start;
            public float End;
            public float Height;
        }

        [SerializeField] private SeabedConfig config;

        private readonly Span[] spans = new Span[MaxSpans];
        private int count;

        public float DefaultHeight => config.DefaultHeight;

        public void Clear()
        {
            count = 0;
        }

        /// <summary>Adds a flat shallow span with ramps on both sides. Extra spans beyond capacity are dropped.</summary>
        public void AddSpan(float startX, float endX, float height)
        {
            if (count >= MaxSpans)
            {
                Debug.LogWarning("Seabed span capacity reached; span ignored.");
                return;
            }

            spans[count++] = new Span { Start = startX, End = endX, Height = height };
        }

        public float GetHeight(float x)
        {
            float result = config.DefaultHeight;
            float ramp = config.RampWidth;
            for (int i = 0; i < count; i++)
            {
                Span span = spans[i];
                if (x < span.Start - ramp || x > span.End + ramp)
                {
                    continue;
                }

                float t;
                if (x < span.Start)
                {
                    t = 1f - (span.Start - x) / ramp;
                }
                else if (x > span.End)
                {
                    t = 1f - (x - span.End) / ramp;
                }
                else
                {
                    t = 1f;
                }

                float h = Mathf.Lerp(config.DefaultHeight, span.Height, Mathf.SmoothStep(0f, 1f, t));
                if (h > result)
                {
                    result = h;
                }
            }

            return result;
        }
    }
}
