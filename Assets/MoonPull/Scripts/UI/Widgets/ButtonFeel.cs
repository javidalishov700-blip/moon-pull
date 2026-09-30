using MoonPull.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>
    /// Tactile buttons: squash to 93% on press, spring back with a small overshoot on release, and a soft click
    /// with a little pitch variation. Runs on unscaled time so it also works while the game is paused.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressedScale = 0.93f;

        private Button button;
        private Vector3 baseScale = Vector3.one;
        private float target = 1f;
        private float current = 1f;
        private float velocity;

        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            current = target = 1f;
            velocity = 0f;
            transform.localScale = baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.IsInteractable())
            {
                return;
            }

            target = PressedScale;
            AudioService audio = AudioService.Current;
            if (audio != null)
            {
                audio.PlaySfx(SfxId.UiTap, Random.Range(0.95f, 1.05f), 0.8f);
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (target < 1f)
            {
                target = 1f;
                velocity += 3.5f; // kick for the overshoot
            }
        }

        private void Update()
        {
            if (Mathf.Abs(current - target) < 0.0005f && Mathf.Abs(velocity) < 0.0005f)
            {
                return;
            }

            // Critically-under-damped spring: fast, with one small bounce.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            velocity += ((target - current) * 520f - velocity * 26f) * dt;
            current += velocity * dt;
            transform.localScale = baseScale * current;
        }
    }
}
