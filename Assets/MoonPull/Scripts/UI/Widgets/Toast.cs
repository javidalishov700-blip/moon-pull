using MoonPull.Ads;
using MoonPull.Core;
using MoonPull.Localization;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Short non-blocking message (ad closed early, ad unavailable, purchase results).</summary>
    public sealed class Toast : MonoBehaviour
    {
        [SerializeField] private AdsCoordinator coordinator;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text label;
        [SerializeField, Min(0.5f)] private float visibleSeconds = 2.2f;

        private float hideAt;

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            coordinator.RewardedClosedEarly += OnClosedEarly;
            coordinator.RewardedUnavailable += OnUnavailable;
        }

        private void OnDisable()
        {
            coordinator.RewardedClosedEarly -= OnClosedEarly;
            coordinator.RewardedUnavailable -= OnUnavailable;
        }

        public void Show(string key, params object[] args)
        {
            label.text = Services.TryGet(out ILocalizationService loc)
                ? (args.Length > 0 ? loc.Format(key, args) : loc.Get(key))
                : key;
            UiTween.Fade(group, 1f, 0.15f);
            UiTween.PopIn(label.transform, 0.25f);
            hideAt = Time.unscaledTime + visibleSeconds;
        }

        private void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                hideAt = 0f;
                UiTween.Fade(group, 0f, 0.25f);
            }
        }

        private void OnClosedEarly(AdPlacement placement) => Show(LocKeys.AdsClosedEarly);

        private void OnUnavailable(AdPlacement placement) => Show(LocKeys.AdsNotReady);
    }
}
