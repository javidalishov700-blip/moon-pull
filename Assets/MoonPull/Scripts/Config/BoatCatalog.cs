using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Config
{
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
}
