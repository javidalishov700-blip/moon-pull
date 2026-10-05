using MoonPull.Rescue;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>DIVE: held down = the boat dives down the wave face. BOOST mode: fills with charge and fires when full.</summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private NightRescue rescue;
        [SerializeField] private RectTransform face;
        [SerializeField] private bool boost;
        [SerializeField] private Image fill;
        [SerializeField] private CanvasGroup group;

        private int pointers;
        private bool down => pointers > 0;
        private float pulse;

        public void OnPointerDown(PointerEventData eventData)
        {
            pointers++;
            if (rescue == null) return;
            if (boost) rescue.UseBoost();
            else rescue.ButtonHold = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointers = Mathf.Max(0, pointers - 1);
            if (rescue != null && !boost && pointers == 0) rescue.ButtonHold = false;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnDisable();
        }

        private void OnDisable()
        {
            pointers = 0;
            if (rescue != null && !boost) rescue.ButtonHold = false;
        }

        private void Update()
        {
            float scale = down ? 0.9f : 1f;
            if (boost && rescue != null)
            {
                float charge = rescue.BoostCharge;
                if (fill != null) fill.fillAmount = charge;
                bool ready = charge >= 1f;
                if (group != null) group.alpha = ready ? 1f : 0.65f;
                pulse += Time.unscaledDeltaTime * (ready ? 6f : 0f);
                if (ready) scale *= 1f + 0.07f * Mathf.Sin(pulse);
            }

            if (face != null) face.localScale = Vector3.Lerp(face.localScale, Vector3.one * scale, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
        }
    }
}
