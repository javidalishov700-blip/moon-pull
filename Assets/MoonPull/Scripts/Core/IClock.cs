using System;

namespace MoonPull.Core
{
    /// <summary>Wall-clock abstraction so idle income, dailies and ad cooldowns are unit-testable.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }

        /// <summary>Seconds since app start, unaffected by timeScale or device clock changes.</summary>
        double RealtimeSinceStartup { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public double RealtimeSinceStartup => UnityEngine.Time.realtimeSinceStartupAsDouble;
    }
}
