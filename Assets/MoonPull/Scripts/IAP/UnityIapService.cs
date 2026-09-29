#if MOONPULL_IAP
using System;
using MoonPull.Config;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace MoonPull.IAP
{
    public sealed class UnityIapService : IIapService, IDetailedStoreListener
    {
        private readonly IapConfig config;
        private readonly PurchaseFulfiller fulfill;
        private readonly ReceiptValidator validator = new ReceiptValidator();

        private IStoreController controller;
        private IExtensionProvider extensions;
        private Action<bool> initCallback;
        private Action<PurchaseOutcome> purchaseCallback;

        public UnityIapService(IapConfig config, PurchaseFulfiller fulfill)
        {
            this.config = config;
            this.fulfill = fulfill;
        }

        public event Action<PurchaseInfo> PurchaseCompleted;

        public bool IsInitialized => controller != null;
        public bool PurchaseInProgress { get; private set; }

        public async void Initialize(Action<bool> onComplete)
        {
            initCallback = onComplete;
            try
            {
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(config.UnityServicesEnvironment));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Unity Services init failed, continuing: {e.Message}");
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            IapConfig.Product[] products = config.Products;
            for (int i = 0; i < products.Length; i++)
            {
                ProductType type = products[i].Kind == IapProductKind.CoinPack ? ProductType.Consumable : ProductType.NonConsumable;
                builder.AddProduct(products[i].Id, type);
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public string LocalizedPrice(string productId)
        {
            Product product = controller?.products.WithID(productId);
            return product != null && product.availableToPurchase ? product.metadata.localizedPriceString : string.Empty;
        }

        public void Purchase(string productId, Action<PurchaseOutcome> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(PurchaseOutcome.NotInitialized);
                return;
            }

            if (PurchaseInProgress)
            {
                return;
            }

            Product product = controller.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(PurchaseOutcome.Failed);
                return;
            }

            PurchaseInProgress = true;
            purchaseCallback = onComplete;
            controller.InitiatePurchase(product);
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false);
                return;
            }

#if UNITY_IOS
            extensions.GetExtension<IAppleExtensions>().RestoreTransactions((success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[IAP] Restore failed: {error}");
                }

                onComplete?.Invoke(success);
            });
#else
            onComplete?.Invoke(true);
#endif
        }

        public void OnInitialized(IStoreController storeController, IExtensionProvider extensionProvider)
        {
            controller = storeController;
            extensions = extensionProvider;
            initCallback?.Invoke(true);
            initCallback = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, string.Empty);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning($"[IAP] Init failed: {error} {message}");
            initCallback?.Invoke(false);
            initCallback = null;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args.purchasedProduct;
            string productId = product.definition.id;

            if (!validator.IsValid(product.receipt, out string error))
            {
                Debug.LogWarning($"[IAP] Invalid receipt for {productId}: {error}");
                Finish(PurchaseOutcome.Failed);
                return PurchaseProcessingResult.Complete;
            }

            if (!fulfill(productId, product.transactionID))
            {
                // Not persisted: leave pending so the store redelivers it on next launch.
                Finish(PurchaseOutcome.Failed);
                return PurchaseProcessingResult.Pending;
            }

            PurchaseCompleted?.Invoke(new PurchaseInfo(productId, product.metadata.localizedPrice, product.metadata.isoCurrencyCode, product.transactionID));
            Finish(PurchaseOutcome.Success);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure) => OnPurchaseFailed(product, failure.reason);

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.Log($"[IAP] Purchase failed: {product?.definition.id} {reason}");
            Finish(reason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);
        }

        private void Finish(PurchaseOutcome outcome)
        {
            PurchaseInProgress = false;
            Action<PurchaseOutcome> callback = purchaseCallback;
            purchaseCallback = null;
            callback?.Invoke(outcome);
        }
    }
}
#endif
