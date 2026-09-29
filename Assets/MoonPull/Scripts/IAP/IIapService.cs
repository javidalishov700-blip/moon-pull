using System;

namespace MoonPull.IAP
{
    public enum PurchaseOutcome
    {
        Success,
        Cancelled,
        Failed,
        NotInitialized
    }

    public readonly struct PurchaseInfo
    {
        public readonly string ProductId;
        public readonly decimal Price;
        public readonly string Currency;
        public readonly string TransactionId;

        public PurchaseInfo(string productId, decimal price, string currency, string transactionId)
        {
            ProductId = productId;
            Price = price;
            Currency = currency;
            TransactionId = transactionId;
        }
    }

    public interface IIapService
    {
        bool IsInitialized { get; }

        /// <summary>True while the store dialog is open. The app backgrounds then, so App Open ads must not fire.</summary>
        bool PurchaseInProgress { get; }

        event Action<PurchaseInfo> PurchaseCompleted;

        void Initialize(Action<bool> onComplete);

        /// <summary>Store-formatted price (e.g. "₺49,99"), or empty if unknown.</summary>
        string LocalizedPrice(string productId);

        void Purchase(string productId, Action<PurchaseOutcome> onComplete);

        /// <summary>Required on iOS. On Google Play purchases restore automatically during initialization.</summary>
        void RestorePurchases(Action<bool> onComplete);
    }

    /// <summary>Grants purchased content. Returns true once the content is safely persisted.</summary>
    public delegate bool PurchaseFulfiller(string productId, string transactionId);
}
