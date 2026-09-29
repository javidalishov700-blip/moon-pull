using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "RegionCatalog", menuName = "MoonPull/Catalog/Regions")]
    public sealed class RegionCatalog : ScriptableObject
    {
        [SerializeField] private RegionDefinition[] regions = new RegionDefinition[0];

        public int Count => regions.Length;

        public RegionDefinition this[int index] => regions[Mathf.Clamp(index, 0, regions.Length - 1)];

        public RegionDefinition ForLevel(int levelIndex, int levelsPerRegion) => this[levelIndex / Mathf.Max(1, levelsPerRegion)];
    }

    [CreateAssetMenu(fileName = "BoatCatalog", menuName = "MoonPull/Catalog/Boats")]
    public sealed class BoatCatalog : ScriptableObject
    {
        [SerializeField] private BoatDefinition[] boats = new BoatDefinition[0];
        [SerializeField] private BoatDefinition defaultBoat;

        public int Count => boats.Length;
        public BoatDefinition this[int index] => boats[index];
        public BoatDefinition Default => defaultBoat != null ? defaultBoat : boats.Length > 0 ? boats[0] : null;

        /// <summary>Finds a boat by id, falling back to the default so a renamed id can never break a save.</summary>
        public BoatDefinition Find(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < boats.Length; i++)
                {
                    if (boats[i] != null && boats[i].Id == id)
                    {
                        return boats[i];
                    }
                }
            }

            return Default;
        }
    }

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
