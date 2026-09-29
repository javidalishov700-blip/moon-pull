using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Drives every UiTween. Created on first use; one tween per target (a new tween replaces the old).</summary>
    public sealed class TweenRunner : MonoBehaviour
    {
        private sealed class Tween
        {
            public object Target;
            public float Duration;
            public float Delay;
            public float Elapsed;
            public Ease Ease;
            public int Loops;
            public bool Yoyo;
            public bool Forward = true;
            public Action<float> Update;
            public Action Complete;
        }

        private static TweenRunner instance;
        private readonly List<Tween> tweens = new List<Tween>(64);

        private static TweenRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[TweenRunner]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<TweenRunner>();
                }

                return instance;
            }
        }

        internal static void Add(object target, float duration, float delay, Ease ease, int loops, bool yoyo, Action<float> update, Action complete)
        {
            Kill(target, false);
            if (duration <= 0f && delay <= 0f)
            {
                update(1f);
                complete?.Invoke();
                return;
            }

            Instance.tweens.Add(new Tween
            {
                Target = target,
                Duration = Mathf.Max(0.0001f, duration),
                Delay = delay,
                Ease = ease,
                Loops = loops,
                Yoyo = yoyo,
                Update = update,
                Complete = complete
            });
        }

        internal static void Kill(object target, bool complete)
        {
            if (instance == null || target == null)
            {
                return;
            }

            List<Tween> list = instance.tweens;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(list[i].Target, target))
                {
                    continue;
                }

                Tween tween = list[i];
                list.RemoveAt(i);
                if (complete)
                {
                    tween.Update(1f);
                    tween.Complete?.Invoke();
                }
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                if (i >= tweens.Count)
                {
                    continue;
                }

                Tween tween = tweens[i];
                if (tween.Target is UnityEngine.Object unityObject && unityObject == null)
                {
                    tweens.RemoveAt(i);
                    continue;
                }

                if (tween.Delay > 0f)
                {
                    tween.Delay -= dt;
                    continue;
                }

                tween.Elapsed += dt;
                float t = Mathf.Clamp01(tween.Elapsed / tween.Duration);
                float eased = UiTween.Evaluate(tween.Ease, tween.Forward ? t : 1f - t);
                tween.Update(eased);
                if (t < 1f)
                {
                    continue;
                }

                if (tween.Loops != 0)
                {
                    tween.Elapsed = 0f;
                    if (tween.Yoyo)
                    {
                        tween.Forward = !tween.Forward;
                    }

                    if (tween.Loops > 0)
                    {
                        tween.Loops--;
                    }

                    continue;
                }

                int index = tweens.IndexOf(tween);
                if (index >= 0)
                {
                    tweens.RemoveAt(index);
                }

                tween.Complete?.Invoke();
            }
        }
    }
}
