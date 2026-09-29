using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class BossChestPopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private RectTransform chest;
        [SerializeField] private Text pendingLabel;
        [SerializeField] private LocalizedText keysLabel;
        [SerializeField] private LocalizedText resultLabel;
        [SerializeField] private Button openWithKeyButton;
        [SerializeField] private RewardedButton openWithAdButton;

        protected override void Awake()
        {
            base.Awake();
            openWithKeyButton.onClick.AddListener(() => Opened(meta.BossChests.OpenWithKey()));
            openWithAdButton.Rewarded += () => Opened(meta.BossChests.OpenWithAd());
        }

        protected override void OnShown()
        {
            resultLabel.gameObject.SetActive(false);
            Refresh();
        }

        private void Refresh()
        {
            BossChestService chests = meta.BossChests;
            pendingLabel.SetText("x{0}", chests.Pending);
            keysLabel.SetKey(LocKeys.ChestKeys, meta.Wallet.Keys);
            openWithKeyButton.gameObject.SetActive(chests.Pending > 0);
            openWithKeyButton.interactable = chests.CanOpenWithKey;
            // The ad path only exists for players without a key, per the design.
            openWithAdButton.gameObject.SetActive(chests.Pending > 0 && !chests.CanOpenWithKey);
        }

        private void Opened(int coins)
        {
            if (coins <= 0)
            {
                return;
            }

            UiTween.Punch(chest, 0.35f, 0.4f);
            resultLabel.gameObject.SetActive(true);
            resultLabel.SetKey(LocKeys.ChestGot, coins);
            coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, chest.position), coins);
            Refresh();
        }
    }
}
