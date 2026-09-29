using System.Collections;
using MoonPull.Analytics;
using MoonPull.Core.Boot;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>Runs after Ads and IAP steps so the router can subscribe to their revenue events.</summary>
    public sealed class AnalyticsBindStep : BootStep
    {
        [SerializeField] private AnalyticsEventRouter router;

        public override IEnumerator Run()
        {
            router.BindServices();
            yield break;
        }
    }
}
