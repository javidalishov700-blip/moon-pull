using MoonPull.Core;
using UnityEngine;

namespace MoonPull.UI
{
    /// <summary>Shows a persistent element (e.g. the wallet bar) only in the listed game states.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class StateVisibility : MonoBehaviour
    {
        [SerializeField] private GameState[] visibleIn = { GameState.Menu, GameState.Win, GameState.Shop };

        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            Apply(GameState.Boot);
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDestroy() => GameEvents.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameState from, GameState to) => Apply(to);

        private void Apply(GameState state)
        {
            bool visible = System.Array.IndexOf(visibleIn, state) >= 0;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
