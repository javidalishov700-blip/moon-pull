using System;
using UnityEngine;

namespace MoonPull.Core
{
    /// <summary>
    /// Game-wide event hub. Per-frame data (moon height, water level) is read through direct references instead,
    /// events carry only discrete moments. Invoking a cached delegate does not allocate.
    /// </summary>
    public static class GameEvents
    {
        // UI → GameManager intents
        public static event Action<LevelStartArgs> PlayRequested;
        public static event Action RestartRequested;
        public static event Action MenuRequested;
        public static event Action ShopRequested;
        public static event Action RewindGranted;

        // Flow
        public static event Action<GameState, GameState> StateChanged;
        public static event Action<LevelStartArgs> LevelStartRequested;
        public static event Action<LevelStartArgs> LevelStarted;
        public static event Action<LevelResult> LevelCompleted;
        public static event Action<FailReason> RunFailed;
        public static event Action RewindStarted;
        public static event Action RewindFinished;
        public static event Action AppPaused;
        public static event Action<float> AppResumed;

        // Gameplay feedback
        public static event Action<float> WaveLaunched;
        public static event Action<int, int> NearMiss;
        public static event Action NearMissChainBroken;
        public static event Action<int> StarCollected;
        public static event Action<int> CoinCollected;
        public static event Action<int> MoonstoneCollected;
        public static event Action<float> FullMoonStarted;
        public static event Action FullMoonEnded;
        public static event Action<int> TreasureFound;
        public static event Action<int> PassengerBoarded;
        public static event Action<int> PassengersDelivered;
        public static event Action<int, int> ScoreChanged;
        public static event Action ShieldConsumed;
        public static event Action<int, int> BossHit;
        public static event Action BossDefeated;
        public static event Action<WeatherKind, float> WeatherWarning;
        public static event Action<WeatherKind> WeatherStarted;
        public static event Action<WeatherKind> WeatherEnded;

        // Economy
        public static event Action<long, long> CoinsChanged;

        public static void RaisePlayRequested(LevelStartArgs args) => PlayRequested?.Invoke(args);
        public static void RaiseRestartRequested() => RestartRequested?.Invoke();
        public static void RaiseMenuRequested() => MenuRequested?.Invoke();
        public static void RaiseShopRequested() => ShopRequested?.Invoke();
        public static void RaiseRewindGranted() => RewindGranted?.Invoke();

        public static void RaiseStateChanged(GameState from, GameState to) => StateChanged?.Invoke(from, to);
        public static void RaiseLevelStartRequested(LevelStartArgs args) => LevelStartRequested?.Invoke(args);
        public static void RaiseLevelStarted(LevelStartArgs args) => LevelStarted?.Invoke(args);
        public static void RaiseLevelCompleted(LevelResult result) => LevelCompleted?.Invoke(result);
        public static void RaiseRunFailed(FailReason reason) => RunFailed?.Invoke(reason);
        public static void RaiseRewindStarted() => RewindStarted?.Invoke();
        public static void RaiseRewindFinished() => RewindFinished?.Invoke();
        public static void RaiseAppPaused() => AppPaused?.Invoke();
        public static void RaiseAppResumed(float secondsAway) => AppResumed?.Invoke(secondsAway);

        public static void RaiseWaveLaunched(float strength01) => WaveLaunched?.Invoke(strength01);
        public static void RaiseNearMiss(int chain, int multiplier) => NearMiss?.Invoke(chain, multiplier);
        public static void RaiseNearMissChainBroken() => NearMissChainBroken?.Invoke();
        public static void RaiseStarCollected(int streak) => StarCollected?.Invoke(streak);
        public static void RaiseCoinCollected(int amount) => CoinCollected?.Invoke(amount);
        public static void RaiseMoonstoneCollected(int count) => MoonstoneCollected?.Invoke(count);
        public static void RaiseFullMoonStarted(float duration) => FullMoonStarted?.Invoke(duration);
        public static void RaiseFullMoonEnded() => FullMoonEnded?.Invoke();
        public static void RaiseTreasureFound(int coins) => TreasureFound?.Invoke(coins);
        public static void RaisePassengerBoarded(int onboard) => PassengerBoarded?.Invoke(onboard);
        public static void RaisePassengersDelivered(int count) => PassengersDelivered?.Invoke(count);
        public static void RaiseScoreChanged(int score, int multiplier) => ScoreChanged?.Invoke(score, multiplier);
        public static void RaiseShieldConsumed() => ShieldConsumed?.Invoke();
        public static void RaiseBossHit(int hits, int required) => BossHit?.Invoke(hits, required);
        public static void RaiseBossDefeated() => BossDefeated?.Invoke();
        public static void RaiseWeatherWarning(WeatherKind kind, float secondsUntil) => WeatherWarning?.Invoke(kind, secondsUntil);
        public static void RaiseWeatherStarted(WeatherKind kind) => WeatherStarted?.Invoke(kind);
        public static void RaiseWeatherEnded(WeatherKind kind) => WeatherEnded?.Invoke(kind);

        public static void RaiseCoinsChanged(long total, long delta) => CoinsChanged?.Invoke(total, delta);

        /// <summary>Clears every subscriber. Runs automatically on play-mode entry so disabled domain reload cannot leak handlers.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            PlayRequested = null;
            RestartRequested = null;
            MenuRequested = null;
            ShopRequested = null;
            RewindGranted = null;
            StateChanged = null;
            LevelStartRequested = null;
            LevelStarted = null;
            LevelCompleted = null;
            RunFailed = null;
            RewindStarted = null;
            RewindFinished = null;
            AppPaused = null;
            AppResumed = null;
            WaveLaunched = null;
            NearMiss = null;
            NearMissChainBroken = null;
            StarCollected = null;
            CoinCollected = null;
            MoonstoneCollected = null;
            FullMoonStarted = null;
            FullMoonEnded = null;
            TreasureFound = null;
            PassengerBoarded = null;
            PassengersDelivered = null;
            ScoreChanged = null;
            ShieldConsumed = null;
            BossHit = null;
            BossDefeated = null;
            WeatherWarning = null;
            WeatherStarted = null;
            WeatherEnded = null;
            CoinsChanged = null;
        }
    }
}
