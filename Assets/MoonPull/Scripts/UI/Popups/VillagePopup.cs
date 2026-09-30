using MoonPull.Localization;
using MoonPull.Meta;
using MoonPull.Rescue;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>Build and upgrade the harbor village with coins: one row per building, level pips and an upgrade button.</summary>
    public sealed class VillagePopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private LocalizedText populationLabel;
        [SerializeField] private Text[] levelLabels = new Text[VillageService.BuildingCount];
        [SerializeField] private Button[] upgradeButtons = new Button[VillageService.BuildingCount];
        [SerializeField] private Text[] costLabels = new Text[VillageService.BuildingCount];
        [SerializeField] private GameObject[] maxLabels = new GameObject[VillageService.BuildingCount];

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                upgradeButtons[i].onClick.AddListener(() => Upgrade(building));
            }
        }

        protected override void OnShown() => Refresh();

        private void Upgrade(VillageBuilding building)
        {
            if (VillageService.TryUpgrade(building, meta.Wallet))
            {
                UiTween.Punch(upgradeButtons[(int)building].transform, 0.3f, 0.3f);
            }

            Refresh();
        }

        private void Refresh()
        {
            populationLabel.SetKey(LocKeys.VillagePopulation, PlayerPrefs.GetInt(NightRescue.VillageKey, 0));
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                int level = VillageService.Level(building);
                int cost = VillageService.NextCost(building);
                levelLabels[i].SetText("{0}/{1}", level, VillageService.MaxLevel);
                upgradeButtons[i].gameObject.SetActive(cost >= 0);
                upgradeButtons[i].interactable = cost >= 0 && meta.Wallet.CanAfford(cost);
                costLabels[i].SetText("{0}", Mathf.Max(0, cost));
                maxLabels[i].SetActive(cost < 0);
            }
        }
    }
}
