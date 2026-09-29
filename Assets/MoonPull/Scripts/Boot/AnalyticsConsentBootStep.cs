using System.Collections;
using MoonPull.Analytics;
using MoonPull.Consent;
using MoonPull.Core;
using MoonPull.Core.Boot;
using MoonPull.Save;

namespace MoonPull.Boot
{
    /// <summary>Applies the resolved consent to Firebase (Consent Mode v2) and keeps it in sync when the user changes it.</summary>
    public sealed class AnalyticsConsentBootStep : BootStep
    {
        private IConsentService consent;

        public override IEnumerator Run()
        {
            consent = Services.Get<IConsentService>();
            consent.ConsentChanged += Apply;
            Apply();

            IAnalyticsService analytics = Services.Get<IAnalyticsService>();
            SaveData data = Services.Get<ISaveService>().Data;
            analytics.SetUserProperty("remove_ads", data.RemoveAds ? "1" : "0");
            analytics.SetUserProperty("sessions_bucket", Bucket(data.Ads.SessionCount));
            yield break;
        }

        private void Apply()
        {
            Services.Get<IAnalyticsService>().SetConsent(consent.CanRequestAds ? TcfConsentReader.Read() : AnalyticsConsent.AllDenied);
        }

        private static string Bucket(int sessions)
        {
            if (sessions <= 1) return "1";
            if (sessions <= 5) return "2-5";
            if (sessions <= 20) return "6-20";
            return "21+";
        }

        private void OnDestroy()
        {
            if (consent != null)
            {
                consent.ConsentChanged -= Apply;
            }
        }
    }
}
