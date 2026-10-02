using MoonPull.Rescue;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MoonPull.UI
{
    /// <summary>Big JUMP button on the HUD: fires on finger-down (no waiting for release) and bounces.</summary>
    public sealed class JumpButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private NightRescue rescue;
        [SerializeField] private RectTransform face;

        private float punch;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (rescue != null)
            {
                rescue.RequestHop();
            }

            punch = 1f;
        }

        private void Update()
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 6f);
            if (face != null)
            {
                face.localScale = Vector3.one * (1f - 0.12f * Mathf.Sin(punch * Mathf.PI));
            }
        }
    }
}
