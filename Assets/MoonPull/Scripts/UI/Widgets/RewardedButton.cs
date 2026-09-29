using System;
using MoonPull.Ads;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>
    /// Opt-in rewarded ad button. Greyed out and non-interactable while no ad is loaded, so the player is never
    /// promised a reward that cannot be delivered.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RewardedButton : MonoBehaviour
    {
        [SerializeField] private AdsCoordinator coordinator;
        [SerializeField] private AdPlacement placement;
        [SerializeField] private CanvasGroup visual;
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private bool pulse;

        private Button button;

        /// <summary>Raised only when the ad was watched to completion.</summary>
        public event Action Rewarded;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            coordinator.RewardedAvailabilityChanged += Refresh;
            Refresh(coordinator.IsRewardedReady);
            if (pulse)
            {
                UiTween.PulseLoop(transform);
            }
        }

        private void OnDisable()
        {
            coordinator.RewardedAvailabilityChanged -= Refresh;
            UiTween.Kill(transform);
            transform.localScale = Vector3.one;
        }

        private void Refresh(bool available)
        {
            // Availability events can be stale; always trust the live state.
            bool ready = coordinator.IsRewardedReady;
            button.interactable = ready;
            if (visual != null)
            {
                visual.alpha = ready ? 1f : disabledAlpha;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(!ready);
            }
        }

        private void OnClick()
        {
            button.interactable = false;
            coordinator.ShowRewarded(placement, () => Rewarded?.Invoke());
            Refresh(coordinator.IsRewardedReady);
        }
    }
}
