using MoonPull.Ads;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.IAP;
using MoonPull.Level;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.Analytics
{
    /// <summary>
    /// Translates game events into analytics events and Crashlytics breadcrumbs. Gameplay code never references
    /// analytics, so tracking can change without touching game logic.
    /// </summary>
    public sealed class AnalyticsEventRouter : MonoBehaviour
    {
        private const int FunnelLevels = 10;

        [SerializeField] private LevelSession session;
        [SerializeField] private MetaGame meta;

        private bool metaBound;
        private bool servicesBound;

        private static IAnalyticsService Analytics => Services.TryGet(out IAnalyticsService service) ? service : null;
        private static ICrashReporter Crash => Services.TryGet(out ICrashReporter reporter) ? reporter : null;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.RunFailed += OnRunFailed;
            GameEvents.NearMiss += OnNearMiss;
            GameEvents.WaveLaunched += OnWaveLaunched;
            GameEvents.FullMoonStarted += OnFullMoon;
            GameEvents.PassengersDelivered += OnPassengersDelivered;
            GameEvents.TreasureFound += OnTreasureFound;
            GameEvents.RewindStarted += OnRewindStarted;
            GameEvents.TutorialStepCompleted += OnTutorialStep;
            meta.Initialized += BindMeta;
            if (meta.IsInitialized)
            {
                BindMeta();
            }
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.RunFailed -= OnRunFailed;
            GameEvents.NearMiss -= OnNearMiss;
            GameEvents.WaveLaunched -= OnWaveLaunched;
            GameEvents.FullMoonStarted -= OnFullMoon;
            GameEvents.PassengersDelivered -= OnPassengersDelivered;
            GameEvents.TreasureFound -= OnTreasureFound;
            GameEvents.RewindStarted -= OnRewindStarted;
            GameEvents.TutorialStepCompleted -= OnTutorialStep;
            meta.Initialized -= BindMeta;
        }

        /// <summary>Hooks SDK-level events (ads, IAP). Called by the boot sequence after those services exist.</summary>
        public void BindServices()
        {
            if (servicesBound)
            {
                return;
            }

            servicesBound = true;
            if (Services.TryGet(out IAdsService ads))
            {
                ads.Impression += OnAdImpression;
                ads.Paid += OnAdPaid;
            }

            if (Services.TryGet(out IIapService iap))
            {
                iap.PurchaseCompleted += OnPurchase;
            }
        }

        private void BindMeta()
        {
            if (metaBound)
            {
                return;
            }

            metaBound = true;
            meta.Lighthouses.StageBuilt += OnLighthouseStage;
            meta.Idle.Collected += OnIdleCollected;
            meta.Spin.Spun += OnSpin;
            meta.Wallet.CoinsEarned += OnCoinsEarned;
            meta.Wallet.CoinsSpent += OnCoinsSpent;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            Crash?.SetCustomKey("state", to.ToString());
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            ICrashReporter crash = Crash;
            if (crash != null)
            {
                crash.SetCustomKey("level", (args.LevelIndex + 1).ToString());
                crash.SetCustomKey("boat", args.BoatId);
                crash.Log($"level_start {args.LevelIndex + 1}");
            }

            Analytics?.LogEvent(AnalyticsEvents.LevelStart,
                AnalyticsParam.Of(AnalyticsEvents.Level, args.LevelIndex + 1),
                AnalyticsParam.Of(AnalyticsEvents.Region, session.Region != null ? session.Region.Id : string.Empty),
                AnalyticsParam.Of(AnalyticsEvents.Boat, args.BoatId));
        }

        private void OnLevelCompleted(LevelResult result)
        {
            int level = result.LevelIndex + 1;
            IAnalyticsService analytics = Analytics;
            if (analytics == null)
            {
                return;
            }

            analytics.LogEvent(AnalyticsEvents.LevelComplete,
                AnalyticsParam.Of(AnalyticsEvents.Level, level),
                AnalyticsParam.Of(AnalyticsEvents.Stars, result.Stars),
                AnalyticsParam.Of(AnalyticsEvents.Duration, Mathf.RoundToInt(result.DurationSeconds)),
                AnalyticsParam.Of(AnalyticsEvents.Score, result.Score),
                AnalyticsParam.Of(AnalyticsEvents.UsedRewind, result.UsedRewind));

            // One event name per early level makes GA4 funnel explorations trivial to build.
            if (level <= FunnelLevels)
            {
                analytics.LogEvent(AnalyticsEvents.FunnelLevelPrefix + level.ToString("D2"));
            }
        }

        private void OnRunFailed(FailReason reason)
        {
            Analytics?.LogEvent(AnalyticsEvents.LevelFail,
                AnalyticsParam.Of(AnalyticsEvents.Level, session.Args.LevelIndex + 1),
                AnalyticsParam.Of(AnalyticsEvents.Reason, ReasonName(reason)),
                AnalyticsParam.Of(AnalyticsEvents.Progress, Mathf.RoundToInt(session.Progress * 100f)));
        }

        private void OnNearMiss(int chain, int multiplier) =>
            Analytics?.LogEvent(AnalyticsEvents.NearMiss, AnalyticsParam.Of(AnalyticsEvents.Chain, chain));

        private void OnWaveLaunched(float strength) =>
            Analytics?.LogEvent(AnalyticsEvents.WaveLaunch, AnalyticsParam.Of(AnalyticsEvents.Strength, System.Math.Round(strength, 2)));

        private void OnFullMoon(float duration) =>
            Analytics?.LogEvent(AnalyticsEvents.FullMoon, AnalyticsParam.Of(AnalyticsEvents.Level, session.Args.LevelIndex + 1));

        private void OnPassengersDelivered(int count) =>
            Analytics?.LogEvent(AnalyticsEvents.PassengerDelivered, AnalyticsParam.Of(AnalyticsEvents.Count, count));

        private void OnTreasureFound(int coins) =>
            Analytics?.LogEvent(AnalyticsEvents.TreasureFound, AnalyticsParam.Of(AnalyticsEvents.Coins, coins));

        private void OnRewindStarted() =>
            Analytics?.LogEvent(AnalyticsEvents.RewindUsed, AnalyticsParam.Of(AnalyticsEvents.Level, session.Args.LevelIndex + 1));

        private void OnTutorialStep(int step, string name) =>
            Analytics?.LogEvent(AnalyticsEvents.TutorialStep, AnalyticsParam.Of(AnalyticsEvents.Step, step), AnalyticsParam.Of("name", name));

        private void OnLighthouseStage(int region, int stage) =>
            Analytics?.LogEvent(AnalyticsEvents.LighthouseStage, AnalyticsParam.Of(AnalyticsEvents.Region, region + 1), AnalyticsParam.Of(AnalyticsEvents.Stage, stage));

        private void OnIdleCollected(long coins, int multiplier) =>
            Analytics?.LogEvent(AnalyticsEvents.IdleIncomeClaimed, AnalyticsParam.Of(AnalyticsEvents.Coins, coins), AnalyticsParam.Of(AnalyticsEvents.Doubled, multiplier > 1));

        private void OnSpin(int segment, bool extra)
        {
            RewardEntry reward = meta.Spin.Segments[segment].Reward;
            Analytics?.LogEvent(AnalyticsEvents.DailySpin,
                AnalyticsParam.Of(AnalyticsEvents.SpinType, extra ? "extra" : "free"),
                AnalyticsParam.Of(AnalyticsEvents.Reward, $"{reward.Type}_{reward.Amount}"));
        }

        private void OnCoinsEarned(long amount, string source) =>
            Analytics?.LogEvent(AnalyticsEvents.EconomyEarn, AnalyticsParam.Of(AnalyticsEvents.Amount, amount), AnalyticsParam.Of(AnalyticsEvents.Source, source));

        private void OnCoinsSpent(long amount, string sink) =>
            Analytics?.LogEvent(AnalyticsEvents.EconomySpend, AnalyticsParam.Of(AnalyticsEvents.Amount, amount), AnalyticsParam.Of(AnalyticsEvents.Source, sink));

        private void OnAdImpression(AdImpressionInfo info) =>
            Analytics?.LogEvent(AnalyticsEvents.AdImpression,
                AnalyticsParam.Of(AnalyticsEvents.AdFormat, info.Format.ToString().ToLowerInvariant()),
                AnalyticsParam.Of(AnalyticsEvents.Placement, info.Placement.ToString()));

        private void OnAdPaid(AdPaidInfo info) =>
            Analytics?.LogEvent(AnalyticsEvents.AdRevenue,
                AnalyticsParam.Of(AnalyticsEvents.Value, info.Value),
                AnalyticsParam.Of(AnalyticsEvents.Currency, info.Currency),
                AnalyticsParam.Of(AnalyticsEvents.Precision, info.Precision),
                AnalyticsParam.Of(AnalyticsEvents.AdFormat, info.Format.ToString().ToLowerInvariant()),
                AnalyticsParam.Of(AnalyticsEvents.Placement, info.Placement.ToString()),
                AnalyticsParam.Of(AnalyticsEvents.AdSource, info.AdSource),
                AnalyticsParam.Of(AnalyticsEvents.AdUnit, info.AdUnitId));

        private void OnPurchase(PurchaseInfo info) =>
            Analytics?.LogEvent(AnalyticsEvents.IapPurchase,
                AnalyticsParam.Of(AnalyticsEvents.ProductId, info.ProductId),
                AnalyticsParam.Of(AnalyticsEvents.Price, (double)info.Price),
                AnalyticsParam.Of(AnalyticsEvents.Currency, info.Currency));

        private static string ReasonName(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.Rock: return "rock";
                case FailReason.Bridge: return "bridge";
                case FailReason.Aground: return "aground";
                case FailReason.Shark: return "shark";
                default: return "kraken";
            }
        }
    }
}
