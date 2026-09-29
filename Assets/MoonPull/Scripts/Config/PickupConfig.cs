using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "PickupConfig", menuName = "MoonPull/Config/Pickups")]
    public sealed class PickupConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float starRadius = 0.45f;
        [SerializeField, Min(0f)] private float coinRadius = 0.35f;
        [SerializeField, Min(0f)] private float moonstoneRadius = 0.5f;
        [Tooltip("How close the keel must get to the seabed to grab a chest.")]
        [SerializeField, Min(0f)] private float chestReach = 0.35f;
        [SerializeField, Min(1)] private int coinValue = 1;
        [Tooltip("Stars collected within this many seconds of each other build a pitch-rising streak.")]
        [SerializeField, Min(0.1f)] private float starStreakWindow = 1.2f;

        public float StarRadius => starRadius;
        public float CoinRadius => coinRadius;
        public float MoonstoneRadius => moonstoneRadius;
        public float ChestReach => chestReach;
        public int CoinValue => coinValue;
        public float StarStreakWindow => starStreakWindow;
    }
}
