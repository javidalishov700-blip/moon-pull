using MoonPull.Core;
using MoonPull.Meta;
using TMPro;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Wallet display. Coins are credited to the wallet instantly (so nothing is lost if the app closes mid-animation);
    /// the counter then rewinds its display by the credited amount and counts up as each flying coin lands.
    /// </summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform icon;

        private long displayed;
        private long pendingCredit;
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

        /// <summary>Starts a fly-in for coins already added to the wallet.</summary>
        public void BeginCredit(long amount)
        {
            holds++;
            pendingCredit += amount;
            SetImmediate(System.Math.Max(0L, meta.Wallet.Coins - pendingCredit));
        }

        public void Bump(long amount)
        {
            pendingCredit = System.Math.Max(0L, pendingCredit - amount);
            displayed += amount;
            label.SetText("{0:0}", displayed);
            UiTween.Punch(icon, 0.25f, 0.2f);
        }

        public void EndCredit()
        {
            holds = Mathf.Max(0, holds - 1);
            if (holds == 0)
            {
                pendingCredit = 0;
                AnimateTo(meta.Wallet.Coins);
            }
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
            UiTween.Kill(this);
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
