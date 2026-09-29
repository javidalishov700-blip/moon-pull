using MoonPull.Config;
using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class LoginStreakPopup : UIPopup
    {
        [System.Serializable]
        private struct DayCell
        {
            public LocalizedText DayLabel;
            public Text RewardLabel;
            public GameObject ClaimedMark;
            public GameObject TodayHighlight;
        }

        [SerializeField] private MetaGame meta;
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private DayCell[] days = new DayCell[7];
        [SerializeField] private Button claimButton;

        protected override void Awake()
        {
            base.Awake();
            claimButton.onClick.AddListener(Claim);
        }

        protected override void OnShown()
        {
            int today = meta.Streak.CurrentDay;
            bool canClaim = meta.Streak.CanClaim;
            for (int i = 0; i < days.Length; i++)
            {
                int day = i + 1;
                days[i].DayLabel.SetKey(LocKeys.StreakDay, day);
                days[i].RewardLabel.text = UiText.Reward(meta.Streak.RewardForDay(day));
                days[i].ClaimedMark.SetActive(day < today || (day == today && !canClaim));
                days[i].TodayHighlight.SetActive(day == today);
            }

            claimButton.interactable = canClaim;
        }

        private void Claim()
        {
            int day = meta.Streak.CurrentDay;
            RewardEntry reward = meta.Streak.RewardForDay(day);
            if (!meta.Streak.Claim())
            {
                return;
            }

            if (reward.Type == RewardType.Coins)
            {
                RectTransform cell = (RectTransform)days[day - 1].RewardLabel.transform;
                coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, cell.position), reward.Amount);
            }

            Close();
        }
    }
}
