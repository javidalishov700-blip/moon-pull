using System;
using UnityEngine;
#if MOONPULL_DOTWEEN
using DG.Tweening;
#endif

namespace MoonPull.UI
{
    /// <summary>
    /// The only file that touches DOTween. All tweens run on unscaled time so UI keeps animating while the game is
    /// paused or in slow motion. Without the MOONPULL_DOTWEEN define, end states apply instantly so the project
    /// still compiles and runs before DOTween is imported.
    /// </summary>
    public static class UiTween
    {
        public static void Fade(CanvasGroup group, float to, float duration, Action onComplete = null)
        {
#if MOONPULL_DOTWEEN
            DOTween.Kill(group);
            DOTween.To(() => group.alpha, a => group.alpha = a, to, duration)
                .SetTarget(group).SetUpdate(true).SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
#else
            group.alpha = to;
            onComplete?.Invoke();
#endif
        }

        public static void PopIn(Transform target, float duration)
        {
#if MOONPULL_DOTWEEN
            target.DOKill();
            target.localScale = Vector3.one * 0.85f;
            target.DOScale(1f, duration).SetUpdate(true).SetEase(Ease.OutBack);
#else
            target.localScale = Vector3.one;
#endif
        }

        public static void Punch(Transform target, float strength = 0.15f, float duration = 0.25f)
        {
#if MOONPULL_DOTWEEN
            target.DOKill(true);
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * strength, duration, 6, 0.6f).SetUpdate(true);
#endif
        }

        /// <summary>Endless breathing scale, used to draw the eye to rewarded offers (the pulsing x3).</summary>
        public static void PulseLoop(Transform target, float scale = 1.08f, float halfPeriod = 0.45f)
        {
#if MOONPULL_DOTWEEN
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOScale(scale, halfPeriod).SetUpdate(true).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
#endif
        }

        public static void MoveAnchored(RectTransform target, Vector2 to, float duration, Action onComplete = null)
        {
#if MOONPULL_DOTWEEN
            DOTween.Kill(target);
            DOTween.To(() => target.anchoredPosition, p => target.anchoredPosition = p, to, duration)
                .SetTarget(target).SetUpdate(true).SetEase(Ease.OutCubic)
                .OnComplete(() => onComplete?.Invoke());
#else
            target.anchoredPosition = to;
            onComplete?.Invoke();
#endif
        }

        /// <summary>Loops between two anchored positions (tutorial hand).</summary>
        public static void YoyoAnchored(RectTransform target, Vector2 from, Vector2 to, float halfPeriod, bool fastUp = false)
        {
            target.anchoredPosition = from;
#if MOONPULL_DOTWEEN
            DOTween.Kill(target);
            DOTween.To(() => target.anchoredPosition, p => target.anchoredPosition = p, to, halfPeriod)
                .SetTarget(target).SetUpdate(true)
                .SetEase(fastUp ? Ease.OutExpo : Ease.InOutSine)
                .SetLoops(-1, fastUp ? LoopType.Restart : LoopType.Yoyo);
#endif
        }

        /// <summary>Moves along a curved path (coin fly): arcs through <paramref name="control"/>.</summary>
        public static void Arc(Transform target, Vector3 from, Vector3 control, Vector3 to, float duration, float delay, Action onComplete)
        {
#if MOONPULL_DOTWEEN
            target.DOKill();
            target.position = from;
            float t = 0f;
            DOTween.To(() => t, v =>
                {
                    t = v;
                    float u = 1f - v;
                    target.position = u * u * from + 2f * u * v * control + v * v * to;
                }, 1f, duration)
                .SetTarget(target).SetDelay(delay).SetUpdate(true).SetEase(Ease.InCubic)
                .OnComplete(() => onComplete?.Invoke());
#else
            target.position = to;
            onComplete?.Invoke();
#endif
        }

        public static void RotateZ(Transform target, float toDegrees, float duration, Action onComplete)
        {
#if MOONPULL_DOTWEEN
            target.DOKill();
            target.DOLocalRotate(new Vector3(0f, 0f, toDegrees), duration, RotateMode.FastBeyond360)
                .SetUpdate(true).SetEase(Ease.OutQuart).OnComplete(() => onComplete?.Invoke());
#else
            target.localRotation = Quaternion.Euler(0f, 0f, toDegrees);
            onComplete?.Invoke();
#endif
        }

        /// <summary>Animated number (score and coin count-ups).</summary>
        public static void Count(object target, float from, float to, float duration, Action<float> onUpdate, Action onComplete = null)
        {
#if MOONPULL_DOTWEEN
            DOTween.Kill(target);
            float value = from;
            DOTween.To(() => value, v =>
                {
                    value = v;
                    onUpdate(v);
                }, to, duration)
                .SetTarget(target).SetUpdate(true).SetEase(Ease.OutCubic)
                .OnComplete(() => onComplete?.Invoke());
#else
            onUpdate(to);
            onComplete?.Invoke();
#endif
        }

        public static void Kill(object target, bool complete = false)
        {
#if MOONPULL_DOTWEEN
            DOTween.Kill(target, complete);
            if (target is Transform transform)
            {
                transform.DOKill(complete);
            }
#endif
        }
    }
}
