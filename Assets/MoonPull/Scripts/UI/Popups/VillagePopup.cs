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

        [Header("Tycoon")]
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private Button buildingsTab;
        [SerializeField] private Button islandsTab;
        [SerializeField] private GameObject buildingsPage;
        [SerializeField] private GameObject islandsPage;
        [SerializeField] private Text treasuryLabel;
        [SerializeField] private LocalizedText incomeLabel;
        [SerializeField] private LocalizedText suppliesLabel;
        [SerializeField] private Button collectButton;
        [SerializeField] private Image treasuryFill;
        [SerializeField] private Button[] islandButtons = new Button[TycoonState.IslandCount];
        [SerializeField] private Text[] islandCostLabels = new Text[TycoonState.IslandCount];
        [SerializeField] private GameObject[] islandOwnedLabels = new GameObject[TycoonState.IslandCount];
        [SerializeField] private LocalizedText[] islandLockedLabels = new LocalizedText[TycoonState.IslandCount];

        private bool showIslands;

        private float nextRefresh;

        protected override void Awake()
        {
            base.Awake();
            int treasury = TycoonState.Treasury;
            treasuryLabel.SetText("{0}", treasury);
            if (TycoonState.OutOfSupplies)
            {
                incomeLabel.SetKey(LocKeys.VillageNoSupplies);
                incomeLabel.GetComponent<Text>().color = new Color(1f, 0.5f, 0.45f);
            }
            else
            {
                incomeLabel.SetKey(LocKeys.VillageIncome, Mathf.RoundToInt(TycoonState.IncomePerMinute * 60f));
                incomeLabel.GetComponent<Text>().color = new Color(0.5f, 0.89f, 0.77f);
            }

            suppliesLabel.SetKey(LocKeys.VillageSupplies, Mathf.FloorToInt(TycoonState.Supplies), TycoonState.SupplyCapacity,
                Mathf.Min(VillageState.Population, TycoonState.JobsTotal), TycoonState.JobsTotal);
            treasuryFill.fillAmount = Mathf.Clamp01(treasury / TycoonState.Capacity);
            collectButton.interactable = treasury > 0;

            for (int i = 0; i < islandButtons.Length; i++)
            {
                bool owned = TycoonState.Owns(i);
                bool canBuy = TycoonState.CanBuyNext(i);
                bool previousOwned = i == 0 || TycoonState.Owns(i - 1);
                islandButtons[i].gameObject.SetActive(canBuy);
                islandButtons[i].interactable = canBuy && meta.Wallet.CanAfford(TycoonState.IslandCost(i));
                islandCostLabels[i].transform.parent.gameObject.SetActive(!owned);
                islandCostLabels[i].SetText("{0}", TycoonState.IslandCost(i));
                islandOwnedLabels[i].SetActive(owned);
                islandLockedLabels[i].gameObject.SetActive(!owned && !canBuy);
                if (!owned && !canBuy)
                {
                    if (previousOwned && VillageState.Population < TycoonState.IslandRequiredPeople(i))
                    {
                        islandLockedLabels[i].SetKey(LocKeys.VillageNeedsPeople, TycoonState.IslandRequiredPeople(i));
                    }
                    else if (previousOwned)
                    {
                        islandLockedLabels[i].SetKey(LocKeys.VillageNeedsLevel, TycoonState.IslandRequiredLevel(i));
                    }
                    else
                    {
                        islandLockedLabels[i].SetKey(LocKeys.IslandBuyPrevious);
                    }
                }
            }

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                upgradeButtons[i].onClick.AddListener(() => Upgrade(building));
            }

            for (int i = 0; i < islandButtons.Length; i++)
            {
                int island = i;
                islandButtons[i].onClick.AddListener(() => BuyIsland(island));
            }

            buildingsTab.onClick.AddListener(() => ShowPage(false));
            islandsTab.onClick.AddListener(() => ShowPage(true));
            collectButton.onClick.AddListener(Collect);
            ShowPage(false);
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

        /// <summary>Opened by tapping an island's price pin in the village.</summary>
        public void ShowIslands() => ShowPage(true);

        /// <summary>Tapping a coin pin over a building collects the Treasury.</summary>
        public void CollectFromWorld() => Collect();

        private void ShowPage(bool islands)
        {
            showIslands = islands;
            buildingsPage.SetActive(!islands);
            islandsPage.SetActive(islands);
            Color on = new Color(1f, 0.71f, 0.28f);
            Color off = new Color(0.25f, 0.3f, 0.55f);
            buildingsTab.GetComponent<Image>().color = islands ? off : on;
            islandsTab.GetComponent<Image>().color = islands ? on : off;
            Refresh();
        }

        private void Collect()
        {
            int amount = TycoonState.Collect(meta.Wallet);
            if (amount > 0)
            {
                UiTween.Punch(collectButton.transform, 0.3f, 0.3f);
                if (coinFly != null)
                {
                    coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, collectButton.transform.position), amount);
                }
            }

            Refresh();
        }

        private void BuyIsland(int island)
        {
            int before = VillageState.Level;
            if (TycoonState.TryBuyIsland(island, meta.Wallet))
            {
                UiTween.Punch(islandButtons[island].transform, 0.3f, 0.3f);
                if (VillageState.Level > before)
                {
                    meta.Wallet.AddCoins(VillageState.LevelReward(VillageState.Level), "village_level");
                }
            }

            Refresh();
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

            int treasury = TycoonState.Treasury;
            treasuryLabel.SetText("{0}", treasury);
            if (TycoonState.OutOfSupplies)
            {
                incomeLabel.SetKey(LocKeys.VillageNoSupplies);
                incomeLabel.GetComponent<Text>().color = new Color(1f, 0.5f, 0.45f);
            }
            else
            {
                incomeLabel.SetKey(LocKeys.VillageIncome, Mathf.RoundToInt(TycoonState.IncomePerMinute * 60f));
                incomeLabel.GetComponent<Text>().color = new Color(0.5f, 0.89f, 0.77f);
            }

            suppliesLabel.SetKey(LocKeys.VillageSupplies, Mathf.FloorToInt(TycoonState.Supplies), TycoonState.SupplyCapacity,
                Mathf.Min(VillageState.Population, TycoonState.JobsTotal), TycoonState.JobsTotal);
            treasuryFill.fillAmount = Mathf.Clamp01(treasury / TycoonState.Capacity);
            collectButton.interactable = treasury > 0;

            for (int i = 0; i < islandButtons.Length; i++)
            {
                bool owned = TycoonState.Owns(i);
                bool canBuy = TycoonState.CanBuyNext(i);
                bool previousOwned = i == 0 || TycoonState.Owns(i - 1);
                islandButtons[i].gameObject.SetActive(canBuy);
                islandButtons[i].interactable = canBuy && meta.Wallet.CanAfford(TycoonState.IslandCost(i));
                islandCostLabels[i].transform.parent.gameObject.SetActive(!owned);
                islandCostLabels[i].SetText("{0}", TycoonState.IslandCost(i));
                islandOwnedLabels[i].SetActive(owned);
                islandLockedLabels[i].gameObject.SetActive(!owned && !canBuy);
                if (!owned && !canBuy)
                {
                    if (previousOwned && VillageState.Population < TycoonState.IslandRequiredPeople(i))
                    {
                        islandLockedLabels[i].SetKey(LocKeys.VillageNeedsPeople, TycoonState.IslandRequiredPeople(i));
                    }
                    else if (previousOwned)
                    {
                        islandLockedLabels[i].SetKey(LocKeys.VillageNeedsLevel, TycoonState.IslandRequiredLevel(i));
                    }
                    else
                    {
                        islandLockedLabels[i].SetKey(LocKeys.IslandBuyPrevious);
                    }
                }
            }

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                var building = (VillageBuilding)i;
                int level = VillageService.Level(building);
                int cost = VillageService.NextCost(building);
                bool needsLevel = VillageService.IsCapped(building);
                bool capped = needsLevel || (cost >= 0 && VillageService.NeedsWorkers);
                levelLabels[i].SetText("{0}/{1}", level, VillageService.MaxLevel);
                upgradeButtons[i].gameObject.SetActive(cost >= 0 && !capped);
                upgradeButtons[i].interactable = cost >= 0 && meta.Wallet.CanAfford(cost);
                costLabels[i].transform.parent.gameObject.SetActive(cost >= 0 && !capped);
                costLabels[i].SetText("{0}", Mathf.Max(0, cost));
                maxLabels[i].SetActive(cost < 0);
                cappedLabels[i].gameObject.SetActive(capped);
                if (capped && !needsLevel)
                {
                    cappedLabels[i].SetKey(LocKeys.VillageNeedsPeople, VillageService.WorkersNeeded);
                }
                else if (capped)
                {
                    // Tier n+1 needs village level 2n+1.
                    cappedLabels[i].SetKey(LocKeys.VillageNeedsLevel, 2 * level + 1);
                }
            }
        }
    }
}
