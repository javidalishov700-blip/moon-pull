using System.Collections.Generic;
using System;

namespace MoonPull.Core
{
    /// <summary>
    /// Pure C# state machine with an explicit transition whitelist, so illegal flows (e.g. Win → Rewinding) fail loudly.
    /// </summary>
    public sealed class GameStateMachine
    {
        private static readonly Dictionary<GameState, GameState[]> AllowedTransitions = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot,      new[] { GameState.Consent, GameState.Menu } },
            { GameState.Consent,   new[] { GameState.Menu } },
            { GameState.Menu,      new[] { GameState.Playing, GameState.Shop } },
            { GameState.Shop,      new[] { GameState.Menu, GameState.Playing } },
            { GameState.Playing,   new[] { GameState.Fail, GameState.Win, GameState.Menu, GameState.Playing, GameState.Rewinding } },
            { GameState.Fail,      new[] { GameState.Rewinding, GameState.Playing, GameState.Menu } },
            { GameState.Rewinding, new[] { GameState.Playing, GameState.Fail } },
            { GameState.Win,       new[] { GameState.Playing, GameState.Menu, GameState.Shop } }
        };

        /// <summary>Raised after a successful transition with (from, to).</summary>
        public event Action<GameState, GameState> Changed;

        /// <summary>The active state.</summary>
        public GameState Current { get; private set; }

        /// <summary>Creates a machine starting in <paramref name="initial"/>.</summary>
        public GameStateMachine(GameState initial = GameState.Boot)
        {
            Current = initial;
        }

        /// <summary>True when <paramref name="target"/> is reachable from the current state.</summary>
        public bool CanTransition(GameState target)
        {
            if (!AllowedTransitions.TryGetValue(Current, out GameState[] targets))
            {
                return false;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Transitions if allowed. Returns false and leaves state untouched otherwise.</summary>
        public bool TryTransition(GameState target)
        {
            if (!CanTransition(target))
            {
                return false;
            }

            GameState previous = Current;
            Current = target;
            Changed?.Invoke(previous, target);
            return true;
        }
    }
}
