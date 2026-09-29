using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "Region_", menuName = "MoonPull/Region Definition")]
    public sealed class RegionDefinition : ScriptableObject
    {
        [SerializeField] private string id = "tropical_lagoon";
        [SerializeField] private string nameKey = "region.tropical_lagoon";
        [Tooltip("Total stars needed to unlock this region.")]
        [SerializeField, Min(0)] private int starsToUnlock;

        [Header("Palette")]
        [SerializeField] private Color skyTop = new Color(0.10f, 0.12f, 0.30f);
        [SerializeField] private Color skyBottom = new Color(0.45f, 0.35f, 0.60f);
        [SerializeField] private Color waterShallow = new Color(0.45f, 0.85f, 0.85f);
        [SerializeField] private Color waterDeep = new Color(0.10f, 0.25f, 0.45f);
        [SerializeField] private Color foam = new Color(0.95f, 0.95f, 1f);
        [SerializeField] private Color fog = new Color(0.55f, 0.55f, 0.70f);

        [Header("Audio")]
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;

        [Header("Obstacles")]
        [SerializeField] private PlacementView[] lowObstacles = new PlacementView[0];
        [SerializeField] private PlacementView[] highObstacles = new PlacementView[0];
        [Tooltip("Region's signature obstacle, used in place of a regular one with LevelGenConfig.SignatureChance.")]
        [SerializeField] private PlacementView signatureObstacle;
        [SerializeField] private PlacementKind signatureKind = PlacementKind.LowObstacle;

        [Header("Lighthouse")]
        [Tooltip("Coin cost of each construction stage, in order. Length defines the stage count.")]
        [SerializeField] private int[] lighthouseStageCosts = { 200, 350, 550, 800, 1100, 1500, 2000, 2600 };
        [Tooltip("One visual per stage, enabled cumulatively on the menu island.")]
        [SerializeField] private GameObject[] lighthouseStageVisuals = new GameObject[0];
        [Tooltip("Idle coins per hour once every stage of this lighthouse is built.")]
        [SerializeField, Min(0)] private int idleCoinsPerHour = 60;

        public string Id => id;
        public string NameKey => nameKey;
        public int StarsToUnlock => starsToUnlock;
        public Color SkyTop => skyTop;
        public Color SkyBottom => skyBottom;
        public Color WaterShallow => waterShallow;
        public Color WaterDeep => waterDeep;
        public Color Foam => foam;
        public Color Fog => fog;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;
        public PlacementView[] LowObstacles => lowObstacles;
        public PlacementView[] HighObstacles => highObstacles;
        public PlacementView SignatureObstacle => signatureObstacle;
        public PlacementKind SignatureKind => signatureKind;
        public int[] LighthouseStageCosts => lighthouseStageCosts;
        public GameObject[] LighthouseStageVisuals => lighthouseStageVisuals;
        public int IdleCoinsPerHour => idleCoinsPerHour;
        public int LighthouseStageCount => lighthouseStageCosts.Length;
    }
}
