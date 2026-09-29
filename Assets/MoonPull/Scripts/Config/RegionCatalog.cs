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
}
