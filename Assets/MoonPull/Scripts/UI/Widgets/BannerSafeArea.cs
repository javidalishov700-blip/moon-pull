using System.Collections;
using System;
using MoonPull.Ads;
using MoonPull.Localization;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>
    /// Lifts content above the adaptive banner so no button ever sits next to or under the ad (AdMob placement policy).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BannerSafeArea : MonoBehaviour
    {
        [SerializeField] private AdsCoordinator coordinator;
        [SerializeField] private Canvas canvas;
        [Tooltip("Extra gap between the banner and the lowest button, in canvas units.")]
        [SerializeField, Min(0f)] private float extraGap = 24f;

        private void OnEnable()
        {
            coordinator.BannerLayoutChanged += Apply;
            Apply();
        }

        private void OnDisable() => coordinator.BannerLayoutChanged -= Apply;

        private void Apply()
        {
            float bannerPixels = coordinator.BannerHeightPixels;
            float height = bannerPixels > 0f ? bannerPixels / canvas.scaleFactor + extraGap : 0f;
            var rect = (RectTransform)transform;
            Vector2 offset = rect.offsetMin;
            offset.y = height;
            rect.offsetMin = offset;
        }
    }
}
