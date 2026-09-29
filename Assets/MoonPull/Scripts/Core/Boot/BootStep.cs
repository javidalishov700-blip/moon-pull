using System.Collections;
using UnityEngine;

namespace MoonPull.Core.Boot
{
    /// <summary>
    /// One ordered unit of startup work (install services, load save, fetch config, resolve consent, init SDKs).
    /// New features add a step instead of editing the bootstrapper.
    /// </summary>
    public abstract class BootStep : MonoBehaviour
    {
        [Tooltip("Seconds before the step is abandoned and boot continues. 0 = wait forever (use for consent UI).")]
        [SerializeField, Min(0f)] private float timeoutSeconds = 8f;
        [Tooltip("Switch GameManager to the Consent state before running this step.")]
        [SerializeField] private bool runsInConsentState;

        public float TimeoutSeconds => timeoutSeconds;
        public bool RunsInConsentState => runsInConsentState;

        public abstract IEnumerator Run();
    }
}
