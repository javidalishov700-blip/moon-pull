using MoonPull.Online;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.UI
{
    /// <summary>Trophy button: opens the Game Center leaderboard (signs in first if needed).</summary>
    public sealed class LeaderboardButton : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData) => Leaderboards.Show();
    }
}
