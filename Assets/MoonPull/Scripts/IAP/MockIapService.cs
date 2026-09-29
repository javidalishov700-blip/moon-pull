using System;
using System.Collections.Generic;
using MoonPull.Config;

namespace MoonPull.IAP
{
    /// <summary>Used when Unity IAP is not installed. Purchases succeed instantly so shop flows are testable.</summary>
    public sealed class MockIapService : IIapService
    {
        private readonly IapConfig config;
        private readonly PurchaseFulfiller fulfill;
        private readonly HashSet<string> known = new HashSet<string>();
        private int transactionCounter;

        public MockIapService(IapConfig config, PurchaseFulfiller fulfill)
        {
            this.config = config;
            this.fulfill = fulfill;
        }

        public event Action<PurchaseInfo> PurchaseCompleted;

        public bool IsInitialized { get; private set; }
        public bool PurchaseInProgress => false;

        public void Initialize(Action<bool> onComplete)
        {
            foreach (IapConfig.Product product in config.Products)
            {
                known.Add(product.Id);
            }

            IsInitialized = true;
            onComplete?.Invoke(true);
        }

        public string LocalizedPrice(string productId) => known.Contains(productId) ? "$0.99" : string.Empty;

        public void Purchase(string productId, Action<PurchaseOutcome> onComplete)
        {
            if (!known.Contains(productId))
            {
                onComplete?.Invoke(PurchaseOutcome.Failed);
                return;
            }

            string transaction = $"mock_{++transactionCounter}_{DateTime.UtcNow.Ticks}";
            bool granted = fulfill(productId, transaction);
            if (granted)
            {
                PurchaseCompleted?.Invoke(new PurchaseInfo(productId, 0.99m, "USD", transaction));
            }

            onComplete?.Invoke(granted ? PurchaseOutcome.Success : PurchaseOutcome.Failed);
        }

        public void RestorePurchases(Action<bool> onComplete) => onComplete?.Invoke(true);
    }
}
