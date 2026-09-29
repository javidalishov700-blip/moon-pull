using System;
using UnityEngine;

namespace MoonPull.Core
{
    /// <summary>Converts OS pause/resume callbacks into game events with the time spent in background.</summary>
    public sealed class AppLifecycle : MonoBehaviour
    {
        private DateTime pausedAtUtc;
        private bool isPaused;
        private readonly IClock clock = new SystemClock();

        private void OnApplicationPause(bool paused)
        {
            if (paused && !isPaused)
            {
                isPaused = true;
                pausedAtUtc = clock.UtcNow;
                GameEvents.RaiseAppPaused();
            }
            else if (!paused && isPaused)
            {
                isPaused = false;
                float secondsAway = (float)Math.Max(0d, (clock.UtcNow - pausedAtUtc).TotalSeconds);
                GameEvents.RaiseAppResumed(secondsAway);
            }
        }
    }
}
