using MoonPull.Config;
using MoonPull.Core;
using UnityEngine;

namespace MoonPull.Mechanics
{
    /// <summary>Single owner of the level score. Applies the near-miss chain multiplier and Full Moon bonus to every source.</summary>
    public sealed class ScoreSystem : MonoBehaviour
    {
        [SerializeField] private ScoreConfig config;

        private int chainMultiplier = 1;
        private bool fullMoon;

        public int Score { get; private set; }

        public int CurrentMultiplier => chainMultiplier * (fullMoon ? config.FullMoonMultiplier : 1);

        public void ResetForLevel()
        {
            Score = 0;
            chainMultiplier = 1;
            fullMoon = false;
            GameEvents.RaiseScoreChanged(Score, CurrentMultiplier);
        }

        public void SetChainMultiplier(int multiplier)
        {
            chainMultiplier = Mathf.Max(1, multiplier);
            GameEvents.RaiseScoreChanged(Score, CurrentMultiplier);
        }

        public void SetFullMoon(bool active)
        {
            fullMoon = active;
            GameEvents.RaiseScoreChanged(Score, CurrentMultiplier);
        }

        /// <summary>Flat bonus scaled by the live multiplier (used by Perfect Crest launches).</summary>
        public int AddBonus(int basePoints)
        {
            int points = basePoints * CurrentMultiplier;
            Score += points;
            GameEvents.RaiseScoreChanged(Score, CurrentMultiplier);
            return points;
        }

        public int Add(ScoreSource source, int count = 1)
        {
            int points = ScoreRules.Apply(ScoreRules.BasePoints(config, source) * count, chainMultiplier, fullMoon, config.FullMoonMultiplier);
            Score += points;
            GameEvents.RaiseScoreChanged(Score, CurrentMultiplier);
            return points;
        }
    }
}
