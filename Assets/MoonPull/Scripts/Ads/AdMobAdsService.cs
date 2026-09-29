#if MOONPULL_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Ads
{
    /// <summary>
    /// Google Mobile Ads implementation. Every format is preloaded, reloaded right after it is shown, and retried with
    /// exponential backoff on failure. Mediation adapters plug in underneath with no code changes.
    /// </summary>
    public sealed class AdMobAdsService : IAdsService
    {
        private readonly AdConfig config;
        private readonly MonoBehaviour host;

        private RewardedAd rewarded;
        private InterstitialAd interstitial;
        private AppOpenAd appOpen;
        private BannerView banner;
        private DateTime appOpenLoadedUtc;

        private int rewardedAttempt;
        private int interstitialAttempt;
        private int appOpenAttempt;
        private int bannerAttempt;

        private bool adsRemoved;
        private bool bannerWanted;
        private bool bannerLoaded;
        private bool rewardEarned;
        private Action<RewardedResult> rewardedCallback;
        private Action fullscreenClosedCallback;
        private AdPlacement rewardedPlacement;
        private AdPlacement interstitialPlacement;

        public AdMobAdsService(AdConfig config, MonoBehaviour host)
        {
            this.config = config;
            this.host = host;
        }

        public event Action<bool> RewardedAvailabilityChanged;
        public event Action BannerLayoutChanged;
        public event Action<AdImpressionInfo> Impression;
        public event Action<AdPaidInfo> Paid;

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady => rewarded != null && rewarded.CanShowAd();
        public bool IsInterstitialReady => !adsRemoved && interstitial != null && interstitial.CanShowAd();
        public bool IsAppOpenReady => !adsRemoved && appOpen != null && appOpen.CanShowAd() && !AppOpenExpired;
        public bool IsShowingFullScreen { get; private set; }
        public float BannerHeightPixels => bannerWanted && bannerLoaded && banner != null ? banner.GetHeightInPixels() : 0f;

        private bool AppOpenExpired => (DateTime.UtcNow - appOpenLoadedUtc).TotalHours >= config.AppOpenExpiryHours;

        public void Initialize(Action onComplete)
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.False,
                MaxAdContentRating = MaxAdContentRating.T,
                TestDeviceIds = new List<string>(config.TestDeviceIds)
            });

            MobileAds.Initialize(status =>
            {
                IsInitialized = true;
                LoadRewarded();
                if (!adsRemoved)
                {
                    LoadInterstitial();
                    LoadAppOpen();
                    if (bannerWanted)
                    {
                        CreateBanner();
                    }
                }

                onComplete?.Invoke();
            });
        }

        public void SetAdsRemoved(bool removed)
        {
            adsRemoved = removed;
            if (!removed)
            {
                return;
            }

            interstitial?.Destroy();
            interstitial = null;
            appOpen?.Destroy();
            appOpen = null;
            DestroyBanner();
        }

        public void ShowRewarded(AdPlacement placement, Action<RewardedResult> onComplete)
        {
            if (!IsRewardedReady || IsShowingFullScreen)
            {
                onComplete?.Invoke(RewardedResult.NotReady);
                return;
            }

            rewardEarned = false;
            rewardedPlacement = placement;
            rewardedCallback = onComplete;
            IsShowingFullScreen = true;
            rewarded.Show(reward => rewardEarned = true);
        }

        public bool TryShowInterstitial(AdPlacement placement, Action onClosed)
        {
            if (!IsInterstitialReady || IsShowingFullScreen)
            {
                onClosed?.Invoke();
                return false;
            }

            interstitialPlacement = placement;
            fullscreenClosedCallback = onClosed;
            IsShowingFullScreen = true;
            interstitial.Show();
            return true;
        }

        public bool TryShowAppOpen(Action onClosed)
        {
            if (!IsAppOpenReady || IsShowingFullScreen)
            {
                if (appOpen != null && AppOpenExpired)
                {
                    LoadAppOpen();
                }

                onClosed?.Invoke();
                return false;
            }

            fullscreenClosedCallback = onClosed;
            IsShowingFullScreen = true;
            appOpen.Show();
            return true;
        }

        public void ShowBanner()
        {
            bannerWanted = true;
            if (adsRemoved || !IsInitialized)
            {
                return;
            }

            if (banner == null)
            {
                CreateBanner();
            }
            else if (bannerLoaded)
            {
                banner.Show();
                BannerLayoutChanged?.Invoke();
            }
        }

        public void HideBanner()
        {
            bannerWanted = false;
            banner?.Hide();
            BannerLayoutChanged?.Invoke();
        }

        private void LoadRewarded()
        {
            if (rewarded != null)
            {
                rewarded.Destroy();
                rewarded = null;
            }

            string unitId = config.UnitId(AdFormat.Rewarded);
            RewardedAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[Ads] Rewarded load failed: {error?.GetMessage()}");
                    RewardedAvailabilityChanged?.Invoke(false);
                    Retry(ref rewardedAttempt, LoadRewarded);
                    return;
                }

                rewardedAttempt = 0;
                rewarded = ad;
                ad.OnAdImpressionRecorded += () => Impression?.Invoke(new AdImpressionInfo(AdFormat.Rewarded, rewardedPlacement));
                ad.OnAdPaid += value => RaisePaid(AdFormat.Rewarded, rewardedPlacement, value, unitId, ad.GetResponseInfo());
                ad.OnAdFullScreenContentClosed += () => CompleteRewarded(rewardEarned ? RewardedResult.Rewarded : RewardedResult.ClosedEarly);
                ad.OnAdFullScreenContentFailed += failure => CompleteRewarded(RewardedResult.Failed);
                RewardedAvailabilityChanged?.Invoke(true);
            });
        }

        private void CompleteRewarded(RewardedResult result)
        {
            IsShowingFullScreen = false;
            Action<RewardedResult> callback = rewardedCallback;
            rewardedCallback = null;
            RewardedAvailabilityChanged?.Invoke(false);
            LoadRewarded();
            callback?.Invoke(result);
        }

        private void LoadInterstitial()
        {
            if (adsRemoved)
            {
                return;
            }

            if (interstitial != null)
            {
                interstitial.Destroy();
                interstitial = null;
            }

            string unitId = config.UnitId(AdFormat.Interstitial);
            InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[Ads] Interstitial load failed: {error?.GetMessage()}");
                    Retry(ref interstitialAttempt, LoadInterstitial);
                    return;
                }

                interstitialAttempt = 0;
                interstitial = ad;
                ad.OnAdImpressionRecorded += () => Impression?.Invoke(new AdImpressionInfo(AdFormat.Interstitial, interstitialPlacement));
                ad.OnAdPaid += value => RaisePaid(AdFormat.Interstitial, interstitialPlacement, value, unitId, ad.GetResponseInfo());
                ad.OnAdFullScreenContentClosed += () => CompleteFullscreen(LoadInterstitial);
                ad.OnAdFullScreenContentFailed += failure => CompleteFullscreen(LoadInterstitial);
            });
        }

        private void LoadAppOpen()
        {
            if (adsRemoved)
            {
                return;
            }

            if (appOpen != null)
            {
                appOpen.Destroy();
                appOpen = null;
            }

            string unitId = config.UnitId(AdFormat.AppOpen);
            AppOpenAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[Ads] App open load failed: {error?.GetMessage()}");
                    Retry(ref appOpenAttempt, LoadAppOpen);
                    return;
                }

                appOpenAttempt = 0;
                appOpen = ad;
                appOpenLoadedUtc = DateTime.UtcNow;
                ad.OnAdImpressionRecorded += () => Impression?.Invoke(new AdImpressionInfo(AdFormat.AppOpen, AdPlacement.AppOpen));
                ad.OnAdPaid += value => RaisePaid(AdFormat.AppOpen, AdPlacement.AppOpen, value, unitId, ad.GetResponseInfo());
                ad.OnAdFullScreenContentClosed += () => CompleteFullscreen(LoadAppOpen);
                ad.OnAdFullScreenContentFailed += failure => CompleteFullscreen(LoadAppOpen);
            });
        }

        private void CompleteFullscreen(Action reload)
        {
            IsShowingFullScreen = false;
            Action callback = fullscreenClosedCallback;
            fullscreenClosedCallback = null;
            reload();
            callback?.Invoke();
        }

        private void CreateBanner()
        {
            DestroyBanner();
            string unitId = config.UnitId(AdFormat.Banner);
            AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            banner = new BannerView(unitId, size, AdPosition.Bottom);
            banner.OnBannerAdLoaded += () =>
            {
                bannerAttempt = 0;
                bannerLoaded = true;
                if (!bannerWanted)
                {
                    banner.Hide();
                }

                BannerLayoutChanged?.Invoke();
            };
            banner.OnBannerAdLoadFailed += error =>
            {
                Debug.LogWarning($"[Ads] Banner load failed: {error?.GetMessage()}");
                DestroyBanner();
                Retry(ref bannerAttempt, () =>
                {
                    if (bannerWanted && !adsRemoved)
                    {
                        CreateBanner();
                    }
                });
            };
            banner.OnAdImpressionRecorded += () => Impression?.Invoke(new AdImpressionInfo(AdFormat.Banner, AdPlacement.MenuBanner));
            banner.OnAdPaid += value => RaisePaid(AdFormat.Banner, AdPlacement.MenuBanner, value, unitId, banner?.GetResponseInfo());
            banner.LoadAd(new AdRequest());
        }

        private void DestroyBanner()
        {
            if (banner == null)
            {
                return;
            }

            banner.Destroy();
            banner = null;
            bannerLoaded = false;
            BannerLayoutChanged?.Invoke();
        }

        private void Retry(ref int attempt, Action load)
        {
            float delay = config.RetryDelay(attempt);
            attempt++;
            host.StartCoroutine(RetryAfter(delay, load));
        }

        private static IEnumerator RetryAfter(float seconds, Action load)
        {
            yield return new WaitForSecondsRealtime(seconds);
            load();
        }

        private void RaisePaid(AdFormat format, AdPlacement placement, AdValue value, string unitId, ResponseInfo response)
        {
            string source = string.Empty;
            AdapterResponseInfo adapter = response?.GetLoadedAdapterResponseInfo();
            if (adapter != null)
            {
                source = adapter.AdSourceName;
            }

            Paid?.Invoke(new AdPaidInfo(format, placement, value.Value, value.CurrencyCode, (int)value.Precision, unitId, source));
        }
    }
}
#endif
