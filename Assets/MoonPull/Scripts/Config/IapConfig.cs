using System;
using UnityEngine;

namespace MoonPull.Config
{
    public enum IapProductKind
    {
        RemoveAds,
        StarterPack,
        CoinPack
    }

    [CreateAssetMenu(fileName = "IapConfig", menuName = "MoonPull/Config/IAP")]
    public sealed class IapConfig : ScriptableObject
    {
        [Serializable]
        public struct Product
        {
            [Tooltip("Must match Google Play Console and App Store Connect exactly.")]
            public string Id;
            public IapProductKind Kind;
            [Tooltip("Coins granted (coin packs and starter pack).")]
            [Min(0)] public int Coins;
            [Tooltip("Starter pack: shown only while the intro discount window is open.")]
            public bool IsIntroOffer;
        }

        [SerializeField] private Product[] products =
        {
            new Product { Id = "moonpull.remove_ads", Kind = IapProductKind.RemoveAds },
            new Product { Id = "moonpull.starter_pack_intro", Kind = IapProductKind.StarterPack, Coins = 2500, IsIntroOffer = true },
            new Product { Id = "moonpull.starter_pack", Kind = IapProductKind.StarterPack, Coins = 2500 },
            new Product { Id = "moonpull.coins_small", Kind = IapProductKind.CoinPack, Coins = 1200 },
            new Product { Id = "moonpull.coins_medium", Kind = IapProductKind.CoinPack, Coins = 3500 },
            new Product { Id = "moonpull.coins_large", Kind = IapProductKind.CoinPack, Coins = 9000 }
        };

        [Tooltip("Rare boat included in the starter pack.")]
        [SerializeField] private string starterPackBoatId = "boat_moonrunner";
        [Tooltip("Hours after first launch during which the discounted starter pack is offered (Remote Config can override).")]
        [SerializeField, Min(0f)] private float starterPackIntroHours = 48f;
        [SerializeField] private string unityServicesEnvironment = "production";

        public Product[] Products => products;
        public string StarterPackBoatId => starterPackBoatId;
        public float StarterPackIntroHours => starterPackIntroHours;
        public string UnityServicesEnvironment => unityServicesEnvironment;

        public bool TryGet(string productId, out Product product)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i].Id == productId)
                {
                    product = products[i];
                    return true;
                }
            }

            product = default;
            return false;
        }

        public string FirstIdOf(IapProductKind kind, bool introOffer)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i].Kind == kind && products[i].IsIntroOffer == introOffer)
                {
                    return products[i].Id;
                }
            }

            return string.Empty;
        }
    }
}
