using System;
using MoonPull.Core;
using MoonPull.IAP;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>One store product. Price comes from the store (localized currency), never hardcoded.</summary>
    public sealed class IapProductCard : MonoBehaviour
    {
        [SerializeField] private Button buyButton;
        [SerializeField] private Text priceLabel;

        private string productId;

        /// <summary>(productId, outcome) after a purchase attempt.</summary>
        public event Action<string, PurchaseOutcome> PurchaseFinished;

        private void Awake() => buyButton.onClick.AddListener(Buy);

        public void Bind(string id)
        {
            productId = id;
            Refresh();
        }

        public void Refresh()
        {
            string price = Services.TryGet(out IIapService iap) ? iap.LocalizedPrice(productId) : string.Empty;
            priceLabel.text = price;
            buyButton.interactable = !string.IsNullOrEmpty(price);
        }

        private void Buy()
        {
            if (!Services.TryGet(out IIapService iap))
            {
                return;
            }

            buyButton.interactable = false;
            iap.Purchase(productId, outcome =>
            {
                buyButton.interactable = true;
                PurchaseFinished?.Invoke(productId, outcome);
            });
        }
    }
}
