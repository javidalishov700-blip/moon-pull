using System;
using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Save;
using UnityEngine;

namespace MoonPull.Meta
{
    /// <summary>
    /// Composition root of the meta layer. Owns the meta services (not global singletons) and turns level results into
    /// progress and coins. UI reads everything through this component.
    /// </summary>
    public sealed class MetaGame : MonoBehaviour
    {
        [SerializeField] private EconomyConfig economy;
        [SerializeField] private LevelGenConfig generation;
        [SerializeField] private RegionCatalog regions;
        [SerializeField] private BoatCatalog boats;
        [SerializeField] private MissionCatalog missions;

        private ISaveService save;
        private bool hasLastReward;

        public event Action Initialized;

        /// <summary>Raised after a won level has been paid out, with the reward.</summary>
        public event Action<LevelReward> LevelRewarded;

        public bool IsInitialized { get; private set; }
        public Wallet Wallet { get; private set; }
        public LevelProgress Progress { get; private set; }
        public LighthouseService Lighthouses { get; private set; }
        public IdleIncomeService Idle { get; private set; }
        public BoatCollection Boats { get; private set; }
        public DailyMissionService Missions { get; private set; }
        public DailySpinService Spin { get; private set; }
        public LoginStreakService Streak { get; private set; }
        public BossChestService BossChests { get; private set; }
        public EconomyConfig Economy => economy;
        public RegionCatalog Regions => regions;

        public LevelReward LastReward { get; private set; }

        /// <summary>True when the last won level beat its previous best score.</summary>
        public bool LastResultWasNewBest { get; private set; }
        public bool LastRewardMultiplied { get; private set; }
        public bool CanMultiplyLastReward => hasLastReward && !LastRewardMultiplied && LastReward.Total > 0;

        /// <summary>"Triple Your Reward" multiplier; overridable by Remote Config.</summary>
        public int RewardedLevelMultiplier { get; set; }

        /// <summary>"Double Idle Earnings" multiplier; overridable by Remote Config.</summary>
        public int IdleRewardedMultiplier { get; set; }

        /// <summary>Hours after first launch the discounted starter pack is offered; overridable by Remote Config.</summary>
        public float StarterPackIntroHours { get; set; } = 48f;

        public void Initialize(ISaveService saveService, IClock clock)
        {
            save = saveService;
            RewardedLevelMultiplier = economy.RewardedLevelMultiplier;
            IdleRewardedMultiplier = economy.IdleRewardedMultiplier;

            Wallet = new Wallet(save);
            Progress = new LevelProgress(save, generation, regions);
            Lighthouses = new LighthouseService(save, regions, Wallet);
            Boats = new BoatCollection(save, boats, Wallet);
            Idle = new IdleIncomeService(save, economy, clock, Wallet, CurrentIdleRate);
            Missions = new DailyMissionService(save, missions, clock, Wallet, Progress);
            Spin = new DailySpinService(save, economy, clock, Wallet);
            Streak = new LoginStreakService(save, economy, clock, Wallet);
            BossChests = new BossChestService(save, economy, Wallet, clock);

            Idle.EnsureBaseline();
            Missions.Refresh();
            IsInitialized = true;
            Initialized?.Invoke();
        }

        private void OnEnable()
        {
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.AppResumed += OnAppResumed;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.AppResumed -= OnAppResumed;
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void OnDestroy()
        {
            Missions?.Dispose();
        }

        /// <summary>Starts a level with the selected (or trial) boat.</summary>
        public void Play(int levelIndex)
        {
            BoatDefinition boat = Boats.BoatForNextLevel;
            GameEvents.RaisePlayRequested(new LevelStartArgs(levelIndex, boat != null ? boat.Id : string.Empty, Boats.TrialBoat != null));
        }

        public void PlayNext() => Play(Progress.NextLevelToPlay());

        /// <summary>Builds the next lighthouse stage. Banks idle coins first so the new rate never applies retroactively.</summary>
        public bool BuildLighthouseStage(int regionIndex)
        {
            if (!Progress.IsRegionUnlocked(regionIndex) || !Wallet.CanAfford(Lighthouses.NextStageCost(regionIndex)))
            {
                return false;
            }

            Idle.Collect(1);
            bool built = Lighthouses.TryBuildNext(regionIndex);
            save.SaveNow();
            return built;
        }

        /// <summary>Grants the extra coins after the "Triple Your Reward" ad paid out. Returns the bonus.</summary>
        public int ApplyRewardMultiplier()
        {
            if (!CanMultiplyLastReward)
            {
                return 0;
            }

            int bonus = EconomyCalculator.RewardedBonus(LastReward.Total, RewardedLevelMultiplier);
            Wallet.AddCoins(bonus, "level_multiplied");
            LastRewardMultiplied = true;
            save.SaveNow();
            return bonus;
        }

        private float CurrentIdleRate() => Lighthouses.IdleCoinsPerHour(Boats.SelectedModifiers.IdleIncomeMultiplier);

        private void OnLevelCompleted(LevelResult result)
        {
            if (!IsInitialized)
            {
                return;
            }

            bool isReplay = Progress.IsCompleted(result.LevelIndex);
            LastResultWasNewBest = isReplay && result.Score > Progress.BestScoreFor(result.LevelIndex);
            Progress.Record(result);

            BoatModifiers modifiers = BoatModifiers.From(Boats.BoatForNextLevel);
            LastReward = EconomyCalculator.ForLevel(economy, result, isReplay, modifiers.CoinMultiplier, modifiers.PassengerCoinMultiplier);
            LastRewardMultiplied = false;
            hasLastReward = true;
            Wallet.AddCoins(LastReward.Total, "level");

            if (result.BossDefeated)
            {
                BossChests.AddChest();
            }

            Boats.EndTrial();
            save.SaveNow();
            LevelRewarded?.Invoke(LastReward);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu && IsInitialized)
            {
                Boats.EndTrial();
                save.SaveIfDirty();
            }
        }

        private void OnAppResumed(float secondsAway)
        {
            if (IsInitialized)
            {
                Missions.Refresh();
            }
        }
    }
}
