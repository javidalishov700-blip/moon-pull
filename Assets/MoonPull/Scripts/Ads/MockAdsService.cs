using System.Collections;
using System;
using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Ads
{
    /// <summary>
    /// Editor/offline stand-in with the same timing semantics as the real SDK (async load, fill failures, early close),
    /// so every ad flow is testable without a device.
    /// </summary>
    public sealed class MockAdsService : IAdsService
    {
        private readonly AdConfig config;
        private readonly MockAdOverlay overlay;
        private bool rewardedLoaded;
        private bool interstitialLoaded;
        private bool appOpenLoaded;
        private bool adsRemoved;
        private bool bannerVisible;
        private int rewardedAttempt;

        public MockAdsService(AdConfig config, MockAdOverlay overlay)
        {
            this.config = config;
            this.overlay = overlay;
        }

        public event Action<bool> RewardedAvailabilityChanged;
        public event Action BannerLayoutChanged;
        public event Action<AdImpressionInfo> Impression;
        public event Action<AdPaidInfo> Paid;

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady => rewardedLoaded;
        public bool IsInterstitialReady => interstitialLoaded && !adsRemoved;
        public bool IsAppOpenReady => appOpenLoaded && !adsRemoved;
        public bool IsShowingFullScreen => overlay.IsShowing;
        public float BannerHeightPixels => bannerVisible && !adsRemoved ? overlay.BannerHeight : 0f;

        public void Initialize(Action onComplete)
        {
            IsInitialized = true;
            LoadRewarded();
            overlay.StartCoroutine(Load(() => interstitialLoaded = true));
            overlay.StartCoroutine(Load(() => appOpenLoaded = true));
            onComplete?.Invoke();
        }

        public void SetAdsRemoved(bool removed)
        {
            adsRemoved = removed;
            if (removed)
            {
                HideBanner();
            }
        }

        public void ShowRewarded(AdPlacement placement, Action<RewardedResult> onComplete)
        {
            if (!rewardedLoaded || overlay.IsShowing)
            {
                onComplete?.Invoke(RewardedResult.NotReady);
                return;
            }

            rewardedLoaded = false;
            RewardedAvailabilityChanged?.Invoke(false);
            Impression?.Invoke(new AdImpressionInfo(AdFormat.Rewarded, placement));
            RaisePaid(AdFormat.Rewarded, placement, 12000);
            overlay.Show($"Rewarded: {placement}", true, completed =>
            {
                LoadRewarded();
                onComplete?.Invoke(completed ? RewardedResult.Rewarded : RewardedResult.ClosedEarly);
            });
        }

        public bool TryShowInterstitial(AdPlacement placement, Action onClosed)
        {
            if (!IsInterstitialReady || overlay.IsShowing)
            {
                onClosed?.Invoke();
                return false;
            }

            interstitialLoaded = false;
            Impression?.Invoke(new AdImpressionInfo(AdFormat.Interstitial, placement));
            RaisePaid(AdFormat.Interstitial, placement, 8000);
            overlay.Show($"Interstitial: {placement}", false, _ =>
            {
                overlay.StartCoroutine(Load(() => interstitialLoaded = true));
                onClosed?.Invoke();
            });
            return true;
        }

        public bool TryShowAppOpen(Action onClosed)
        {
            if (!IsAppOpenReady || overlay.IsShowing)
            {
                onClosed?.Invoke();
                return false;
            }

            appOpenLoaded = false;
            Impression?.Invoke(new AdImpressionInfo(AdFormat.AppOpen, AdPlacement.AppOpen));
            RaisePaid(AdFormat.AppOpen, AdPlacement.AppOpen, 6000);
            overlay.Show("App Open", false, _ =>
            {
                overlay.StartCoroutine(Load(() => appOpenLoaded = true));
                onClosed?.Invoke();
            });
            return true;
        }

        public void ShowBanner()
        {
            if (adsRemoved)
            {
                return;
            }

            bannerVisible = true;
            overlay.ShowBanner(true);
            BannerLayoutChanged?.Invoke();
        }

        public void HideBanner()
        {
            bannerVisible = false;
            overlay.ShowBanner(false);
            BannerLayoutChanged?.Invoke();
        }

        private void LoadRewarded()
        {
            overlay.StartCoroutine(Load(() =>
            {
                rewardedLoaded = true;
                rewardedAttempt = 0;
                RewardedAvailabilityChanged?.Invoke(true);
            }, () =>
            {
                overlay.StartCoroutine(Retry(config.RetryDelay(rewardedAttempt++), LoadRewarded));
            }));
        }

        private IEnumerator Load(Action onLoaded, Action onFailed = null)
        {
            yield return new WaitForSecondsRealtime(config.MockLoadSeconds);
            if (UnityEngine.Random.value <= config.MockFillRate)
            {
                onLoaded();
            }
            else
            {
                onFailed?.Invoke();
            }
        }

        private static IEnumerator Retry(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action();
        }

        private void RaisePaid(AdFormat format, AdPlacement placement, long micros) =>
            Paid?.Invoke(new AdPaidInfo(format, placement, micros, "USD", 0, "mock", "mock"));
    }
}
