using MoonPull.Core;
using MoonPull.Meta;
using TMPro;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Wallet display. While coins are flying, it holds and counts up as each coin lands.</summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform icon;

        private long displayed;
        private int holds;

        public RectTransform Icon => icon;

        private void OnEnable()
        {
            GameEvents.CoinsChanged += OnCoinsChanged;
            if (meta.IsInitialized)
            {
                SetImmediate(meta.Wallet.Coins);
            }
            else
            {
                meta.Initialized += OnMetaInitialized;
            }
        }

        private void OnDisable()
        {
            GameEvents.CoinsChanged -= OnCoinsChanged;
            meta.Initialized -= OnMetaInitialized;
            UiTween.Kill(this);
        }

        public void Hold() => holds++;

        public void Release()
        {
            holds = Mathf.Max(0, holds - 1);
            if (holds == 0 && meta.IsInitialized)
            {
                AnimateTo(meta.Wallet.Coins);
            }
        }

        public void Bump(long amount)
        {
            displayed += amount;
            label.SetText("{0:0}", displayed);
            UiTween.Punch(icon, 0.25f, 0.2f);
        }

        private void OnMetaInitialized() => SetImmediate(meta.Wallet.Coins);

        private void OnCoinsChanged(long total, long delta)
        {
            if (holds == 0)
            {
                AnimateTo(total);
            }
        }

        private void SetImmediate(long value)
        {
            displayed = value;
            label.SetText("{0:0}", value);
        }

        private void AnimateTo(long target)
        {
            long start = displayed;
            UiTween.Count(this, start, target, 0.45f, v =>
            {
                displayed = (long)v;
                label.SetText("{0:0}", v);
            }, () => SetImmediate(target));
        }
    }
}
