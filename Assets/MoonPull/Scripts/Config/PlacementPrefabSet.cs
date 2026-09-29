using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Config
{
    /// <summary>Prefabs for every non-obstacle placement plus fallback obstacles when a region has none.</summary>
    [CreateAssetMenu(fileName = "PlacementPrefabSet", menuName = "MoonPull/Catalog/Placement Prefabs")]
    public sealed class PlacementPrefabSet : ScriptableObject
    {
        [SerializeField] private PlacementView[] fallbackLowObstacles = new PlacementView[0];
        [SerializeField] private PlacementView[] fallbackHighObstacles = new PlacementView[0];
        [SerializeField] private PlacementView star;
        [SerializeField] private PlacementView coin;
        [SerializeField] private PlacementView moonstone;
        [SerializeField] private PlacementView chest;
        [SerializeField] private PlacementView sandbar;
        [SerializeField] private PlacementView dock;
        [SerializeField] private PlacementView dolphin;
        [SerializeField] private PlacementView whale;
        [SerializeField] private PlacementView shark;
        [SerializeField] private PlacementView kraken;
        [SerializeField] private PlacementView lighthouse;
        [SerializeField] private PlacementView harbor;

        public PlacementView[] FallbackLowObstacles => fallbackLowObstacles;
        public PlacementView[] FallbackHighObstacles => fallbackHighObstacles;
        public PlacementView Star => star;
        public PlacementView Coin => coin;
        public PlacementView Moonstone => moonstone;
        public PlacementView Chest => chest;
        public PlacementView Sandbar => sandbar;
        public PlacementView Dock => dock;
        public PlacementView Dolphin => dolphin;
        public PlacementView Whale => whale;
        public PlacementView Shark => shark;
        public PlacementView Kraken => kraken;
        public PlacementView Lighthouse => lighthouse;
        public PlacementView Harbor => harbor;
    }
}
