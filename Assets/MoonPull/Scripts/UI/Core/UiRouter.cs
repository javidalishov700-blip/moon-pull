using MoonPull.Core;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Maps game states to screens. Screens never open each other; they raise intents and the router follows state.</summary>
    public sealed class UiRouter : MonoBehaviour
    {
        [SerializeField] private UIScreen loading;
        [SerializeField] private UIScreen menu;
        [SerializeField] private UIScreen hud;
        [SerializeField] private UIScreen fail;
        [SerializeField] private UIScreen win;
        [SerializeField] private UIScreen shop;
        [SerializeField] private PopupManager popups;

        private UIScreen current;

        private void Awake()
        {
            // Every screen and popup starts hidden; they are all built active so the generator can lay them out.
            foreach (UIScreen screen in FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (screen != loading)
                {
                    screen.HideInstant();
                }
            }
            current = loading;
            loading.Show();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            UIScreen next = ScreenFor(to);
            if (from != to)
            {
                popups.CloseAll();
            }

            if (next == current)
            {
                return;
            }

            if (current != null)
            {
                current.Hide();
            }

            current = next;
            current.Show();
        }

        private UIScreen ScreenFor(GameState state)
        {
            switch (state)
            {
                case GameState.Menu: return menu;
                case GameState.Playing:
                case GameState.Rewinding: return hud;
                case GameState.Fail: return fail;
                case GameState.Win: return win;
                case GameState.Shop: return shop;
                default: return loading;
            }
        }
    }
}
