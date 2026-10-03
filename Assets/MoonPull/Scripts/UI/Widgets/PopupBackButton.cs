using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.UI
{
    /// <summary>Back arrow on a full-screen popup: closes the popup it sits in.</summary>
    public sealed class PopupBackButton : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            var popup = GetComponentInParent<UIPopup>();
            if (popup != null) popup.Close();
        }
    }
}
