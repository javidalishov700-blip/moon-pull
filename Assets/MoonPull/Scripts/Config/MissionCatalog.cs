using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "MissionCatalog", menuName = "MoonPull/Catalog/Missions")]
    public sealed class MissionCatalog : ScriptableObject
    {
        [SerializeField] private MissionDefinition[] missions = new MissionDefinition[0];
        [SerializeField, Min(1)] private int missionsPerDay = 3;

        public MissionDefinition[] Missions => missions;
        public int MissionsPerDay => missionsPerDay;

        public MissionDefinition Find(string id)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                if (missions[i] != null && missions[i].Id == id)
                {
                    return missions[i];
                }
            }

            return null;
        }
    }
}
