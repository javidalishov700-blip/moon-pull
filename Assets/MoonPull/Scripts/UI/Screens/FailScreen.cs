using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Rewind;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>
    /// Shipwreck screen. "Rewind the Tide" is an opt-in rewarded ad, offered once per level; Retry and Home are
    /// always free and never gated behind an ad.
    /// </summary>
    public sealed class FailScreen : UIScreen
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RewindController rewind;
        [SerializeField] private LocalizedText reasonLabel;
        [SerializeField] private GameObject rewindGroup;
        [SerializeField] private RewardedButton rewindButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            rewindButton.Rewarded += GameEvents.RaiseRewindGranted;
            retryButton.onClick.AddListener(GameEvents.RaiseRestartRequested);
            homeButton.onClick.AddListener(GameEvents.RaiseMenuRequested);
        }

        protected override void OnShown()
        {
            reasonLabel.SetKey(UiText.FailReasonKey(gameManager.LastFailReason));
            rewindGroup.SetActive(rewind.CanRewind);
        }
    }
}
