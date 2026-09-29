using UnityEngine;

namespace MoonPull.Config
{
    public enum BoatRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum BoatPerkType
    {
        None,
        CoinBonusPercent,
        CrashShields,
        LaunchBonusPercent,
        NearMissWindowPercent,
        FullMoonBonusSeconds,
        StartingMoonstones,
        PassengerBonusPercent,
        PickupRadiusBonus,
        IdleIncomeBonusPercent
    }

    [CreateAssetMenu(fileName = "Boat_", menuName = "MoonPull/Boat Definition")]
    public sealed class BoatDefinition : ScriptableObject
    {
        [SerializeField] private string id = "boat_id";
        [SerializeField] private string nameKey = "boat.id.name";
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private BoatRarity rarity;
        [Tooltip("Coin price. 0 = free/default. Ignored when IapOnly.")]
        [SerializeField, Min(0)] private int price;
        [Tooltip("Only obtainable through an IAP bundle (e.g. Starter Pack).")]
        [SerializeField] private bool iapOnly;
        [SerializeField] private BoatPerkType perk;
        [SerializeField] private float perkValue;
        [SerializeField] private Color debrisTint = Color.white;

        public string Id => id;
        public string NameKey => nameKey;
        public GameObject ModelPrefab => modelPrefab;
        public Sprite Icon => icon;
        public BoatRarity Rarity => rarity;
        public int Price => price;
        public bool IapOnly => iapOnly;
        public BoatPerkType Perk => perk;
        public float PerkValue => perkValue;
        public Color DebrisTint => debrisTint;
    }
}
