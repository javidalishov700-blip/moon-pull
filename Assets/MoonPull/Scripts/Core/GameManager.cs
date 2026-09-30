using UnityEngine;

namespace MoonPull.Core
{
    /// <summary>
    /// Owns the <see cref="GameStateMachine"/>. The only class that changes game state; everything else raises intents
    /// through <see cref="GameEvents"/> and reacts to <see cref="GameEvents.StateChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        private readonly GameStateMachine machine = new GameStateMachine(GameState.Boot);

        public GameState State => machine.Current;

        /// <summary>Arguments of the level currently being played or last played.</summary>
        public LevelStartArgs CurrentLevel { get; private set; }

        /// <summary>Result of the last won level; valid while in <see cref="GameState.Win"/>.</summary>
        public LevelResult LastResult { get; private set; }

        /// <summary>Last failure reason; valid while in <see cref="GameState.Fail"/>.</summary>
        public FailReason LastFailReason { get; private set; }

        private void Awake()
        {
            machine.Changed += GameEvents.RaiseStateChanged;
        }

        private void OnEnable()
        {
            GameEvents.PlayRequested += OnPlayRequested;
            GameEvents.RestartRequested += OnRestartRequested;
            GameEvents.MenuRequested += OnMenuRequested;
            GameEvents.ShopRequested += OnShopRequested;
            GameEvents.RewindGranted += OnRewindGranted;
            GameEvents.RewindFinished += OnRewindFinished;
            GameEvents.RunFailed += OnRunFailed;
            GameEvents.LevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            GameEvents.PlayRequested -= OnPlayRequested;
            GameEvents.RestartRequested -= OnRestartRequested;
            GameEvents.MenuRequested -= OnMenuRequested;
            GameEvents.ShopRequested -= OnShopRequested;
            GameEvents.RewindGranted -= OnRewindGranted;
            GameEvents.RewindFinished -= OnRewindFinished;
            GameEvents.RunFailed -= OnRunFailed;
            GameEvents.LevelCompleted -= OnLevelCompleted;
        }

        public void EnterConsent() => Transition(GameState.Consent);

        public void CompleteBoot() => Transition(GameState.Menu);

        private void OnPlayRequested(LevelStartArgs args)
        {
            if (!machine.CanTransition(GameState.Playing))
            {
                Debug.LogWarning($"Ignored play request in state {machine.Current}.");
                return;
            }

            // Set before transitioning so StateChanged listeners already see the new level.
            CurrentLevel = args;
            Transition(GameState.Playing);
            GameEvents.RaiseLevelStartRequested(args);
        }

        private void OnRestartRequested()
        {
            if (Transition(GameState.Playing))
            {
                GameEvents.RaiseLevelStartRequested(CurrentLevel);
            }
        }

        private void OnMenuRequested() => Transition(GameState.Menu);

        private void OnShopRequested() => Transition(GameState.Shop);

        private void OnRewindGranted() => Transition(GameState.Rewinding);

        private void OnRewindFinished() => Transition(GameState.Playing);

        private void OnRunFailed(FailReason reason)
        {
            LastFailReason = reason;
            // Forgiving by design: the moon turns the tide back a few seconds instead of ending the run.
            if (GameEvents.TryFreeRewind != null && GameEvents.TryFreeRewind() && Transition(GameState.Rewinding))
            {
                return;
            }

            Transition(GameState.Fail);
        }

        private void OnLevelCompleted(LevelResult result)
        {
            LastResult = result;
            Transition(GameState.Win);
        }

        private bool Transition(GameState target)
        {
            if (machine.TryTransition(target))
            {
                return true;
            }

            Debug.LogWarning($"Ignored illegal transition {machine.Current} → {target}.");
            return false;
        }
    }
}
