using MoonPull.Localization;
using MoonPull.Online;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Menu line: your best night, and the next player to beat on the Game Center leaderboard.</summary>
    public sealed class RecordLabel : MonoBehaviour
    {
        [SerializeField] private LocalizedText label;

        private void OnEnable()
        {
            Leaderboards.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => Leaderboards.Changed -= Refresh;

        private void Refresh()
        {
            if (label == null) return;
            int best = Leaderboards.PersonalBest;
            if (best <= 0)
            {
                label.SetKey("menu.record_none");
            }
            else if (!string.IsNullOrEmpty(Leaderboards.RivalName))
            {
                label.SetKey("menu.record_rival", best, Leaderboards.RivalName, Leaderboards.RivalScore);
            }
            else
            {
                label.SetKey("menu.record_best", best);
            }
        }
    }
}
