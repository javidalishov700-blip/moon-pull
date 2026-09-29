using System;
using MoonPull.Config;
using MoonPull.Localization;
using MoonPull.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    public sealed class BoatCard : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private LocalizedText nameLabel;
        [SerializeField] private LocalizedText rarityLabel;
        [SerializeField] private TMP_Text perkLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private GameObject iapOnlyLabel;
        [SerializeField] private RewardedButton tryButton;

        private BoatDefinition boat;
        private MetaGame meta;

        /// <summary>Raised after the "Try This Boat" ad paid out.</summary>
        public event Action<BoatDefinition> TrialGranted;

        public event Action Changed;

        private void Awake()
        {
            buyButton.onClick.AddListener(() =>
            {
                if (meta.Boats.TryPurchase(boat))
                {
                    meta.Boats.Select(boat);
                    Changed?.Invoke();
                }
            });
            selectButton.onClick.AddListener(() =>
            {
                meta.Boats.Select(boat);
                Changed?.Invoke();
            });
            tryButton.Rewarded += () => TrialGranted?.Invoke(boat);
        }

        public void Bind(BoatDefinition definition, MetaGame metaGame, Ads.AdsCoordinator ads)
        {
            boat = definition;
            meta = metaGame;
            tryButton.SetCoordinator(ads);
            icon.sprite = definition.Icon;
            nameLabel.SetKey(definition.NameKey);
            rarityLabel.SetKey(UiText.RarityKey(definition.Rarity));
            Refresh();
        }

        public void Refresh()
        {
            bool owned = meta.Boats.IsOwned(boat);
            bool selected = owned && meta.Boats.Selected == boat;
            perkLabel.text = UiText.Perk(boat);

            selectedMark.SetActive(selected);
            selectButton.gameObject.SetActive(owned && !selected);
            buyButton.gameObject.SetActive(!owned && !boat.IapOnly);
            buyButton.interactable = meta.Boats.CanPurchase(boat);
            priceLabel.SetText("{0}", boat.Price);
            iapOnlyLabel.SetActive(!owned && boat.IapOnly);
            tryButton.gameObject.SetActive(!owned);
        }
    }
}
