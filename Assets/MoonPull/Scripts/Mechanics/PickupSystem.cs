using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Mechanics
{
    /// <summary>Collects stars, coins, moonstones and seabed chests the boat touches.</summary>
    public sealed class PickupSystem : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private PickupConfig config;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private ScoreSystem score;
        [SerializeField] private FullMoonMode fullMoon;

        private float lastStarTime = float.MinValue;
        private int starStreak;

        public int StarsCollected { get; private set; }
        public int CoinsCollected { get; private set; }
        public int TreasuresFound { get; private set; }

        public void ResetForLevel()
        {
            StarsCollected = 0;
            CoinsCollected = 0;
            TreasuresFound = 0;
            starStreak = 0;
            lastStarTime = float.MinValue;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (runner.Plan == null)
            {
                return;
            }

            Box2 hull = boat.HullBounds;
            float bonus = boat.Modifiers.PickupRadiusBonus;

            for (int i = runner.WindowStart; i < runner.WindowEnd; i++)
            {
                LevelPlacement placement = runner.GetPlacement(i);
                if (placement.MinX > hull.MaxX + config.MoonstoneRadius + bonus)
                {
                    break;
                }

                ref PlacementRuntime state = ref runner.GetRuntime(i);
                if (state.Consumed)
                {
                    continue;
                }

                switch (placement.Kind)
                {
                    case PlacementKind.Star:
                        if (Touches(hull, placement, config.StarRadius + bonus))
                        {
                            runner.Consume(i);
                            CollectStar(levelTime);
                        }

                        break;
                    case PlacementKind.Coin:
                        if (Touches(hull, placement, config.CoinRadius + bonus))
                        {
                            runner.Consume(i);
                            CoinsCollected += config.CoinValue;
                            score.Add(ScoreSource.Coin);
                            GameEvents.RaiseCoinCollected(config.CoinValue);
                        }

                        break;
                    case PlacementKind.Moonstone:
                        if (Touches(hull, placement, config.MoonstoneRadius + bonus))
                        {
                            runner.Consume(i);
                            score.Add(ScoreSource.Moonstone);
                            fullMoon.AddMoonstone();
                        }

                        break;
                    case PlacementKind.Chest:
                        if (ReachesChest(hull, placement))
                        {
                            runner.Consume(i);
                            TreasuresFound++;
                            CoinsCollected += placement.Value;
                            score.Add(ScoreSource.Chest);
                            GameEvents.RaiseTreasureFound(placement.Value);
                        }

                        break;
                }
            }
        }

        private void CollectStar(float levelTime)
        {
            starStreak = levelTime - lastStarTime <= config.StarStreakWindow ? starStreak + 1 : 1;
            lastStarTime = levelTime;
            StarsCollected++;
            score.Add(ScoreSource.Star);
            GameEvents.RaiseStarCollected(starStreak);
        }

        private static bool Touches(in Box2 hull, in LevelPlacement placement, float radius) =>
            placement.X > hull.MinX - radius && placement.X < hull.MaxX + radius
            && placement.Y > hull.MinY - radius && placement.Y < hull.MaxY + radius;

        private bool ReachesChest(in Box2 hull, in LevelPlacement placement)
        {
            bool overX = placement.X > hull.MinX - placement.Width * 0.5f && placement.X < hull.MaxX + placement.Width * 0.5f;
            float keel = boat.Y - boat.Config.Draft;
            return overX && keel - placement.Y <= config.ChestReach;
        }
    }
}
