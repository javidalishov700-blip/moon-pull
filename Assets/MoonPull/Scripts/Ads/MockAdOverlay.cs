using System;
using UnityEngine;

namespace MoonPull.Ads
{
    /// <summary>IMGUI fake ad screen for the Editor. Rewarded ads offer "Finish" and "Close early" to test both paths.</summary>
    public sealed class MockAdOverlay : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float bannerHeight = 150f;

        private string title;
        private bool isRewarded;
        private bool bannerVisible;
        private Action<bool> onClosed;

        public bool IsShowing { get; private set; }
        public float BannerHeight => bannerHeight;

        public void Show(string adTitle, bool rewarded, Action<bool> closed)
        {
            title = adTitle;
            isRewarded = rewarded;
            onClosed = closed;
            IsShowing = true;
            AudioListener.pause = true;
        }

        public void ShowBanner(bool visible) => bannerVisible = visible;

        private void Close(bool completed)
        {
            IsShowing = false;
            AudioListener.pause = false;
            Action<bool> callback = onClosed;
            onClosed = null;
            callback?.Invoke(completed);
        }

        private void OnGUI()
        {
            if (bannerVisible)
            {
                GUI.Box(new Rect(0f, Screen.height - bannerHeight, Screen.width, bannerHeight), "MOCK BANNER");
            }

            if (!IsShowing)
            {
                return;
            }

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), $"MOCK AD\n{title}");
            float width = Screen.width * 0.6f;
            float x = (Screen.width - width) * 0.5f;
            if (GUI.Button(new Rect(x, Screen.height * 0.45f, width, 120f), isRewarded ? "Finish (grant reward)" : "Close"))
            {
                Close(true);
            }

            if (isRewarded && GUI.Button(new Rect(x, Screen.height * 0.45f + 150f, width, 120f), "Close early (no reward)"))
            {
                Close(false);
            }
        }
    }
}
