using System;
using MoonPull.Config;
using MoonPull.Core.TimeControl;
using MoonPull.Core;
using MoonPull.IAP;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Ads
{
    /// <summary>
    /// Applies monetization policy on top of <see cref="IAdsService"/>: when interstitials and app open ads may show,
    /// banner visibility per screen, pausing the game under full-screen ads, and lifetime playtime tracking.
    /// UI never talks to the SDK directly.
    /// </summary>
    public sealed class AdsCoordinator : MonoBehaviour
    {
        [SerializeField] private AdConfig config;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TimeScaleController timeScale;

        private AdFrequencyRules rules;
        private float lastFullscreenClosedRealtime = float.MinValue;
        private bool pausedForAd;

        /// <summary>Rewarded ad closed before completion; UI shows a friendly "no reward" message.</summary>
        public event Action<AdPlacement> RewardedClosedEarly;

        /// <summary>Rewarded ad could not be shown (not loaded / failed).</summary>
        public event Action<AdPlacement> RewardedUnavailable;

        /// <summary>Forwarded from the service so buttons can enable/disable without polling.</summary>
        public event Action<bool> RewardedAvailabilityChanged;

        public event Action BannerLayoutChanged;

        public bool IsRewardedReady => Ads != null && Ads.IsRewardedReady;
        public float BannerHeightPixels => Ads != null ? Ads.BannerHeightPixels : 0f;

        private IAdsService Ads { get; set; }
        private ISaveService Save { get; set; }
        private IClock Clock { get; set; }

        private bool AdsRemoved => Save != null && Save.Data.RemoveAds;

        private void Awake()
        {
            rules = new AdFrequencyRules(config.DefaultRules);
        }

        /// <summary>Called by the Ads boot step once the service exists (initialized or not).</summary>
        public void Bind(IAdsService ads, ISaveService save, IClock clock)
        {
            Ads = ads;
            Save = save;
            Clock = clock;
            ads.SetAdsRemoved(save.Data.RemoveAds);
            ads.RewardedAvailabilityChanged += available => RewardedAvailabilityChanged?.Invoke(available);
            ads.BannerLayoutChanged += () => BannerLayoutChanged?.Invoke();
            UpdateBanner(gameManager.State);
        }

        /// <summary>Remote Config hook.</summary>
        public void ApplyRules(AdRulesSettings settings) => rules.Settings = settings;

        public void SetAdsRemoved(bool removed)
        {
            Ads?.SetAdsRemoved(removed);
            UpdateBanner(gameManager.State);
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.AppResumed += OnAppResumed;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.AppResumed -= OnAppResumed;
        }

        private void Update()
        {
            // Lifetime playtime gates interstitials for new players; counted only while the app is in the foreground.
            if (Save != null && !pausedForAd)
            {
                Save.Data.Ads.LifetimePlaySeconds += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Shows a rewarded ad. <paramref name="onRewarded"/> runs only if the ad was watched to completion.
        /// </summary>
        public void ShowRewarded(AdPlacement placement, Action onRewarded)
        {
            if (Ads == null || !Ads.IsRewardedReady)
            {
                RewardedUnavailable?.Invoke(placement);
                return;
            }

            PauseForAd();
            Ads.ShowRewarded(placement, result =>
            {
                ResumeAfterAd();
                switch (result)
                {
                    case RewardedResult.Rewarded:
                        rules.OnRewardedShown(Save.Data.Ads, Clock.UtcNow);
                        Save.MarkDirty();
                        onRewarded?.Invoke();
                        break;
                    case RewardedResult.ClosedEarly:
                        RewardedClosedEarly?.Invoke(placement);
                        break;
                    default:
                        RewardedUnavailable?.Invoke(placement);
                        break;
                }
            });
        }

        /// <summary>
        /// Called when the player taps Continue on the level-complete screen. Shows an interstitial if the pacing
        /// rules allow, then runs <paramref name="next"/>. The ad appears only after a deliberate tap, never over buttons.
        /// </summary>
        public void ContinueAfterLevelComplete(int completedLevelIndex, Action next)
        {
            if (Ads == null || Save == null)
            {
                next?.Invoke();
                return;
            }

            InterstitialDecision decision = rules.EvaluateInterstitial(Save.Data.Ads, completedLevelIndex, Clock.UtcNow, AdsRemoved);
            Save.MarkDirty();
            if (decision != InterstitialDecision.Show || !Ads.IsInterstitialReady)
            {
                next?.Invoke();
                return;
            }

            PauseForAd();
            rules.OnInterstitialShown(Save.Data.Ads, Clock.UtcNow);
            Ads.TryShowInterstitial(AdPlacement.LevelEndInterstitial, () =>
            {
                ResumeAfterAd();
                next?.Invoke();
            });
        }

        private void OnLevelCompleted(LevelResult result)
        {
            if (Save != null)
            {
                rules.OnLevelCompleted(Save.Data.Ads);
            }
        }

        private void OnAppResumed(float secondsAway)
        {
            if (Ads == null || Save == null || Ads.IsShowingFullScreen)
            {
                return;
            }

            if (Time.realtimeSinceStartup - lastFullscreenClosedRealtime < config.AppOpenIgnoreAfterAdSeconds)
            {
                return;
            }

            if (Services.TryGet(out IIapService iap) && iap.PurchaseInProgress)
            {
                return;
            }

            GameState state = gameManager.State;
            bool safeScreen = state == GameState.Menu || state == GameState.Shop || state == GameState.Win || state == GameState.Fail;
            if (!safeScreen || !rules.ShouldShowAppOpen(Save.Data.Ads, secondsAway, Clock.UtcNow, AdsRemoved) || !Ads.IsAppOpenReady)
            {
                return;
            }

            PauseForAd();
            rules.OnAppOpenShown(Save.Data.Ads, Clock.UtcNow);
            Save.MarkDirty();
            Ads.TryShowAppOpen(ResumeAfterAd);
        }

        private void OnStateChanged(GameState from, GameState to) => UpdateBanner(to);

        // Banner only on menu and shop, never during gameplay or result screens with primary buttons.
        private void UpdateBanner(GameState state)
        {
            if (Ads == null)
            {
                return;
            }

            if (!AdsRemoved && (state == GameState.Menu || state == GameState.Shop))
            {
                Ads.ShowBanner();
            }
            else
            {
                Ads.HideBanner();
            }
        }

        private void PauseForAd()
        {
            if (pausedForAd)
            {
                return;
            }

            pausedForAd = true;
            timeScale.PushPause();
            AudioListener.pause = true;
        }

        private void ResumeAfterAd()
        {
            lastFullscreenClosedRealtime = Time.realtimeSinceStartup;
            if (!pausedForAd)
            {
                return;
            }

            pausedForAd = false;
            timeScale.PopPause();
            AudioListener.pause = false;
        }
    }
}
