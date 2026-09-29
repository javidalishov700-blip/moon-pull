using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Base for full screens and popups. Input stays disabled until the entrance animation finishes, which prevents
    /// the tap that closed an ad or the previous screen from landing on a new button (accidental-click policy).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;
        [SerializeField, Min(0f)] private float inputLockSeconds = 0.3f;

        private CanvasGroup group;
        private float unlockAt;

        public bool IsVisible { get; private set; }

        protected CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        public void Show()
        {
            if (IsVisible)
            {
                return;
            }

            IsVisible = true;
            gameObject.SetActive(true);
            Group.interactable = false;
            Group.blocksRaycasts = true;
            Group.alpha = 0f;
            UiTween.Fade(Group, 1f, fadeDuration);
            if (content != null)
            {
                UiTween.PopIn(content, fadeDuration + 0.1f);
            }

            unlockAt = Time.unscaledTime + inputLockSeconds;
            OnShown();
        }

        public void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            IsVisible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            OnHidden();
            UiTween.Fade(Group, 0f, fadeDuration, () =>
            {
                if (!IsVisible)
                {
                    gameObject.SetActive(false);
                }
            });
        }

        /// <summary>Hides immediately without animation (initial state).</summary>
        public void HideInstant()
        {
            IsVisible = false;
            Group.alpha = 0f;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private void Update()
        {
            // Unscaled so screens opened while paused (timeScale 0) still unlock.
            if (IsVisible && !Group.interactable && Time.unscaledTime >= unlockAt)
            {
                Group.interactable = true;
            }

            OnUpdate();
        }

        protected virtual void OnUpdate()
        {
        }
    }
}
