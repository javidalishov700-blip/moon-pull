using System.Collections;
using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Core.Boot
{
    /// <summary>Runs <see cref="BootStep"/>s in order with per-step timeouts, then hands control to the menu.</summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BootStep[] steps = new BootStep[0];
        [SerializeField] private GameObject loadingOverlay;

        private IEnumerator Start()
        {
            Application.targetFrameRate = config.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            Input.multiTouchEnabled = config.MultiTouchEnabled;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            for (int i = 0; i < steps.Length; i++)
            {
                BootStep step = steps[i];
                if (step == null)
                {
                    continue;
                }

                if (step.RunsInConsentState && gameManager.State == GameState.Boot)
                {
                    gameManager.EnterConsent();
                }

                yield return RunWithTimeout(step);
            }

            gameManager.CompleteBoot();

            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        private IEnumerator RunWithTimeout(BootStep step)
        {
            var status = new StepStatus();
            Coroutine routine = StartCoroutine(Execute(step, status));
            float elapsed = 0f;

            while (!status.Done)
            {
                if (step.TimeoutSeconds > 0f && elapsed >= step.TimeoutSeconds)
                {
                    StopCoroutine(routine);
                    Debug.LogWarning($"Boot step {step.GetType().Name} timed out after {step.TimeoutSeconds}s. Continuing with defaults.");
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // An exception inside a step kills its coroutine without setting Done; the timeout then recovers boot.
        private static IEnumerator Execute(BootStep step, StepStatus status)
        {
            yield return step.Run();
            status.Done = true;
        }

        private sealed class StepStatus
        {
            public bool Done;
        }
    }
}
