using MoonPull.Localization;
using MoonPull.Meta;
using MoonPull.Rescue;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>
    /// The Village screen: a bottom sheet over the live village view. Shows the village level and XP, happiness,
    /// food and homes, and one row per building with its level and a Build button. Buildings can grow only as far as
    /// the village level allows, so caring for villagers is what unlocks bigger buildings.
    /// </summary>
    public sealed class VillagePopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private VillageDirector director;
        [SerializeField] private CanvasGroup hideWhileOpen;
        [SerializeField] private LocalizedText populationLabel;
        [SerializeField] private LocalizedText levelLabel;
        [SerializeField] private Image xpFill;
        [SerializeField] private LocalizedText happinessLabel;
        [SerializeField] private Image happinessFill;
        [SerializeField] private LocalizedText foodLabel;
        [SerializeField] private LocalizedText housingLabel;
        [SerializeField] private Text[] levelLabels = new Text[VillageService.BuildingCount];
        [SerializeField] private Button[] upgradeButtons = new Button[VillageService.BuildingCount];
        [SerializeField] private Text[] costLabels = new Text[VillageService.BuildingCount];
        [SerializeField] private GameObject[] maxLabels = new GameObject[VillageService.BuildingCount];
        [SerializeField] private LocalizedText[] cappedLabels = new LocalizedText[VillageService.BuildingCount];

        private float nextRefresh;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                upgradeButtons[i].onClick.AddListener(() => Upgrade(building));
            }
        }

        protected override void OnShown()
        {
            if (director != null)
            {
                director.Enter();
            }

            if (hideWhileOpen != null)
            {
                hideWhileOpen.alpha = 0f;
            }

            Refresh();
        }

        protected override void OnHidden()
        {
            if (director != null)
            {
                director.Exit();
            }

            if (hideWhileOpen != null)
            {
                hideWhileOpen.alpha = 1f;
            }
        }

        protected override void OnUpdate()
        {
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.5f;
                Refresh();
            }
        }

        private void Upgrade(VillageBuilding building)
        {
            int before = VillageState.Level;
            if (VillageService.TryUpgrade(building, meta.Wallet))
            {
                UiTween.Punch(upgradeButtons[(int)building].transform, 0.3f, 0.3f);
                if (VillageState.Level > before)
                {
                    meta.Wallet.AddCoins(VillageState.LevelReward(VillageState.Level), "village_level");
                }
            }

            Refresh();
        }

        private void Refresh()
        {
            populationLabel.SetKey(LocKeys.VillagePopulation, VillageState.Population);
            levelLabel.SetKey(LocKeys.VillageLevel, VillageState.Level);
            xpFill.fillAmount = VillageState.Level >= VillageState.MaxVillageLevel ? 1f : VillageState.Xp / (float)VillageState.XpForNextLevel;
            int happiness = VillageState.Happiness;
            happinessLabel.SetKey(LocKeys.VillageHappiness, happiness);
            happinessFill.fillAmount = happiness / 100f;
            happinessFill.color = Color.Lerp(new Color(1f, 0.45f, 0.4f), new Color(0.5f, 0.9f, 0.6f), happiness / 100f);
            foodLabel.SetKey(LocKeys.VillageFood, Mathf.FloorToInt(VillageState.Food));
            housingLabel.SetKey(LocKeys.VillageHousing, VillageState.Population, VillageState.Housing);

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                int level = VillageService.Level(building);
                int cost = VillageService.NextCost(building);
                bool capped = VillageService.IsCapped(building);
                levelLabels[i].SetText("{0}/{1}", level, VillageService.MaxLevel);
                upgradeButtons[i].gameObject.SetActive(cost >= 0 && !capped);
                upgradeButtons[i].interactable = cost >= 0 && meta.Wallet.CanAfford(cost);
                costLabels[i].transform.parent.gameObject.SetActive(cost >= 0 && !capped);
                costLabels[i].SetText("{0}", Mathf.Max(0, cost));
                maxLabels[i].SetActive(cost < 0);
                cappedLabels[i].gameObject.SetActive(capped);
                if (capped)
                {
                    // Tier n+1 needs village level 2n+1.
                    cappedLabels[i].SetKey(LocKeys.VillageNeedsLevel, 2 * level + 1);
                }
            }
        }
    }
}
