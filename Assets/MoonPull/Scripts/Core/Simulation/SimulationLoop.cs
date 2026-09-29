using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Core.Simulation
{
    /// <summary>
    /// Single Update entry point for gameplay. Ticking systems from one ordered list removes script-execution-order
    /// bugs (e.g. boat reading last frame's water) and lets the whole world freeze in one place.
    /// </summary>
    public sealed class SimulationLoop : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Ticked top to bottom. Every entry must implement ISimulationTickable.")]
        [SerializeField] private MonoBehaviour[] tickables = new MonoBehaviour[0];

        private ISimulationTickable[] cached;

        /// <summary>Seconds of simulated play in the current level. Restored by rewind.</summary>
        public float LevelTime { get; private set; }

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            cached = new ISimulationTickable[tickables.Length];
            for (int i = 0; i < tickables.Length; i++)
            {
                cached[i] = tickables[i] as ISimulationTickable;
                if (cached[i] == null)
                {
                    Debug.LogError($"SimulationLoop entry {i} ({tickables[i]}) does not implement ISimulationTickable.", this);
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        /// <summary>Sets the level clock, used at level start (0) and when rewind restores a snapshot.</summary>
        public void SetLevelTime(float levelTime)
        {
            LevelTime = levelTime;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            IsRunning = to == GameState.Playing;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, config.MaxSimulationDeltaTime);
            if (dt <= 0f)
            {
                return;
            }

            LevelTime += dt;
            for (int i = 0; i < cached.Length; i++)
            {
                // A tick can end the run (crash → Fail); stop so later systems don't act on a dead boat.
                if (!IsRunning)
                {
                    return;
                }

                cached[i]?.SimulationTick(dt, LevelTime);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < tickables.Length; i++)
            {
                if (tickables[i] != null && !(tickables[i] is ISimulationTickable))
                {
                    Debug.LogWarning($"{tickables[i].GetType().Name} is not an ISimulationTickable and will be ignored.", this);
                }
            }
        }
#endif
    }
}
