using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Level;
using MoonPull.Mechanics;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Layers.Passengers
{
    /// <summary>
    /// Holding the tide at dock height while sailing past boards the waiting passengers; they pay out at the harbor.
    /// Tick before LevelSession so delivery is counted in the same frame the level completes.
    /// </summary>
    public sealed class PassengerSystem : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private PassengerConfig config;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private TideModel tide;
        [SerializeField] private ScoreSystem score;

        public int Onboard { get; private set; }
        public int Delivered { get; private set; }

        /// <summary>True while a dock is close enough that the HUD should show the alignment gauge.</summary>
        public bool GaugeVisible { get; private set; }

        /// <summary>Signed normalized difference between sea level and the upcoming dock (positive = water too high).</summary>
        public float AlignmentOffset { get; private set; }

        public bool IsAligned { get; private set; }

        public float AlignmentTolerance => config.AlignmentTolerance;

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
        }

        private void OnLevelStarted(LevelStartArgs args)
        {
            Onboard = 0;
            Delivered = 0;
            GaugeVisible = false;
            IsAligned = false;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            LevelPlan plan = runner.Plan;
            if (plan == null)
            {
                return;
            }

            GaugeVisible = false;
            IsAligned = false;
            float boatX = boat.X;

            for (int i = runner.WindowStart; i < runner.WindowEnd; i++)
            {
                LevelPlacement dock = runner.GetPlacement(i);
                if (dock.Kind != PlacementKind.Dock)
                {
                    continue;
                }

                ref PlacementRuntime state = ref runner.GetRuntime(i);
                if (state.Resolved || dock.MaxX < boatX || dock.MinX > boatX + config.GaugeLookAhead)
                {
                    if (!state.Resolved && dock.MaxX < boatX)
                    {
                        state.Resolved = true;
                    }

                    continue;
                }

                GaugeVisible = true;
                AlignmentOffset = tide.Level01 - dock.Tide01;
                IsAligned = Mathf.Abs(AlignmentOffset) <= config.AlignmentTolerance && !boat.IsAirborne;

                bool beside = boatX >= dock.MinX && boatX <= dock.MaxX;
                if (beside && IsAligned)
                {
                    state.Timer += deltaTime;
                    if (state.Timer >= config.BoardSeconds)
                    {
                        Board(i, dock.Value);
                    }
                }

                break;
            }

            if (Onboard > 0 && boatX >= plan.HarborX)
            {
                Deliver();
            }
        }

        private void Board(int index, int passengers)
        {
            runner.Consume(index);
            Onboard += passengers;
            GameEvents.RaisePassengerBoarded(Onboard);
        }

        private void Deliver()
        {
            int count = Onboard;
            Onboard = 0;
            Delivered += count;
            score.Add(ScoreSource.Passenger, count);
            GameEvents.RaisePassengersDelivered(count);
        }
    }
}
