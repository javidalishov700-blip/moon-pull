using UnityEngine;

namespace MoonPull.Config
{
    public enum MissionType
    {
        NearMisses,
        DeliverPassengers,
        FindChests,
        CompleteLevels,
        CollectStars,
        WaveLaunches,
        FullMoons,
        ThreeStarLevels
    }

    [CreateAssetMenu(fileName = "Mission_", menuName = "MoonPull/Mission Definition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        [SerializeField] private string id = "mission_id";
        [Tooltip("Localization key with a {0} placeholder for the target, e.g. \"Do {0} near misses\".")]
        [SerializeField] private string descriptionKey = "mission.near_misses";
        [SerializeField] private MissionType type;
        [SerializeField, Min(1)] private int target = 5;
        [SerializeField, Min(0)] private int rewardCoins = 100;
        [Tooltip("Mission is only offered once the player has reached this level index (e.g. passengers after level 6).")]
        [SerializeField, Min(0)] private int minLevelIndex;

        public string Id => id;
        public string DescriptionKey => descriptionKey;
        public MissionType Type => type;
        public int Target => target;
        public int RewardCoins => rewardCoins;
        public int MinLevelIndex => minLevelIndex;
    }

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
