using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Level;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Layers.Creatures
{
    /// <summary>
    /// Dolphins boost speed, whales surface at high tide and act as a launch ramp, sharks bite at very low tide.
    /// Each creature is a pure tide question, so they deepen the core mechanic instead of adding a new one.
    /// </summary>
    public sealed class CreatureSystem : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private CreatureConfig config;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private TideModel tide;

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (runner.Plan == null)
            {
                return;
            }

            float boatX = boat.X;
            float tide01 = tide.Level01;

            for (int i = runner.WindowStart; i < runner.WindowEnd; i++)
            {
                LevelPlacement placement = runner.GetPlacement(i);
                switch (placement.Kind)
                {
                    case PlacementKind.Dolphin:
                        TickDolphin(i, placement, boatX);
                        break;
                    case PlacementKind.Whale:
                        TickWhale(i, placement, boatX, tide01);
                        break;
                    case PlacementKind.Shark:
                        if (!TickShark(i, placement, boatX, tide01, deltaTime))
                        {
                            return;
                        }

                        break;
                }
            }
        }

        private void TickDolphin(int index, in LevelPlacement placement, float boatX)
        {
            ref PlacementRuntime state = ref runner.GetRuntime(index);
            if (state.Resolved || boatX < placement.MinX)
            {
                return;
            }

            state.Resolved = true;
            boat.ApplySpeedBoost(config.DolphinBoostMultiplier, config.DolphinBoostSeconds);
            if (state.View != null)
            {
                state.View.OnTriggered();
            }
        }

        private void TickWhale(int index, in LevelPlacement placement, float boatX, float tide01)
        {
            ref PlacementRuntime state = ref runner.GetRuntime(index);
            bool surfaced = tide01 >= placement.Tide01;
            if (state.View != null)
            {
                state.View.SetEngaged(surfaced && !state.Resolved);
            }

            if (state.Resolved)
            {
                return;
            }

            if (boatX > placement.MaxX)
            {
                state.Resolved = true;
                return;
            }

            bool onRamp = boatX >= placement.MinX && boatX <= placement.MaxX;
            if (onRamp && surfaced && !boat.IsAirborne)
            {
                state.Resolved = true;
                boat.Launch(config.WhaleLaunchVelocity);
                if (state.View != null)
                {
                    state.View.OnTriggered();
                }
            }
        }

        // Returns false when the shark ended the run.
        private bool TickShark(int index, in LevelPlacement placement, float boatX, float tide01, float deltaTime)
        {
            ref PlacementRuntime state = ref runner.GetRuntime(index);
            bool near = boatX >= placement.MinX - config.EngageDistance && boatX <= placement.MaxX;
            bool inside = boatX >= placement.MinX && boatX <= placement.MaxX;
            bool danger = tide01 < placement.Tide01 && !boat.IsAirborne;

            if (state.View != null)
            {
                state.View.SetEngaged(near && danger);
            }

            if (!inside || !danger)
            {
                state.Timer = 0f;
                return true;
            }

            state.Timer += deltaTime;
            if (state.Timer < config.SharkBiteDelay)
            {
                return true;
            }

            state.Timer = 0f;
            if (state.View != null)
            {
                state.View.OnTriggered();
            }

            return !boat.TryCrash(FailReason.Shark);
        }
    }
}
