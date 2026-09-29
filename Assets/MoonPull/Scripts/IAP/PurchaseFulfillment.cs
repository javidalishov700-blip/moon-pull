using System.Collections.Generic;
using System;
using MoonPull.Config;
using MoonPull.Meta;
using MoonPull.Save;

namespace MoonPull.IAP
{
    /// <summary>
    /// Applies purchased content. Idempotent per transaction ID, because stores legitimately redeliver purchases
    /// (restore, crash before acknowledge, reinstall).
    /// </summary>
    public sealed class PurchaseFulfillment
    {
        private const int MaxRememberedTransactions = 64;

        private readonly IapConfig config;
        private readonly ISaveService save;
        private readonly MetaGame meta;
        private readonly Action<bool> setAdsRemoved;

        public PurchaseFulfillment(IapConfig config, ISaveService save, MetaGame meta, Action<bool> setAdsRemoved)
        {
            this.config = config;
            this.save = save;
            this.meta = meta;
            this.setAdsRemoved = setAdsRemoved;
        }

        public bool Fulfill(string productId, string transactionId)
        {
            List<string> processed = save.Data.ProcessedTransactions;
            if (!string.IsNullOrEmpty(transactionId) && processed.Contains(transactionId))
            {
                return true;
            }

            if (!config.TryGet(productId, out IapConfig.Product product))
            {
                return false;
            }

            switch (product.Kind)
            {
                case IapProductKind.RemoveAds:
                    GrantRemoveAds();
                    break;
                case IapProductKind.StarterPack:
                    GrantRemoveAds();
                    if (!save.Data.StarterPackPurchased)
                    {
                        save.Data.StarterPackPurchased = true;
                        meta.Wallet.AddCoins(product.Coins, "iap_starter");
                        meta.Boats.Grant(meta.Boats.Catalog.Find(config.StarterPackBoatId));
                    }

                    break;
                case IapProductKind.CoinPack:
                    meta.Wallet.AddCoins(product.Coins, "iap_coins");
                    break;
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                processed.Add(transactionId);
                if (processed.Count > MaxRememberedTransactions)
                {
                    processed.RemoveAt(0);
                }
            }

            save.SaveNow();
            return true;
        }

        private void GrantRemoveAds()
        {
            save.Data.RemoveAds = true;
            setAdsRemoved(true);
        }
    }
}
