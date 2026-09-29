using System;
using System.Collections;
using UnityEngine;
#if MOONPULL_ADMOB
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
#endif
#if MOONPULL_ATT && UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace MoonPull.Consent
{
    public interface IConsentService
    {
        /// <summary>True once consent (or its absence of requirement) allows ad requests.</summary>
        bool CanRequestAds { get; }

        /// <summary>True when the user must be offered a way to change their choice (GDPR). Drives the settings button.</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary>True when analytics/ad personalization is allowed (for Firebase consent mode).</summary>
        bool AnalyticsAllowed { get; }

        event Action ConsentChanged;

        /// <summary>Updates consent info and shows the form only if required. Always completes.</summary>
        IEnumerator Gather();

        void ShowPrivacyOptions(Action onClosed);
    }

#if MOONPULL_ADMOB
    /// <summary>Google UMP: shows the GDPR/US-state message configured in AdMob → Privacy &amp; messaging.</summary>
    public sealed class UmpConsentService : IConsentService
    {
        private readonly bool debugEea;
        private readonly List<string> testDeviceHashedIds;

        public UmpConsentService(bool debugEea, IEnumerable<string> testDeviceHashedIds)
        {
            this.debugEea = debugEea;
            this.testDeviceHashedIds = new List<string>(testDeviceHashedIds);
        }

        public event Action ConsentChanged;

        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        // UMP consent status Obtained/NotRequired both allow collection; Firebase consent mode refines personalization.
        public bool AnalyticsAllowed => CanRequestAds;

        public IEnumerator Gather()
        {
            bool done = false;
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            if (debugEea)
            {
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = testDeviceHashedIds
                };
            }

            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[Consent] Update failed: {updateError.Message}");
                    done = true;
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning($"[Consent] Form failed: {formError.Message}");
                    }

                    done = true;
                });
            });

            while (!done)
            {
                yield return null;
            }

            ConsentChanged?.Invoke();
        }

        public void ShowPrivacyOptions(Action onClosed)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[Consent] Privacy options failed: {error.Message}");
                }

                ConsentChanged?.Invoke();
                onClosed?.Invoke();
            });
        }
    }
#endif

    /// <summary>Editor/no-SDK stand-in: consent granted, privacy button visible so the settings flow can be tested.</summary>
    public sealed class MockConsentService : IConsentService
    {
        public event Action ConsentChanged;

        public bool CanRequestAds => true;
        public bool PrivacyOptionsRequired => true;
        public bool AnalyticsAllowed => true;

        public IEnumerator Gather()
        {
            ConsentChanged?.Invoke();
            yield break;
        }

        public void ShowPrivacyOptions(Action onClosed)
        {
            Debug.Log("[Consent] Mock privacy options shown.");
            ConsentChanged?.Invoke();
            onClosed?.Invoke();
        }
    }

    /// <summary>iOS App Tracking Transparency. No-op on other platforms or without the iOS support package.</summary>
    public static class AttRequester
    {
        public static IEnumerator RequestIfNeeded(float timeoutSeconds)
        {
#if MOONPULL_ATT && UNITY_IOS && !UNITY_EDITOR
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                yield break;
            }

            ATTrackingStatusBinding.RequestAuthorizationTracking();
            float elapsed = 0f;
            while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED
                   && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
#else
            yield break;
#endif
        }
    }
}
