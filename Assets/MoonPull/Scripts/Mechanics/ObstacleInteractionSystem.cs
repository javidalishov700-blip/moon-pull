using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Mechanics
{
    /// <summary>
    /// Collision and near-miss detection. Tracks the minimum clearance while the boat is under/over an obstacle and
    /// judges the near miss when the boat has fully passed it.
    /// </summary>
    public sealed class ObstacleInteractionSystem : MonoBehaviour, ISimulationTickable
    {
        private const float SolidExtent = 100f;

        [SerializeField] private NearMissConfig config;
        [SerializeField] private ScoreConfig scoreConfig;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private ScoreSystem score;

        private NearMissChain chain;

        public int NearMissCount { get; private set; }

        /// <summary>Obstacles cleared since level start. The tutorial listens to know when to hide its hint.</summary>
        public int ObstaclesPassed { get; private set; }

        private void Awake()
        {
            chain = new NearMissChain(scoreConfig.ChainMultipliers);
        }

        public void ResetForLevel()
        {
            chain.Break();
            NearMissCount = 0;
            ObstaclesPassed = 0;
        }

        /// <summary>Called after rewind so a chain does not survive into the replayed section.</summary>
        public void BreakChain()
        {
            if (chain.Break())
            {
                score.SetChainMultiplier(1);
                GameEvents.RaiseNearMissChainBroken();
            }
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (runner.Plan == null)
            {
                return;
            }

            Box2 hull = boat.HullBounds;
            Box2 mast = boat.MastBounds;
            float threshold = config.ClearanceThreshold * boat.Modifiers.NearMissWindowMultiplier;

            for (int i = runner.WindowStart; i < runner.WindowEnd; i++)
            {
                LevelPlacement placement = runner.GetPlacement(i);
                if (!placement.IsObstacle)
                {
                    continue;
                }

                ref PlacementRuntime state = ref runner.GetRuntime(i);
                if (state.Resolved || state.Consumed)
                {
                    continue;
                }

                bool isLow = placement.Kind == PlacementKind.LowObstacle;
                Box2 obstacle = isLow
                    ? new Box2(placement.MinX, placement.Y - SolidExtent, placement.MaxX, placement.Y)
                    : new Box2(placement.MinX, placement.Y, placement.MaxX, placement.Y + SolidExtent);
                Box2 probe = isLow ? hull : mast;

                if (probe.OverlapsX(obstacle))
                {
                    state.Tracking = true;
                    float clearance = isLow ? hull.MinY - placement.Y : placement.Y - mast.MaxY;
                    if (clearance < state.MinClearance)
                    {
                        state.MinClearance = clearance;
                    }

                    if (clearance < 0f && !HandleContact(i, isLow))
                    {
                        return;
                    }
                }
                else if (state.Tracking && obstacle.MaxX <= probe.MinX)
                {
                    state.Resolved = true;
                    ObstaclesPassed++;
                    JudgePass(state.MinClearance, threshold);
                }
            }
        }

        // Returns false when the run ended and ticking must stop.
        private bool HandleContact(int index, bool isLow)
        {
            if (boat.IsProtected)
            {
                runner.Consume(index);
                ObstaclesPassed++;
                return true;
            }

            if (boat.TryCrash(isLow ? FailReason.Rock : FailReason.Bridge))
            {
                return false;
            }

            // A shield absorbed the hit: smash the obstacle so the boat is not hit again next frame.
            runner.Consume(index);
            ObstaclesPassed++;
            BreakChain();
            return true;
        }

        private void JudgePass(float minClearance, float threshold)
        {
            if (minClearance >= 0f && minClearance < threshold)
            {
                NearMissCount++;
                int multiplier = chain.Register();
                score.SetChainMultiplier(multiplier);
                score.Add(ScoreSource.NearMiss);
                GameEvents.RaiseNearMiss(chain.Length, multiplier);
            }
            else
            {
                BreakChain();
            }
        }
    }
}
