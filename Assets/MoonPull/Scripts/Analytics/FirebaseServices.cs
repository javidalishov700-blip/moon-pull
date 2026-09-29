using System;
using System.Collections.Generic;
using UnityEngine;
#if MOONPULL_FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif
#if MOONPULL_FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif
#if MOONPULL_FIREBASE_REMOTE_CONFIG
using Firebase.RemoteConfig;
#endif

namespace MoonPull.Analytics
{
#if MOONPULL_FIREBASE_ANALYTICS
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        public void SetConsent(AnalyticsConsent consent)
        {
            FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, Status(consent.AnalyticsStorage) },
                { ConsentType.AdStorage, Status(consent.AdStorage) },
                { ConsentType.AdUserData, Status(consent.AdUserData) },
                { ConsentType.AdPersonalization, Status(consent.AdPersonalization) }
            });
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        }

        public void SetUserProperty(string name, string value) => FirebaseAnalytics.SetUserProperty(name, value);

        public void LogEvent(string name, params AnalyticsParam[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                FirebaseAnalytics.LogEvent(name);
                return;
            }

            var converted = new Parameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                AnalyticsParam p = parameters[i];
                switch (p.Type)
                {
                    case AnalyticsParam.Kind.Long:
                        converted[i] = new Parameter(p.Name, p.LongValue);
                        break;
                    case AnalyticsParam.Kind.Double:
                        converted[i] = new Parameter(p.Name, p.DoubleValue);
                        break;
                    default:
                        converted[i] = new Parameter(p.Name, p.StringValue);
                        break;
                }
            }

            FirebaseAnalytics.LogEvent(name, converted);
        }

        private static ConsentStatus Status(bool granted) => granted ? ConsentStatus.Granted : ConsentStatus.Denied;
    }
#endif

#if MOONPULL_FIREBASE_CRASHLYTICS
    public sealed class FirebaseCrashReporter : ICrashReporter
    {
        public FirebaseCrashReporter()
        {
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
        }

        public void Log(string message) => Crashlytics.Log(message);
        public void RecordException(Exception exception) => Crashlytics.LogException(exception);
        public void SetCustomKey(string key, string value) => Crashlytics.SetCustomKey(key, value);
    }
#endif

#if MOONPULL_FIREBASE_REMOTE_CONFIG
    public sealed class FirebaseRemoteConfigService : IRemoteConfigService
    {
        public bool HasRemoteValues => FirebaseRemoteConfig.DefaultInstance.Info.LastFetchStatus == LastFetchStatus.Success
                                       || FirebaseRemoteConfig.DefaultInstance.Info.FetchTime.Year > 2000;

        public long GetLong(string key, long fallback) => TryGet(key, out ConfigValue v) ? v.LongValue : fallback;
        public double GetDouble(string key, double fallback) => TryGet(key, out ConfigValue v) ? v.DoubleValue : fallback;
        public bool GetBool(string key, bool fallback) => TryGet(key, out ConfigValue v) ? v.BooleanValue : fallback;

        private static bool TryGet(string key, out ConfigValue value)
        {
            value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            return value.Source != ValueSource.StaticValue;
        }
    }
#endif

    /// <summary>Editor / no-Firebase: logs to the console so event wiring can be verified.</summary>
    public sealed class DebugAnalyticsService : IAnalyticsService
    {
        private readonly bool verbose;

        public DebugAnalyticsService(bool verbose)
        {
            this.verbose = verbose;
        }

        public void SetConsent(AnalyticsConsent consent)
        {
            if (verbose)
            {
                Debug.Log($"[Analytics] consent analytics={consent.AnalyticsStorage} ads={consent.AdStorage} personalization={consent.AdPersonalization}");
            }
        }

        public void SetUserProperty(string name, string value)
        {
            if (verbose)
            {
                Debug.Log($"[Analytics] user property {name}={value}");
            }
        }

        public void LogEvent(string name, params AnalyticsParam[] parameters)
        {
            if (!verbose)
            {
                return;
            }

            var text = new System.Text.StringBuilder(name);
            for (int i = 0; i < parameters.Length; i++)
            {
                AnalyticsParam p = parameters[i];
                text.Append(' ').Append(p.Name).Append('=');
                switch (p.Type)
                {
                    case AnalyticsParam.Kind.Long: text.Append(p.LongValue); break;
                    case AnalyticsParam.Kind.Double: text.Append(p.DoubleValue); break;
                    default: text.Append(p.StringValue); break;
                }
            }

            Debug.Log($"[Analytics] {text}");
        }
    }

    public sealed class DebugCrashReporter : ICrashReporter
    {
        public void Log(string message) => Debug.Log($"[Crash] {message}");
        public void RecordException(Exception exception) => Debug.LogWarning($"[Crash] non-fatal: {exception}");
        public void SetCustomKey(string key, string value)
        {
        }
    }

    /// <summary>Remote Config stand-in that always returns the local defaults.</summary>
    public sealed class LocalRemoteConfigService : IRemoteConfigService
    {
        public bool HasRemoteValues => false;
        public long GetLong(string key, long fallback) => fallback;
        public double GetDouble(string key, double fallback) => fallback;
        public bool GetBool(string key, bool fallback) => fallback;
    }
}
