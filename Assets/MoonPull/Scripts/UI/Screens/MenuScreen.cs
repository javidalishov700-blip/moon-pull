using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class MenuScreen : UIScreen
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private PopupManager popups;

        [Header("Labels")]
        [SerializeField] private LocalizedText levelLabel;
        [SerializeField] private LocalizedText regionLabel;
        [SerializeField] private Text starsLabel;
        [SerializeField] private LocalizedText lockedLabel;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button lighthouseButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button spinButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button bossChestButton;

        [Header("Badges")]
        [SerializeField] private Badge missionsBadge;
        [SerializeField] private Badge spinBadge;
        [SerializeField] private Badge chestBadge;

        [Header("Popups")]
        [SerializeField] private UIScreen idlePopup;
        [SerializeField] private UIScreen streakPopup;
        [SerializeField] private UIScreen spinPopup;
        [SerializeField] private UIScreen missionsPopup;
        [SerializeField] private UIScreen settingsPopup;
        [SerializeField] private UIScreen lighthousePopup;
        [SerializeField] private UIScreen bossChestPopup;

        [Tooltip("Auto popups (idle income, daily reward) re-appear only after the app was away this long.")]
        [SerializeField, Min(0f)] private float autoPopupMinAwaySeconds = 300f;
        [SerializeField, Min(0.1f)] private float badgeRefreshSeconds = 0.5f;

        private bool autoPopupsPending = true;
        private int promptedChestCount;
        private float nextBadgeRefresh;

        private void Awake()
        {
            playButton.onClick.AddListener(() => meta.PlayNext());
            shopButton.onClick.AddListener(GameEvents.RaiseShopRequested);
            lighthouseButton.onClick.AddListener(() => popups.Open(lighthousePopup));
            missionsButton.onClick.AddListener(() => popups.Open(missionsPopup));
            spinButton.onClick.AddListener(() => popups.Open(spinPopup));
            settingsButton.onClick.AddListener(() => popups.Open(settingsPopup));
            bossChestButton.onClick.AddListener(() => popups.Open(bossChestPopup));
        }

        private void OnEnable() => GameEvents.AppResumed += OnAppResumed;

        private void OnDisable() => GameEvents.AppResumed -= OnAppResumed;

        protected override void OnShown()
        {
            if (!meta.IsInitialized)
            {
                return;
            }

            RefreshLabels();
            RefreshBadges();
            if (autoPopupsPending)
            {
                autoPopupsPending = false;
                QueueAutoPopups();
            }

            int chests = meta.BossChests.Pending;
            if (chests > promptedChestCount)
            {
                popups.Enqueue(bossChestPopup);
            }

            promptedChestCount = chests;
        }

        protected override void OnUpdate()
        {
            if (Time.unscaledTime < nextBadgeRefresh || !meta.IsInitialized)
            {
                return;
            }

            nextBadgeRefresh = Time.unscaledTime + badgeRefreshSeconds;
            RefreshBadges();
        }

        private void QueueAutoPopups()
        {
            if (meta.Streak.CanClaim)
            {
                popups.Enqueue(streakPopup);
            }

            if (meta.Idle.ShouldShowCollectScreen)
            {
                popups.Enqueue(idlePopup);
            }
        }

        private void OnAppResumed(float secondsAway)
        {
            if (secondsAway < autoPopupMinAwaySeconds || !meta.IsInitialized)
            {
                return;
            }

            if (IsVisible)
            {
                QueueAutoPopups();
            }
            else
            {
                autoPopupsPending = true;
            }
        }

        private void RefreshLabels()
        {
            LevelProgress progress = meta.Progress;
            int level = progress.NextLevelToPlay();
            levelLabel.SetKey(LocKeys.MenuLevel, level + 1);
            regionLabel.SetKey(meta.Regions.ForLevel(level, progress.LevelsPerRegion).NameKey);
            starsLabel.SetText("{0}", progress.TotalStars);

            int nextRegion = progress.RegionOf(progress.HighestUnlockedLevel);
            bool gated = !progress.IsRegionUnlocked(nextRegion);
            lockedLabel.gameObject.SetActive(gated);
            if (gated)
            {
                lockedLabel.SetKey(LocKeys.LighthouseRegionLocked, meta.Regions[nextRegion].StarsToUnlock);
            }
        }

        private void RefreshBadges()
        {
            missionsBadge.Set(meta.Missions.ClaimableCount);
            spinBadge.Set(meta.Spin.FreeSpinAvailable);
            int chests = meta.BossChests.Pending;
            chestBadge.Set(chests);
            bossChestButton.gameObject.SetActive(chests > 0);
        }
    }
}
