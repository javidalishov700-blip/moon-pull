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

        [SerializeField] private RectTransform sheet;
        [SerializeField] private Button exploreButton;
        [SerializeField] private float sheetOpenY = -400f;
        [SerializeField] private float sheetHiddenY = -1210f;

        [Header("Tycoon")]
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private Button buildingsTab;
        [SerializeField] private Button islandsTab;
        [SerializeField] private GameObject buildingsPage;
        [SerializeField] private GameObject islandsPage;
        [SerializeField] private Button boatTab;
        [SerializeField] private GameObject boatPage;
        [SerializeField] private Text[] boatLevelLabels = new Text[BoatUpgrades.PartCount];
        [SerializeField] private Button[] boatButtons = new Button[BoatUpgrades.PartCount];
        [SerializeField] private Text[] boatCostLabels = new Text[BoatUpgrades.PartCount];
        [SerializeField] private GameObject[] boatMaxLabels = new GameObject[BoatUpgrades.PartCount];
        [SerializeField] private Text treasuryLabel;
        [SerializeField] private LocalizedText incomeLabel;
        [SerializeField] private LocalizedText suppliesLabel;
        [SerializeField] private Button collectButton;
        [SerializeField] private Image treasuryFill;
        [SerializeField] private Button[] islandButtons = new Button[TycoonState.IslandCount];
        [SerializeField] private Text[] islandCostLabels = new Text[TycoonState.IslandCount];
        [SerializeField] private GameObject[] islandOwnedLabels = new GameObject[TycoonState.IslandCount];
        [SerializeField] private LocalizedText[] islandLockedLabels = new LocalizedText[TycoonState.IslandCount];

        private int page;
        private bool exploring;

        private float nextRefresh;

        protected override void Awake()
        {
            base.Awake();
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

            buildingsTab.onClick.AddListener(() => ShowPage(0));
            islandsTab.onClick.AddListener(() => ShowPage(1));
            boatTab.onClick.AddListener(() => ShowPage(2));
            for (int i = 0; i < boatButtons.Length; i++)
            {
                var part = (BoatPart)i;
                boatButtons[i].onClick.AddListener(() => UpgradeBoat(part));
            }
            collectButton.onClick.AddListener(Collect);
            if (exploreButton != null)
            {
                exploreButton.onClick.AddListener(() => SetExploring(!exploring));
            }
            ShowPage(0);
        }

        protected override void OnShown()
        {
            if (director != null)
            {
                director.Enter();
            }

            if (hideWhileOpen != null)
            {
                // Hidden and untouchable: taps on the village must never reach the menu's buttons underneath.
                hideWhileOpen.alpha = 0f;
                hideWhileOpen.interactable = false;
                hideWhileOpen.blocksRaycasts = false;
            }

            SetExploring(false);

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
                hideWhileOpen.interactable = true;
                hideWhileOpen.blocksRaycasts = true;
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

        /// <summary>Slides the sheet down so the whole village can be explored (drag to pan, pinch to zoom).</summary>
        private void SetExploring(bool value)
        {
            exploring = value;
            if (exploreButton != null)
            {
                exploreButton.GetComponentInChildren<LocalizedText>().SetKey(value ? LocKeys.VillageManage : LocKeys.VillageExplore);
            }

            if (sheet != null)
            {
                sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, value ? sheetHiddenY : sheetOpenY);
            }

            if (director != null)
            {
                director.SetExploring(value);
            }
        }

        /// <summary>Opened by tapping an island's price pin in the village.</summary>
        public void ShowIslands()
        {
            SetExploring(false);
            ShowPage(1);
        }

        /// <summary>Tapping a coin pin over a building collects the Treasury.</summary>
        public void CollectFromWorld() => Collect();

        private void ShowPage(int index)
        {
            page = index;
            buildingsPage.SetActive(index == 0);
            islandsPage.SetActive(index == 1);
            boatPage.SetActive(index == 2);
            Color on = new Color(1f, 0.71f, 0.28f);
            Color off = new Color(0.25f, 0.3f, 0.55f);
            buildingsTab.GetComponent<Image>().color = index == 0 ? on : off;
            islandsTab.GetComponent<Image>().color = index == 1 ? on : off;
            boatTab.GetComponent<Image>().color = index == 2 ? on : off;
            Refresh();
        }

        private void UpgradeBoat(BoatPart part)
        {
            if (BoatUpgrades.TryUpgrade(part, meta.Wallet))
            {
                UiTween.Punch(boatButtons[(int)part].transform, 0.3f, 0.3f);
            }

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
            if (meta == null || !meta.IsInitialized)
            {
                return; // the wallet is not loaded yet (Awake runs before boot)
            }

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

            for (int i = 0; i < boatButtons.Length; i++)
            {
                var part = (BoatPart)i;
                int cost = BoatUpgrades.NextCost(part);
                boatLevelLabels[i].SetText("{0}/{1}", BoatUpgrades.Level(part), BoatUpgrades.MaxLevel);
                boatButtons[i].gameObject.SetActive(cost >= 0);
                boatButtons[i].interactable = cost >= 0 && meta.Wallet.CanAfford(cost);
                boatCostLabels[i].transform.parent.gameObject.SetActive(cost >= 0);
                boatCostLabels[i].SetText("{0}", Mathf.Max(0, cost));
                boatMaxLabels[i].SetActive(cost < 0);
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
