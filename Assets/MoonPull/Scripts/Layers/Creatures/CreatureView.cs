using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Layers.Creatures
{
    /// <summary>
    /// Creature (dolphin, whale, shark, Kraken). Uses an Animator when one is assigned; otherwise animates procedurally:
    /// the body rises while engaged (surfaced whale, circling shark fin, risen Kraken) and punches on trigger.
    /// </summary>
    public sealed class CreatureView : PlacementView
    {
        private static readonly int EngagedId = Animator.StringToHash("Engaged");
        private static readonly int TriggerId = Animator.StringToHash("Trigger");

        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource triggerSound;
        [Tooltip("Procedural mode: body offset while hidden / engaged.")]
        [SerializeField] private Transform body;
        [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -1.2f, 0f);
        [SerializeField] private Vector3 engagedOffset = Vector3.zero;
        [SerializeField] private bool alwaysEngaged;
        [SerializeField, Min(0.1f)] private float riseSharpness = 6f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.12f;
        [SerializeField, Min(0.1f)] private float bobFrequency = 1.4f;

        private bool engaged;
        private float triggerTime = -10f;
        private Vector3 current;

        public override void Setup(int placementIndex, in LevelPlacement placement)
        {
            base.Setup(placementIndex, placement);
            engaged = alwaysEngaged;
            current = engaged ? engagedOffset : hiddenOffset;
            triggerTime = -10f;
            if (animator != null)
            {
                animator.Rebind();
                animator.SetBool(EngagedId, engaged);
            }
        }

        public override void SetEngaged(bool value)
        {
            value |= alwaysEngaged;
            if (engaged == value)
            {
                return;
            }

            engaged = value;
            if (animator != null)
            {
                animator.SetBool(EngagedId, value);
            }
        }

        public override void OnTriggered()
        {
            triggerTime = Time.time;
            if (animator != null)
            {
                animator.SetTrigger(TriggerId);
            }

            if (triggerSound != null)
            {
                triggerSound.Play();
            }
        }

        private void Update()
        {
            if (animator != null || body == null)
            {
                return;
            }

            Vector3 target = engaged ? engagedOffset : hiddenOffset;
            current = Vector3.Lerp(current, target, 1f - Mathf.Exp(-riseSharpness * Time.deltaTime));
            float bob = Mathf.Sin(Time.time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
            body.localPosition = current + new Vector3(0f, bob, 0f);

            float sinceTrigger = Time.time - triggerTime;
            float punch = sinceTrigger < 0.5f ? Mathf.Sin(sinceTrigger / 0.5f * Mathf.PI) * 0.25f : 0f;
            body.localScale = Vector3.one * (1f + punch);
        }
    }
}
