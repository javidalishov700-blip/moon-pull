using System;

namespace MoonPull.Analytics
{
    public readonly struct AnalyticsParam
    {
        public enum Kind : byte
        {
            String,
            Long,
            Double
        }

        public readonly string Name;
        public readonly Kind Type;
        public readonly string StringValue;
        public readonly long LongValue;
        public readonly double DoubleValue;

        private AnalyticsParam(string name, Kind type, string s, long l, double d)
        {
            Name = name;
            Type = type;
            StringValue = s;
            LongValue = l;
            DoubleValue = d;
        }

        public static AnalyticsParam Of(string name, string value) => new AnalyticsParam(name, Kind.String, value ?? string.Empty, 0, 0);
        public static AnalyticsParam Of(string name, long value) => new AnalyticsParam(name, Kind.Long, null, value, 0);
        public static AnalyticsParam Of(string name, int value) => new AnalyticsParam(name, Kind.Long, null, value, 0);
        public static AnalyticsParam Of(string name, double value) => new AnalyticsParam(name, Kind.Double, null, 0, value);
        public static AnalyticsParam Of(string name, bool value) => new AnalyticsParam(name, Kind.Long, null, value ? 1 : 0, 0);
    }

    /// <summary>Consent Mode v2 signals, derived from the IAB TCF string written by UMP.</summary>
    public readonly struct AnalyticsConsent
    {
        public readonly bool AnalyticsStorage;
        public readonly bool AdStorage;
        public readonly bool AdUserData;
        public readonly bool AdPersonalization;

        public AnalyticsConsent(bool analyticsStorage, bool adStorage, bool adUserData, bool adPersonalization)
        {
            AnalyticsStorage = analyticsStorage;
            AdStorage = adStorage;
            AdUserData = adUserData;
            AdPersonalization = adPersonalization;
        }

        public static AnalyticsConsent AllGranted => new AnalyticsConsent(true, true, true, true);
        public static AnalyticsConsent AllDenied => new AnalyticsConsent(false, false, false, false);
    }

    public interface IAnalyticsService
    {
        void SetConsent(AnalyticsConsent consent);
        void SetUserProperty(string name, string value);
        void LogEvent(string name, params AnalyticsParam[] parameters);
    }

    public interface ICrashReporter
    {
        void Log(string message);
        void RecordException(Exception exception);
        void SetCustomKey(string key, string value);
    }

    public interface IRemoteConfigService
    {
        /// <summary>True when values came from the server (this or a previous session), not only local defaults.</summary>
        bool HasRemoteValues { get; }

        long GetLong(string key, long fallback);
        double GetDouble(string key, double fallback);
        bool GetBool(string key, bool fallback);
    }
}
