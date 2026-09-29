using System;
using MoonPull.Core;
using MoonPull.Localization;
using MoonPull.Meta;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class MissionsPopup : UIPopup
    {
        [SerializeField] private MetaGame meta;
        [SerializeField] private CoinFlyEffect coinFly;
        [SerializeField] private MissionRow[] rows = new MissionRow[3];
        [SerializeField] private LocalizedText refreshLabel;

        private float nextRefresh;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].Claimed += OnClaimed;
            }
        }

        protected override void OnShown()
        {
            meta.Missions.Refresh();
            Bind();
        }

        protected override void OnUpdate()
        {
            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + 1f;
            DateTime nowLocal = Services.Get<IClock>().UtcNow.ToLocalTime();
            refreshLabel.SetKey(LocKeys.MissionsRefresh, UiText.Duration(nowLocal.Date.AddDays(1) - nowLocal));
        }

        private void Bind()
        {
            var missions = meta.Missions.Today;
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < missions.Count;
                rows[i].gameObject.SetActive(has);
                if (has)
                {
                    rows[i].Bind(missions[i], meta.Missions);
                }
            }
        }

        private void OnClaimed(MissionRow row, int coins)
        {
            coinFly.PlayCredited(RectTransformUtility.WorldToScreenPoint(null, row.transform.position), coins);
            Bind();
        }
    }
}
