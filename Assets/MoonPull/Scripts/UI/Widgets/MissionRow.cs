using System;
using MoonPull.Config;
using MoonPull.Localization;
using MoonPull.Meta;
using MoonPull.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    public sealed class MissionRow : MonoBehaviour
    {
        [SerializeField] private LocalizedText description;
        [SerializeField] private Image progressFill;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject claimedMark;

        private MissionProgress mission;
        private DailyMissionService service;

        public event Action<MissionRow, int> Claimed;

        private void Awake()
        {
            claimButton.onClick.AddListener(() =>
            {
                MissionDefinition definition = service.DefinitionOf(mission);
                if (service.Claim(mission))
                {
                    Claimed?.Invoke(this, definition.RewardCoins);
                }
            });
        }

        public void Bind(MissionProgress progress, DailyMissionService missions)
        {
            mission = progress;
            service = missions;
            MissionDefinition definition = service.DefinitionOf(progress);
            gameObject.SetActive(definition != null);
            if (definition == null)
            {
                return;
            }

            description.SetKey(definition.DescriptionKey, definition.Target);
            progressFill.fillAmount = Mathf.Clamp01(progress.Progress / (float)definition.Target);
            progressLabel.SetText("{0}/{1}", progress.Progress, definition.Target);
            rewardLabel.SetText("{0}", definition.RewardCoins);
            claimButton.gameObject.SetActive(!progress.Claimed);
            claimButton.interactable = service.CanClaim(progress);
            claimedMark.SetActive(progress.Claimed);
        }
    }
}
