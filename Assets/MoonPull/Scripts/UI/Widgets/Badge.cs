using System;
using System.Collections;
using MoonPull.Ads;
using MoonPull.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>Notification dot with an optional count.</summary>
    public sealed class Badge : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text count;

        public void Set(int value)
        {
            root.SetActive(value > 0);
            if (count != null && value > 0)
            {
                count.SetText("{0}", value);
            }
        }

        public void Set(bool visible)
        {
            root.SetActive(visible);
            if (count != null)
            {
                count.text = string.Empty;
            }
        }
    }
}
