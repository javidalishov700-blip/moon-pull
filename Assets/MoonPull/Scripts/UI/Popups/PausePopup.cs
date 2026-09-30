using MoonPull.Core.TimeControl;
using MoonPull.Core;
using MoonPull.Rescue;
using UnityEngine.UI;
using UnityEngine;

namespace MoonPull.UI
{
    public sealed class PausePopup : UIPopup
    {
        [SerializeField] private TimeScaleController timeScale;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private NightRescue rescue;

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
            if (finishButton != null)
            {
                // Ends the night right here: everyone aboard gets home and the run is scored as usual.
                finishButton.onClick.AddListener(() =>
                {
                    Resume();
                    if (rescue != null)
                    {
                        rescue.EndNightNow();
                    }
                });
            }

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
