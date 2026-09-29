using MoonPull.Core;
using MoonPull.Core.TimeControl;
using UnityEngine;
using UnityEngine.UI;

namespace MoonPull.UI
{
    public sealed class PausePopup : UIPopup
    {
        [SerializeField] private TimeScaleController timeScale;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;

        private bool paused;

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(() =>
            {
                Resume();
                GameEvents.RaiseRestartRequested();
            });
            homeButton.onClick.AddListener(() =>
            {
                Resume();
                GameEvents.RaiseMenuRequested();
            });
        }

        protected override void OnShown() => paused = true;

        // Also runs when the popup is closed by a state change, so the pause is never left stuck.
        protected override void OnHidden()
        {
            if (paused)
            {
                paused = false;
                timeScale.PopPause();
            }
        }

        private void Resume() => Close();
    }
}
