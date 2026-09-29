using System.Collections.Generic;
using System;
using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Pooling;
using MoonPull.Core.Simulation;
using MoonPull.Obstacles;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Level
{
    /// <summary>Per-placement mutable state, stored in a flat array parallel to <see cref="LevelPlan.Placements"/>.</summary>
    public struct PlacementRuntime
    {
        public bool Consumed;
        public bool Tracking;
        public bool Resolved;
        public float MinClearance;
        public float Timer;
        public int Counter;
        public PlacementView View;

        public void ResetTracking()
        {
            Tracking = false;
            Resolved = false;
            MinClearance = float.MaxValue;
            Timer = 0f;
            Counter = 0;
        }
    }

    /// <summary>
    /// Streams a <see cref="LevelPlan"/> into the world through pools: spawns ahead of the boat, despawns behind it.
    /// Gameplay systems iterate the active window by index, so there are no per-frame allocations or lookups.
    /// </summary>
    public sealed class LevelRunner : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private LevelRunnerConfig config;
        [SerializeField] private PoolManager pools;
        [SerializeField] private PlacementPrefabSet prefabs;
        [SerializeField] private BoatController boat;
        [SerializeField] private Seabed seabed;

        private PlacementRuntime[] runtime = new PlacementRuntime[128];
        private List<LevelPlacement> placements;
        private RegionDefinition region;

        public LevelPlan Plan { get; private set; }

        /// <summary>First placement index still in the world.</summary>
        public int WindowStart { get; private set; }

        /// <summary>One past the last spawned placement index.</summary>
        public int WindowEnd { get; private set; }

        /// <summary>Raised when a placement enters the window, with its index.</summary>
        public event Action<int> PlacementSpawned;

        public LevelPlacement GetPlacement(int index) => placements[index];

        public ref PlacementRuntime GetRuntime(int index) => ref runtime[index];

        public void Load(LevelPlan plan, RegionDefinition regionDefinition)
        {
            Unload();
            Plan = plan;
            placements = plan.Placements;
            region = regionDefinition;

            if (runtime.Length < placements.Count)
            {
                runtime = new PlacementRuntime[Mathf.NextPowerOfTwo(placements.Count)];
            }

            for (int i = 0; i < placements.Count; i++)
            {
                runtime[i] = default;
                runtime[i].ResetTracking();
            }

            seabed.Clear();
            for (int i = 0; i < plan.Shallows.Count; i++)
            {
                ShallowSpan span = plan.Shallows[i];
                seabed.AddSpan(span.StartX, span.EndX, span.Height);
            }

            Resync(boat.X);
        }

        /// <summary>
        /// Rebuilds the visible window around <paramref name="boatX"/>. Used after rewind: everything ahead of the
        /// boat becomes live again, consumed pickups stay consumed.
        /// </summary>
        public void Resync(float boatX)
        {
            DespawnWindow();
            if (placements == null)
            {
                return;
            }

            float boatMinX = boat.HullBounds.MinX;
            int start = 0;
            while (start < placements.Count && placements[start].MaxX + config.DespawnBehind < boatX)
            {
                start++;
            }

            for (int i = start; i < placements.Count; i++)
            {
                if (placements[i].MaxX >= boatMinX)
                {
                    bool consumed = runtime[i].Consumed && !placements[i].IsObstacle;
                    runtime[i].ResetTracking();
                    runtime[i].Consumed = consumed;
                }
            }

            WindowStart = start;
            WindowEnd = start;
            SpawnAhead(boatX);
        }

        public void Unload()
        {
            DespawnWindow();
            Plan = null;
            placements = null;
            WindowStart = 0;
            WindowEnd = 0;
        }

        /// <summary>Marks a placement consumed (collected or smashed) and plays its consume visual.</summary>
        public void Consume(int index)
        {
            ref PlacementRuntime state = ref runtime[index];
            if (state.Consumed)
            {
                return;
            }

            state.Consumed = true;
            state.Resolved = true;
            if (state.View != null)
            {
                state.View.OnConsumed();
            }
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            if (placements == null)
            {
                return;
            }

            SpawnAhead(boat.X);
            DespawnBehind(boat.X);
        }

        private void SpawnAhead(float boatX)
        {
            float limit = boatX + config.SpawnAhead;
            while (WindowEnd < placements.Count && placements[WindowEnd].MinX <= limit)
            {
                int index = WindowEnd++;
                if (runtime[index].Consumed)
                {
                    continue;
                }

                PlacementView prefab = ResolvePrefab(placements[index]);
                if (prefab != null)
                {
                    LevelPlacement placement = placements[index];
                    PlacementView view = pools.Spawn(prefab, new Vector3(placement.X, placement.Y, 0f), Quaternion.identity);
                    view.Setup(index, placement);
                    runtime[index].View = view;
                }

                PlacementSpawned?.Invoke(index);
            }
        }

        private void DespawnBehind(float boatX)
        {
            while (WindowStart < WindowEnd && placements[WindowStart].MaxX + config.DespawnBehind < boatX)
            {
                DespawnView(WindowStart);
                WindowStart++;
            }
        }

        private void DespawnWindow()
        {
            for (int i = WindowStart; i < WindowEnd; i++)
            {
                DespawnView(i);
            }
        }

        private void DespawnView(int index)
        {
            PlacementView view = runtime[index].View;
            if (view != null)
            {
                view.GetComponent<PooledObject>().Despawn();
                runtime[index].View = null;
            }
        }

        private PlacementView ResolvePrefab(in LevelPlacement placement)
        {
            switch (placement.Kind)
            {
                case PlacementKind.LowObstacle:
                    return ResolveObstacle(placement, region != null ? region.LowObstacles : null, prefabs.FallbackLowObstacles);
                case PlacementKind.HighObstacle:
                    return ResolveObstacle(placement, region != null ? region.HighObstacles : null, prefabs.FallbackHighObstacles);
                case PlacementKind.Star: return prefabs.Star;
                case PlacementKind.Coin: return prefabs.Coin;
                case PlacementKind.Moonstone: return prefabs.Moonstone;
                case PlacementKind.Chest: return prefabs.Chest;
                case PlacementKind.Sandbar: return prefabs.Sandbar;
                case PlacementKind.Dock: return prefabs.Dock;
                case PlacementKind.Dolphin: return prefabs.Dolphin;
                case PlacementKind.Whale: return prefabs.Whale;
                case PlacementKind.Shark: return prefabs.Shark;
                case PlacementKind.KrakenSurface: return prefabs.Kraken;
                case PlacementKind.Lighthouse: return prefabs.Lighthouse;
                case PlacementKind.Harbor: return prefabs.Harbor;
                default: return null;
            }
        }

        private PlacementView ResolveObstacle(in LevelPlacement placement, PlacementView[] regionSet, PlacementView[] fallback)
        {
            if (placement.Has(PlacementFlags.Signature) && region != null && region.SignatureObstacle != null
                && region.SignatureKind == placement.Kind)
            {
                return region.SignatureObstacle;
            }

            PlacementView[] set = regionSet != null && regionSet.Length > 0 ? regionSet : fallback;
            return set.Length > 0 ? set[placement.Variant % set.Length] : null;
        }
    }
}
