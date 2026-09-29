using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class IdleIncomePopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private RectTransform coinOrigin;
        [SerializeField] private LocalizedText bodyLabel;
        [SerializeField] private LocalizedText rateLabel;
        [SerializeField] private Image fillBar;
        [SerializeField] private GameObject fullLabel;
        [SerializeField] private Button collectButton;
        [SerializeField] private RewardedButton doubleButton;

        protected override void Awake()
        {
            base.Awake();
            collectButton.onClick.AddListener(() => Collect(1));
            doubleButton.Rewarded += () => Collect(meta.IdleRewardedMultiplier);
        }

        protected override void OnShown()
        {
            long pending = meta.Idle.Pending;
            bodyLabel.SetKey(LocKeys.IdleBody, UiText.Number(pending));
            rateLabel.SetKey(LocKeys.IdleRate, Mathf.RoundToInt(meta.Lighthouses.IdleCoinsPerHour(meta.Boats.SelectedModifiers.IdleIncomeMultiplier)));
            float fill = meta.Idle.Fill01;
            fillBar.fillAmount = fill;
            fullLabel.SetActive(fill >= 1f);
            bool hasCoins = pending > 0;
            collectButton.interactable = hasCoins;
            doubleButton.gameObject.SetActive(hasCoins);
        }

        private void Collect(int multiplier)
        {
            long coins = meta.Idle.Collect(multiplier);
            coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, coinOrigin.position), coins);
            Close();
        }
    }
}
