using System.Collections;
using MoonPull.Ads;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.IAP;
using MoonPull.Meta;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>Starts the store after Save and Meta so restored purchases can be granted immediately.</summary>
    public sealed class IapBootStep : BootStep
    {
        [SerializeField] private IapConfig config;
        [SerializeField] private MetaGame meta;
        [SerializeField] private AdsCoordinator adsCoordinator;

        public override IEnumerator Run()
        {
            ISaveService save = Services.Get<ISaveService>();
            var fulfillment = new PurchaseFulfillment(config, save, meta, adsCoordinator.SetAdsRemoved);
            IIapService iap;
#if MOONPULL_IAP
            iap = new UnityIapService(config, fulfillment.Fulfill);
#else
            iap = new MockIapService(config, fulfillment.Fulfill);
#endif
            Services.Register(iap);

            bool done = false;
            iap.Initialize(_ => done = true);
            while (!done)
            {
                yield return null;
            }
        }
    }
}
