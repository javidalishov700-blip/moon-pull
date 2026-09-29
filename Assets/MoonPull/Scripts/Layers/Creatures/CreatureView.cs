using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Layers.Creatures
{
    /// <summary>Animator-driven creature (dolphin, whale, shark, kraken). Maps engage/trigger to animator parameters.</summary>
    public sealed class CreatureView : PlacementView
    {
        private static readonly int EngagedId = Animator.StringToHash("Engaged");
        private static readonly int TriggerId = Animator.StringToHash("Trigger");

        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource triggerSound;

        private bool engaged;

        public override void Setup(int placementIndex, in LevelPlacement placement)
        {
            base.Setup(placementIndex, placement);
            engaged = false;
            if (animator != null)
            {
                animator.Rebind();
                animator.SetBool(EngagedId, false);
            }
        }

        public override void SetEngaged(bool value)
        {
            if (engaged == value || animator == null)
            {
                return;
            }

            engaged = value;
            animator.SetBool(EngagedId, value);
        }

        public override void OnTriggered()
        {
            if (animator != null)
            {
                animator.SetTrigger(TriggerId);
            }

            if (triggerSound != null)
            {
                triggerSound.Play();
            }
        }
    }
}
