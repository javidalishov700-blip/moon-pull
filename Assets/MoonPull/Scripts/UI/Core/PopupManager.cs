using System.Collections.Generic;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Shows popups one at a time. Auto-opened popups (idle income, login streak, boss chest) queue up instead of stacking.</summary>
    public sealed class PopupManager : MonoBehaviour
    {
        private readonly Queue<UIScreen> queue = new Queue<UIScreen>();
        private UIScreen current;

        public bool IsShowing => current != null && current.IsVisible;

        public void Enqueue(UIScreen popup)
        {
            if (popup == null || popup == current || queue.Contains(popup))
            {
                return;
            }

            queue.Enqueue(popup);
            TryShowNext();
        }

        /// <summary>Opens immediately, replacing whatever popup is showing (user-initiated taps).</summary>
        public void Open(UIScreen popup)
        {
            if (current != null && current != popup)
            {
                current.Hide();
            }

            current = popup;
            popup.Show();
        }

        public void Close(UIScreen popup)
        {
            popup.Hide();
            if (popup == current)
            {
                current = null;
                TryShowNext();
            }
        }

        public void CloseAll()
        {
            queue.Clear();
            if (current != null)
            {
                current.Hide();
                current = null;
            }
        }

        private void TryShowNext()
        {
            if (IsShowing || queue.Count == 0)
            {
                return;
            }

            current = queue.Dequeue();
            current.Show();
        }
    }
}
