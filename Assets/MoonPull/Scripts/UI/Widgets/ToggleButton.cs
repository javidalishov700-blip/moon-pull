using System;
using System.Collections;
using MoonPull.Ads;
using MoonPull.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    /// <summary>On/off button for settings, with a localized On/Off label.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ToggleButton : MonoBehaviour
    {
        [SerializeField] private GameObject onVisual;
        [SerializeField] private GameObject offVisual;
        [SerializeField] private LocalizedText stateLabel;

        public event Action<bool> Toggled;

        public bool IsOn { get; private set; }

        private void Awake() => GetComponent<Button>().onClick.AddListener(() =>
        {
            SetState(!IsOn);
            Toggled?.Invoke(IsOn);
        });

        public void SetState(bool on)
        {
            IsOn = on;
            onVisual.SetActive(on);
            offVisual.SetActive(!on);
            if (stateLabel != null)
            {
                stateLabel.SetKey(on ? LocKeys.CommonOn : LocKeys.CommonOff);
            }
        }
    }
}
