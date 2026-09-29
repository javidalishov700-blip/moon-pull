using MoonPull.Ads;
using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Level complete. Coins are already credited; the screen animates them in. The x3 offer sits well above
    /// Continue so the two can't be confused, and any interstitial plays only after Continue is tapped.
    /// </summary>
    public sealed class WinScreen : UIScreen
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MetaGame meta;
        [SerializeField] private AdsCoordinator ads;
        [SerializeField] private CoinFlyEffect coinFly;

        [SerializeField] private StarRatingView stars;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text rewardLabel;
        [SerializeField] private RectTransform rewardAnchor;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private GameObject bossChestNote;

        [SerializeField] private GameObject tripleGroup;
        [SerializeField] private RewardedButton tripleButton;
        [SerializeField] private LocalizedText tripleDescription;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button homeButton;

        private bool leaving;

        private void Awake()
        {
            tripleButton.Rewarded += OnTripleRewarded;
            continueButton.onClick.AddListener(() => Leave(meta.PlayNext));
            homeButton.onClick.AddListener(() => Leave(GameEvents.RaiseMenuRequested));
        }

        protected override void OnShown()
        {
            leaving = false;
            LevelResult result = gameManager.LastResult;
            LevelReward reward = meta.LastReward;

            stars.Animate(result.Stars);
            UiTween.Count(scoreLabel, 0f, result.Score, 0.8f, v => scoreLabel.SetText("{0:0}", v));
            rewardLabel.SetText("+{0}", reward.Total);
            newBestBadge.SetActive(meta.LastResultWasNewBest);
            bossChestNote.SetActive(result.BossDefeated);

            bool canMultiply = meta.CanMultiplyLastReward;
            tripleGroup.SetActive(canMultiply);
            if (canMultiply)
            {
                tripleDescription.SetKey(LocKeys.WinTripleDesc, reward.Total, reward.Total * meta.RewardedLevelMultiplier);
            }

            coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, rewardAnchor.position), reward.Total);
        }

        private void OnTripleRewarded()
        {
            int bonus = meta.ApplyRewardMultiplier();
            tripleGroup.SetActive(false);
            rewardLabel.SetText("+{0}", meta.LastReward.Total + bonus);
            UiTween.Punch(rewardLabel.transform, 0.3f, 0.3f);
            coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, rewardAnchor.position), bonus);
        }

        private void Leave(System.Action next)
        {
            if (leaving)
            {
                return;
            }

            leaving = true;
            ads.ContinueAfterLevelComplete(gameManager.LastResult.LevelIndex, next);
        }
    }
}
