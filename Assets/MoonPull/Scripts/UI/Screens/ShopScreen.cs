using System.Collections.Generic;
using MoonPull.Config;
using MoonPull.Core;
using MoonPull.IAP;
using MoonPull.Localization;
using MoonPull.Meta;
using MoonPull.Save;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    public sealed class ShopScreen : UIScreen
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private IapConfig iapConfig;
        [SerializeField] private Ads.AdsCoordinator ads;
        [SerializeField] private Toast toast;

        [Header("Tabs")]
        [SerializeField] private GameObject boatsTab;
        [SerializeField] private GameObject storeTab;
        [SerializeField] private Button boatsTabButton;
        [SerializeField] private Button storeTabButton;
        [SerializeField] private Button backButton;

        [Header("Boats")]
        [SerializeField] private BoatCard boatCardPrefab;
        [SerializeField] private Transform boatList;

        [Header("Store")]
        [SerializeField] private IapProductCard removeAdsCard;
        [SerializeField] private IapProductCard starterCard;
        [SerializeField] private LocalizedText starterDescription;
        [SerializeField] private LocalizedText starterTimer;
        [SerializeField] private IapProductCard[] coinPackCards = new IapProductCard[3];
        [SerializeField] private Button restoreButton;

        private readonly List<BoatCard> cards = new List<BoatCard>(20);
        private float nextTimerRefresh;

        private void Awake()
        {
            boatsTabButton.onClick.AddListener(() => SetTab(true));
            storeTabButton.onClick.AddListener(() => SetTab(false));
            backButton.onClick.AddListener(GameEvents.RaiseMenuRequested);
            restoreButton.onClick.AddListener(Restore);
#if !UNITY_IOS
            // Google Play restores automatically; Apple requires an explicit button.
            restoreButton.gameObject.SetActive(false);
#endif
            removeAdsCard.PurchaseFinished += OnPurchaseFinished;
            starterCard.PurchaseFinished += OnPurchaseFinished;
            for (int i = 0; i < coinPackCards.Length; i++)
            {
                coinPackCards[i].PurchaseFinished += OnPurchaseFinished;
            }
        }

        protected override void OnShown()
        {
            if (cards.Count == 0)
            {
                BuildBoatCards();
            }

            SetTab(true);
            RefreshBoats();
            RefreshStore();
        }

        protected override void OnUpdate()
        {
            if (Time.unscaledTime < nextTimerRefresh || !starterTimer.gameObject.activeInHierarchy)
            {
                return;
            }

            nextTimerRefresh = Time.unscaledTime + 1f;
            SaveData data = Services.Get<ISaveService>().Data;
            System.TimeSpan remaining = StarterPackOffer.IntroRemaining(data, Services.Get<IClock>().UtcNow, meta.StarterPackIntroHours);
            if (remaining <= System.TimeSpan.Zero)
            {
                RefreshStore();
                return;
            }

            starterTimer.SetKey(LocKeys.ShopOfferEnds, UiText.Duration(remaining));
        }

        private void SetTab(bool boats)
        {
            boatsTab.SetActive(boats);
            storeTab.SetActive(!boats);
        }

        private void BuildBoatCards()
        {
            BoatCatalog catalog = meta.Boats.Catalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                BoatCard card = Instantiate(boatCardPrefab, boatList, false);
                card.Bind(catalog[i], meta, ads);
                card.Changed += RefreshBoats;
                card.TrialGranted += StartTrial;
                cards.Add(card);
            }
        }

        private void RefreshBoats()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Refresh();
            }
        }

        private void RefreshStore()
        {
            SaveData data = Services.Get<ISaveService>().Data;
            removeAdsCard.gameObject.SetActive(!data.RemoveAds);
            removeAdsCard.Bind(iapConfig.FirstIdOf(IapProductKind.RemoveAds, false));

            bool starterAvailable = StarterPackOffer.IsAvailable(data);
            starterCard.gameObject.SetActive(starterAvailable);
            if (starterAvailable)
            {
                bool intro = StarterPackOffer.IsIntroActive(data, Services.Get<IClock>().UtcNow, meta.StarterPackIntroHours);
                string productId = iapConfig.FirstIdOf(IapProductKind.StarterPack, intro);
                starterCard.Bind(productId);
                starterTimer.gameObject.SetActive(intro);
                if (iapConfig.TryGet(productId, out IapConfig.Product product))
                {
                    starterDescription.SetKey(LocKeys.ShopStarterDesc, product.Coins);
                }
            }

            int packIndex = 0;
            IapConfig.Product[] products = iapConfig.Products;
            for (int i = 0; i < products.Length && packIndex < coinPackCards.Length; i++)
            {
                if (products[i].Kind == IapProductKind.CoinPack)
                {
                    coinPackCards[packIndex++].Bind(products[i].Id);
                }
            }
        }

        private void StartTrial(BoatDefinition boat)
        {
            meta.Boats.StartTrial(boat);
            meta.PlayNext();
        }

        private void OnPurchaseFinished(string productId, PurchaseOutcome outcome)
        {
            if (outcome == PurchaseOutcome.Success)
            {
                toast.Show(LocKeys.ShopPurchaseOk);
            }
            else if (outcome != PurchaseOutcome.Cancelled)
            {
                toast.Show(LocKeys.ShopPurchaseFailed);
            }

            RefreshStore();
            RefreshBoats();
        }

        private void Restore()
        {
            if (Services.TryGet(out IIapService iap))
            {
                iap.RestorePurchases(success =>
                {
                    toast.Show(success ? LocKeys.ShopRestored : LocKeys.ShopPurchaseFailed);
                    RefreshStore();
                });
            }
        }
    }
}
