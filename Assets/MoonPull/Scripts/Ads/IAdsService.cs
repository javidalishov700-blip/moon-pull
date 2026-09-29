using System;

namespace MoonPull.Ads
{
    /// <summary>
    /// Ad SDK abstraction. Implementations only load and show; placement rules live in <see cref="AdFrequencyRules"/>
    /// and <see cref="AdsCoordinator"/>, so switching SDKs never changes monetization behavior.
    /// </summary>
    public interface IAdsService
    {
        bool IsInitialized { get; }
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }
        bool IsAppOpenReady { get; }
        bool IsShowingFullScreen { get; }

        /// <summary>Banner height in screen pixels while visible, else 0. UI pads the bottom by this.</summary>
        float BannerHeightPixels { get; }

        event Action<bool> RewardedAvailabilityChanged;
        event Action BannerLayoutChanged;
        event Action<AdImpressionInfo> Impression;
        event Action<AdPaidInfo> Paid;

        /// <summary>Initializes the SDK. Only call after consent allows ad requests.</summary>
        void Initialize(Action onComplete);

        void ShowRewarded(AdPlacement placement, Action<RewardedResult> onComplete);

        /// <summary>Shows an interstitial if loaded. <paramref name="onClosed"/> always runs, shown or not.</summary>
        bool TryShowInterstitial(AdPlacement placement, Action onClosed);

        bool TryShowAppOpen(Action onClosed);

        void ShowBanner();
        void HideBanner();

        /// <summary>Remove Ads purchase: stops interstitial, app open and banner. Rewarded stays.</summary>
        void SetAdsRemoved(bool removed);
    }
}
