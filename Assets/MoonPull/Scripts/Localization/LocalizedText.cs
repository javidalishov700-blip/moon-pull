using MoonPull.Core;
using TMPro;
using UnityEngine;

namespace MoonPull.Localization
{
    /// <summary>Binds a TMP label to a localization key and refreshes on language change. Supports {0}-style arguments.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<LocalizedText> Active = new System.Collections.Generic.List<LocalizedText>(128);

        [SerializeField] private string key;

        private TMP_Text label;
        private object[] args;
        private ILocalizationService service;

        public string Key => key;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        /// <summary>Refreshes every enabled label. Called once the localization service is registered during boot.</summary>
        public static void RefreshAllActive()
        {
            for (int i = 0; i < Active.Count; i++)
            {
                Active[i].Refresh();
            }
        }

        private void OnEnable()
        {
            Active.Add(this);
            Refresh();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (service != null)
            {
                service.LanguageChanged -= Refresh;
                service = null;
            }
        }

        public void SetKey(string newKey, params object[] formatArgs)
        {
            key = newKey;
            args = formatArgs != null && formatArgs.Length > 0 ? formatArgs : null;
            Refresh();
        }

        public void SetArgs(params object[] formatArgs)
        {
            args = formatArgs;
            Refresh();
        }

        public void Refresh()
        {
            if (label == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            // Labels can be enabled before boot registers the service; bind lazily on first successful lookup.
            ILocalizationService current = service;
            if (current == null)
            {
                if (!Services.TryGet(out current))
                {
                    label.text = key;
                    return;
                }

                if (isActiveAndEnabled)
                {
                    service = current;
                    service.LanguageChanged += Refresh;
                }
            }

            label.text = args == null ? current.Get(key) : current.Format(key, args);
        }
    }
}
