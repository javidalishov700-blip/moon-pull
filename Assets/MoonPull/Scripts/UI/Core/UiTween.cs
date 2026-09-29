using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonPull.UI
{
    public enum Ease
    {
        Linear,
        OutQuad,
        OutCubic,
        OutQuart,
        OutExpo,
        OutBack,
        InCubic,
        InOutSine
    }

    /// <summary>
    /// Small self-contained tween engine for UI (fades, pops, count-ups, coin arcs, wheel spin). Replaces DOTween so
    /// the project builds from source on CI with no Asset Store import. Runs on unscaled time, so UI keeps animating
    /// while gameplay is paused or in slow motion.
    /// </summary>
    public static class UiTween
    {
        public static void Fade(CanvasGroup group, float to, float duration, Action onComplete = null)
        {
            float from = group.alpha;
            TweenRunner.Add(group, duration, 0f, Ease.OutQuad, 0, false, t => group.alpha = Mathf.LerpUnclamped(from, to, t), onComplete);
        }

        public static void PopIn(Transform target, float duration)
        {
            target.localScale = Vector3.one * 0.85f;
            TweenRunner.Add(target, duration, 0f, Ease.OutBack, 0, false,
                t => target.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t), null);
        }

        public static void Punch(Transform target, float strength = 0.15f, float duration = 0.25f)
        {
            TweenRunner.Add(target, duration, 0f, Ease.Linear, 0, false,
                t => target.localScale = Vector3.one * (1f + strength * Mathf.Sin(t * Mathf.PI * 3f) * (1f - t)),
                () => target.localScale = Vector3.one);
        }

        /// <summary>Endless breathing scale, used to draw the eye to rewarded offers (the pulsing x3).</summary>
        public static void PulseLoop(Transform target, float scale = 1.08f, float halfPeriod = 0.45f)
        {
            target.localScale = Vector3.one;
            TweenRunner.Add(target, halfPeriod, 0f, Ease.InOutSine, -1, true,
                t => target.localScale = Vector3.one * Mathf.LerpUnclamped(1f, scale, t), null);
        }

        public static void MoveAnchored(RectTransform target, Vector2 to, float duration, Action onComplete = null)
        {
            Vector2 from = target.anchoredPosition;
            TweenRunner.Add(target, duration, 0f, Ease.OutCubic, 0, false,
                t => target.anchoredPosition = Vector2.LerpUnclamped(from, to, t), onComplete);
        }

        /// <summary>Loops between two anchored positions (tutorial hand). fastUp = flick then restart.</summary>
        public static void YoyoAnchored(RectTransform target, Vector2 from, Vector2 to, float halfPeriod, bool fastUp = false)
        {
            target.anchoredPosition = from;
            TweenRunner.Add(target, halfPeriod, 0f, fastUp ? Ease.OutExpo : Ease.InOutSine, -1, !fastUp,
                t => target.anchoredPosition = Vector2.LerpUnclamped(from, to, t), null);
        }

        /// <summary>Moves along a quadratic curve (coin fly) through <paramref name="control"/>.</summary>
        public static void Arc(Transform target, Vector3 from, Vector3 control, Vector3 to, float duration, float delay, Action onComplete)
        {
            target.position = from;
            TweenRunner.Add(target, duration, delay, Ease.InCubic, 0, false, v =>
            {
                float u = 1f - v;
                target.position = u * u * from + 2f * u * v * control + v * v * to;
            }, onComplete);
        }

        public static void RotateZ(Transform target, float toDegrees, float duration, Action onComplete)
        {
            float from = target.localEulerAngles.z;
            TweenRunner.Add(target, duration, 0f, Ease.OutQuart, 0, false,
                t => target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(from, toDegrees, t)), onComplete);
        }

        /// <summary>Animated number (score and coin count-ups).</summary>
        public static void Count(object target, float from, float to, float duration, Action<float> onUpdate, Action onComplete = null)
        {
            TweenRunner.Add(target, duration, 0f, Ease.OutCubic, 0, false, t => onUpdate(Mathf.LerpUnclamped(from, to, t)), onComplete);
        }

        public static void Kill(object target, bool complete = false) => TweenRunner.Kill(target, complete);

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutQuart: return 1f - Mathf.Pow(1f - t, 4f);
                case Ease.OutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.OutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case Ease.InCubic: return t * t * t;
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default: return t;
            }
        }
    }
}
