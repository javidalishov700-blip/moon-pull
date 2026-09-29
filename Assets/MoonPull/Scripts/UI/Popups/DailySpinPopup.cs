using MoonPull.Config;
using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Wheel with segments laid out clockwise from the top pointer. The result is decided (and granted) before the
    /// animation, so closing the app mid-spin never loses a reward.
    /// </summary>
    public sealed class DailySpinPopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private RectTransform wheel;
        [SerializeField] private Text[] segmentLabels = new Text[8];
        [SerializeField] private Image[] segmentImages = new Image[8];
        [SerializeField] private Button freeSpinButton;
        [SerializeField] private RewardedButton extraSpinButton;
        [SerializeField] private LocalizedText extraLeftLabel;
        [SerializeField] private LocalizedText statusLabel;
        [SerializeField, Min(0.5f)] private float spinSeconds = 3.2f;
        [SerializeField, Min(1)] private int fullTurns = 5;

        private bool spinning;

        protected override void Awake()
        {
            base.Awake();
            freeSpinButton.onClick.AddListener(() => Spin(meta.Spin.SpinFree()));
            extraSpinButton.Rewarded += () => Spin(meta.Spin.SpinExtra());
        }

        protected override void OnShown()
        {
            SpinSegment[] segments = meta.Spin.Segments;
            for (int i = 0; i < segmentLabels.Length && i < segments.Length; i++)
            {
                segmentLabels[i].text = UiText.Reward(segments[i].Reward);
                segmentImages[i].color = segments[i].Color;
            }

            wheel.localRotation = Quaternion.identity;
            statusLabel.SetKey(LocKeys.SpinTitle);
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool free = meta.Spin.FreeSpinAvailable;
            freeSpinButton.gameObject.SetActive(free);
            freeSpinButton.interactable = !spinning;

            bool extra = meta.Spin.CanExtraSpin;
            extraSpinButton.gameObject.SetActive(!free && extra && !spinning);
            extraLeftLabel.gameObject.SetActive(!free && extra);
            if (!free && extra)
            {
                extraLeftLabel.SetKey(LocKeys.SpinLeft, meta.Spin.ExtraSpinsRemaining);
            }
            else if (!free && !spinning)
            {
                statusLabel.SetKey(LocKeys.SpinTomorrow);
            }
        }

        private void Spin(int index)
        {
            if (index < 0 || spinning)
            {
                return;
            }

            spinning = true;
            RefreshButtons();
            int count = meta.Spin.Segments.Length;
            float segmentAngle = 360f / count;
            wheel.localRotation = Quaternion.identity;
            UiTween.RotateZ(wheel, fullTurns * 360f + index * segmentAngle, spinSeconds, () => OnLanded(index));
        }

        private void OnLanded(int index)
        {
            spinning = false;
            RewardEntry reward = meta.Spin.Segments[index].Reward;
            statusLabel.SetKey(LocKeys.SpinWon, UiText.Reward(reward));
            if (reward.Type == RewardType.Coins)
            {
                coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, wheel.position), reward.Amount);
            }

            RefreshButtons();
        }
    }
}
