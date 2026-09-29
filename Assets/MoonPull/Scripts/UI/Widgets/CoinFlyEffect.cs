using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Pooled coin icons that arc from a point to the wallet counter. Requires a Screen Space - Overlay canvas.</summary>
    public sealed class CoinFlyEffect : MonoBehaviour
    {
        [SerializeField] private RectTransform[] icons = new RectTransform[0];
        [SerializeField] private CoinCounter counter;
        [SerializeField] private Camera worldCamera;
        [SerializeField, Min(0.1f)] private float duration = 0.65f;
        [SerializeField, Min(0f)] private float stagger = 0.04f;
        [SerializeField, Min(0f)] private float spreadPixels = 90f;
        [SerializeField, Min(0f)] private float arcHeightPixels = 220f;

        private int inFlight;

        private void Awake()
        {
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].gameObject.SetActive(false);
            }
        }

        public void PlayFromWorld(Vector3 worldPosition, long amount)
        {
            PlayFromScreen(worldCamera.WorldToScreenPoint(worldPosition), amount);
        }

        public void PlayFromScreen(Vector2 screenPosition, long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int count = (int)Mathf.Min(icons.Length, Mathf.Max(1, amount));
            long share = amount / count;
            long remainder = amount - share * count;
            Vector3 target = counter.Icon.position;
            counter.Hold();
            inFlight += count;

            for (int i = 0; i < count; i++)
            {
                RectTransform icon = icons[i];
                icon.gameObject.SetActive(true);
                Vector3 start = screenPosition + Random.insideUnitCircle * spreadPixels;
                Vector3 control = (start + target) * 0.5f + Vector3.up * arcHeightPixels;
                long value = share + (i == count - 1 ? remainder : 0);
                UiTween.Arc(icon, start, control, target, duration, i * stagger, () =>
                {
                    icon.gameObject.SetActive(false);
                    counter.Bump(value);
                    inFlight--;
                    if (inFlight == 0)
                    {
                        counter.Release();
                    }
                });
            }
        }
    }
}
