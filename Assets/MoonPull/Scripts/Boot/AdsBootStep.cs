using System.Collections;
using MoonPull.Ads;
using MoonPull.Config;
using MoonPull.Consent;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>
    /// Creates the ads service and initializes the SDK only if consent allows ad requests. If consent arrives later
    /// (privacy options changed), the SDK is initialized then.
    /// </summary>
    public sealed class AdsBootStep : BootStep
    {
        [SerializeField] private AdConfig config;
        [SerializeField] private AdsCoordinator coordinator;
        [SerializeField] private MockAdOverlay mockOverlay;

        private IAdsService ads;
        private IConsentService consent;

        public override IEnumerator Run()
        {
#if MOONPULL_ADMOB && !UNITY_EDITOR
            ads = new AdMobAdsService(config, this);
#else
            ads = new MockAdsService(config, mockOverlay);
#endif
            Services.Register(ads);
            coordinator.Bind(ads, Services.Get<ISaveService>(), Services.Get<IClock>());

            consent = Services.Get<IConsentService>();
            consent.ConsentChanged += InitializeIfAllowed;

            if (!consent.CanRequestAds)
            {
                yield break;
            }

            bool done = false;
            ads.Initialize(() => done = true);
            while (!done)
            {
                yield return null;
            }
        }

        private void InitializeIfAllowed()
        {
            if (ads != null && !ads.IsInitialized && consent.CanRequestAds)
            {
                ads.Initialize(null);
            }
        }

        private void OnDestroy()
        {
            if (consent != null)
            {
                consent.ConsentChanged -= InitializeIfAllowed;
            }
        }
    }
}
