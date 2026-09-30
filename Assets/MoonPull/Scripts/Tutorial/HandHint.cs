using MoonPull.UI;
using UnityEngine;

namespace MoonPull.Tutorial
{
    public enum HintGesture
    {
        SwipeUpDown,
        FlickUp,
        SwipeDown,
        SwipeUp,
        Hold
    }

    /// <summary>Animated hand next to the moon. The only teaching tool: no text, gestures only.</summary>
    public sealed class HandHint : MonoBehaviour
    {
        [SerializeField] private RectTransform hand;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Vector2 low = new Vector2(0f, -220f);
        [SerializeField] private Vector2 mid = new Vector2(0f, 0f);
        [SerializeField] private Vector2 high = new Vector2(0f, 220f);
        [SerializeField, Min(0.1f)] private float swipeHalfPeriod = 0.7f;
        [SerializeField, Min(0.05f)] private float flickSeconds = 0.35f;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            hand.gameObject.SetActive(false);
        }

        public void Show(HintGesture gesture)
        {
            IsShowing = true;
            hand.gameObject.SetActive(true);
            UiTween.Fade(group, 1f, 0.25f);
            hand.localScale = Vector3.one;
            switch (gesture)
            {
                case HintGesture.SwipeUpDown:
                    UiTween.YoyoAnchored(hand, low, high, swipeHalfPeriod);
                    break;
                case HintGesture.FlickUp: // taught as a quick tap: the tap launches the boat
                    UiTween.Kill(hand);
                    hand.anchoredPosition = mid;
                    UiTween.PulseLoop(hand, 0.8f, 0.22f);
                    break;
                case HintGesture.SwipeDown:
                    UiTween.YoyoAnchored(hand, mid, low, swipeHalfPeriod);
                    break;
                case HintGesture.SwipeUp:
                    UiTween.YoyoAnchored(hand, mid, high, swipeHalfPeriod);
                    break;
                default:
                    UiTween.Kill(hand);
                    hand.anchoredPosition = mid;
                    UiTween.PulseLoop(hand, 1.15f, 0.4f);
                    break;
            }
        }

        public void Hide()
        {
            if (!IsShowing)
            {
                return;
            }

            IsShowing = false;
            UiTween.Fade(group, 0f, 0.25f, () =>
            {
                if (!IsShowing)
                {
                    UiTween.Kill(hand);
                    hand.gameObject.SetActive(false);
                }
            });
        }
    }
}
