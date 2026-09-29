using System.Collections;
using MoonPull.Config;
using MoonPull.Consent;
using MoonPull.Core.Boot;
using MoonPull.Core;
using UnityEngine;

namespace MoonPull.Boot
{
    /// <summary>
    /// Resolves GDPR/US privacy consent (UMP) and then iOS ATT, before any ad or analytics SDK starts.
    /// Mark "Runs In Consent State" and set timeout to 0 in the inspector: the form waits for the user.
    /// </summary>
    public sealed class ConsentBootStep : BootStep
    {
        [SerializeField] private AdConfig adConfig;
        [Tooltip("Development builds pretend to be in the EEA so the consent form can be tested anywhere.")]
        [SerializeField] private bool debugGeographyEeaInDevBuilds = true;
        [SerializeField, Min(1f)] private float attTimeoutSeconds = 120f;

        public override IEnumerator Run()
        {
            IConsentService consent;
#if MOONPULL_ADMOB && !UNITY_EDITOR
            consent = new UmpConsentService(debugGeographyEeaInDevBuilds && AdConfig.IsDevelopmentBuild, adConfig.TestDeviceIds);
#else
            consent = new MockConsentService();
#endif
            Services.Register(consent);
            yield return consent.Gather();
            yield return AttRequester.RequestIfNeeded(attTimeoutSeconds);
        }
    }
}
