using System;
using System.Collections;
using MoonPull.Ads;
using MoonPull.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>Fits a RectTransform to the device safe area (notches, home indicator).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect applied;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != applied)
            {
                Apply();
            }
        }

        private void Apply()
        {
            applied = Screen.safeArea;
            var rect = (RectTransform)transform;
            Vector2 min = applied.position;
            Vector2 max = applied.position + applied.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
