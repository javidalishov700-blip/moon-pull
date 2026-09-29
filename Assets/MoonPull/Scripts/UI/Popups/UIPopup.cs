using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Popup with a close button that returns control to the <see cref="PopupManager"/> queue.</summary>
    public abstract class UIPopup : UIScreen
    {
        [SerializeField] private PopupManager popups;
        [SerializeField] private Button closeButton;

        protected PopupManager Popups => popups;

        protected virtual void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Close() => popups.Close(this);
    }
}
