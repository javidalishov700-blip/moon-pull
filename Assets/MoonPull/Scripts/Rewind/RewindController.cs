using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Gameplay.CameraControl;
using MoonPull.Layers.Boss;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Rewind
{
    /// <summary>
    /// Records world snapshots at a fixed rate while playing (tick it last) and, on "Rewind the Tide", plays them back
    /// in reverse and resumes from the oldest one. Restores snapshots rather than re-simulating, so it is exact.
    /// </summary>
    public sealed class RewindController : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private RewindConfig config;
        [SerializeField] private SimulationLoop loop;
        [SerializeField] private MoonController moon;
        [SerializeField] private TideModel tide;
        [SerializeField] private WaterSurface water;
        [SerializeField] private BoatController boat;
        [SerializeField] private FullMoonMode fullMoon;
        [SerializeField] private KrakenBoss kraken;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private ObstacleInteractionSystem obstacles;
        [SerializeField] private CameraRig cameraRig;

        private RingBuffer<WorldSnapshot> buffer;
        private float sampleAccumulator;
        private int rewindsUsed;
        private bool playing;

        // Free "Tide Turn" rewinds per level: a crash rewinds a few seconds instead of failing. Only when they run
        // out does the Fail screen appear (where the rewarded "Rewind the Tide" is still offered).
        private const int FreeRewindsPerLevel = 3;
        private int freeRewindsLeft;
        private bool pendingFree;
        private float playbackElapsed;

        /// <summary>True when the Fail screen may offer "Rewind the Tide".</summary>
        public bool CanRewind =>
            rewindsUsed < config.MaxRewindsPerLevel
            && buffer.Count >= Mathf.CeilToInt(config.MinimumHistorySeconds * config.SamplesPerSecond);

        private void Awake()
        {
            buffer = new RingBuffer<WorldSnapshot>(config.Capacity);
        }

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.TryFreeRewind = TryFreeRewind;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.StateChanged -= OnStateChanged;
            if (GameEvents.TryFreeRewind == TryFreeRewind)
            {
                GameEvents.TryFreeRewind = null;
            }
        }

        private bool TryFreeRewind()
        {
            int needed = Mathf.CeilToInt(config.MinimumHistorySeconds * config.SamplesPerSecond);
            if (freeRewindsLeft <= 0 || buffer.Count < needed)
            {
                return false;
            }

            freeRewindsLeft--;
            pendingFree = true;
            return true;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            sampleAccumulator += deltaTime;
            float interval = 1f / config.SamplesPerSecond;
            if (sampleAccumulator < interval)
            {
                return;
            }

            sampleAccumulator -= interval;
            buffer.Push(Capture());
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            buffer.Clear();
            sampleAccumulator = 0f;
            rewindsUsed = 0;
            playing = false;
            freeRewindsLeft = FreeRewindsPerLevel;
            pendingFree = false;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Rewinding)
            {
                BeginPlayback();
            }
            else if (from == GameState.Rewinding)
            {
                playing = false;
            }
        }

        private void BeginPlayback()
        {
            if (buffer.Count == 0)
            {
                GameEvents.RaiseRewindFinished();
                return;
            }

            if (pendingFree)
            {
                pendingFree = false;
                GameEvents.RaiseTideTurned(freeRewindsLeft);
            }
            else
            {
                rewindsUsed++;
            }

            playing = true;
            playbackElapsed = 0f;
            GameEvents.RaiseRewindStarted();
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            playbackElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(playbackElapsed / config.PlaybackSeconds);
            // Ease-out so the reverse starts fast (dramatic) and settles on the resume point.
            float eased = 1f - (1f - t) * (1f - t);
            int stepsBack = Mathf.RoundToInt(eased * (buffer.Count - 1));
            Apply(buffer.FromNewest(stepsBack));

            if (t >= 1f)
            {
                FinishPlayback();
            }
        }

        private void FinishPlayback()
        {
            playing = false;
            WorldSnapshot resumePoint = buffer.Oldest;
            Apply(resumePoint);
            buffer.Clear();
            sampleAccumulator = 0f;

            runner.Resync(boat.X);
            obstacles.BreakChain();
            boat.GrantInvulnerability(config.ResumeInvulnerability);
            cameraRig.Snap();
            GameEvents.RaiseRewindFinished();
        }

        private WorldSnapshot Capture()
        {
            return new WorldSnapshot
            {
                LevelTime = loop.LevelTime,
                Moon = moon.CaptureState(),
                Tide = tide.CaptureState(),
                Water = water.CaptureState(),
                Boat = boat.CaptureState(),
                FullMoon = fullMoon.CaptureState(),
                KrakenHits = kraken.Hits
            };
        }

        private void Apply(in WorldSnapshot snapshot)
        {
            loop.SetLevelTime(snapshot.LevelTime);
            moon.RestoreState(snapshot.Moon);
            tide.RestoreState(snapshot.Tide);
            water.RestoreState(snapshot.Water);
            boat.RestoreState(snapshot.Boat);
            fullMoon.RestoreState(snapshot.FullMoon);
            kraken.RestoreHits(snapshot.KrakenHits);
        }
    }
}
