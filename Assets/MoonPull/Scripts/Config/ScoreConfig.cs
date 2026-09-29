using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "MoonPull/Config/Score")]
    public sealed class ScoreConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int starPoints = 100;
        [SerializeField, Min(0)] private int coinPoints = 10;
        [SerializeField, Min(0)] private int moonstonePoints = 50;
        [SerializeField, Min(0)] private int chestPoints = 250;
        [SerializeField, Min(0)] private int nearMissPoints = 75;
        [SerializeField, Min(0)] private int passengerPoints = 200;
        [SerializeField, Min(0)] private int bossHitPoints = 500;
        [Tooltip("Multiplier per consecutive near miss: 1st, 2nd, 3rd+.")]
        [SerializeField] private int[] chainMultipliers = { 2, 3, 5 };
        [SerializeField, Min(1)] private int fullMoonMultiplier = 2;
        [Tooltip("Near-miss multiplier assumed when estimating a level's max score for star thresholds.")]
        [SerializeField, Min(1f)] private float estimatedAverageMultiplier = 2f;

        public int StarPoints => starPoints;
        public int CoinPoints => coinPoints;
        public int MoonstonePoints => moonstonePoints;
        public int ChestPoints => chestPoints;
        public int NearMissPoints => nearMissPoints;
        public int PassengerPoints => passengerPoints;
        public int BossHitPoints => bossHitPoints;
        public int[] ChainMultipliers => chainMultipliers;
        public int FullMoonMultiplier => fullMoonMultiplier;
        public float EstimatedAverageMultiplier => estimatedAverageMultiplier;
    }
}
