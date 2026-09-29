using MoonPull.Localization;
using MoonPull.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    public sealed class LighthousePopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private LocalizedText regionLabel;
        [SerializeField] private LocalizedText stageLabel;
        [SerializeField] private LocalizedText rateLabel;
        [SerializeField] private LocalizedText lockedLabel;
        [SerializeField] private GameObject completeLabel;
        [SerializeField] private Image[] stagePips = new Image[8];
        [SerializeField] private Color builtColor = Color.white;
        [SerializeField] private Color pendingColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Button buildButton;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button collectButton;
        [SerializeField] private UIScreen idlePopup;

        private int region;

        protected override void Awake()
        {
            base.Awake();
            buildButton.onClick.AddListener(Build);
            previousButton.onClick.AddListener(() => Select(region - 1));
            nextButton.onClick.AddListener(() => Select(region + 1));
            collectButton.onClick.AddListener(() => Popups.Open(idlePopup));
        }

        protected override void OnShown()
        {
            Select(meta.Progress.RegionOf(meta.Progress.NextLevelToPlay()));
        }

        private void Select(int index)
        {
            region = Mathf.Clamp(index, 0, meta.Lighthouses.RegionCount - 1);
            Refresh();
        }

        private void Refresh()
        {
            LighthouseService lighthouses = meta.Lighthouses;
            int stage = lighthouses.StageOf(region);
            int count = lighthouses.StageCount(region);
            bool unlocked = meta.Progress.IsRegionUnlocked(region);
            int cost = lighthouses.NextStageCost(region);

            regionLabel.SetKey(meta.Regions[region].NameKey);
            stageLabel.SetKey(LocKeys.LighthouseStage, stage, count);
            rateLabel.SetKey(LocKeys.IdleRate, Mathf.RoundToInt(lighthouses.IdleCoinsPerHour(meta.Boats.SelectedModifiers.IdleIncomeMultiplier)));
            for (int i = 0; i < stagePips.Length; i++)
            {
                stagePips[i].gameObject.SetActive(i < count);
                stagePips[i].color = i < stage ? builtColor : pendingColor;
            }

            lockedLabel.gameObject.SetActive(!unlocked);
            if (!unlocked)
            {
                lockedLabel.SetKey(LocKeys.LighthouseRegionLocked, meta.Regions[region].StarsToUnlock);
            }

            completeLabel.SetActive(cost < 0);
            buildButton.gameObject.SetActive(unlocked && cost >= 0);
            buildButton.interactable = cost >= 0 && meta.Wallet.CanAfford(cost);
            costLabel.SetText("{0}", Mathf.Max(0, cost));
            previousButton.interactable = region > 0;
            nextButton.interactable = region < lighthouses.RegionCount - 1;
            collectButton.gameObject.SetActive(meta.Idle.Pending > 0);
        }

        private void Build()
        {
            if (meta.BuildLighthouseStage(region))
            {
                UiTween.Punch(stagePips[Mathf.Max(0, meta.Lighthouses.StageOf(region) - 1)].transform, 0.4f, 0.35f);
            }

            Refresh();
        }
    }
}
