using MoonPull.Core.Pooling;
using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Obstacles
{
    /// <summary>
    /// Visual for any placement. Pivot conventions: low obstacles at their top edge, high obstacles at their bottom
    /// edge, sandbars at their top, pickups at their centre, so <see cref="LevelPlacement.Y"/> maps directly to position.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public class PlacementView : MonoBehaviour, IPoolable
    {
        [Tooltip("Hidden when consumed (pickup collected, obstacle smashed).")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private ParticleSystem consumeBurst;
        [Tooltip("Width the mesh was authored at. Visual root is scaled on X to match the placement width. 0 = never scale.")]
        [SerializeField, Min(0f)] private float authoredWidth;

        public int PlacementIndex { get; private set; } = -1;

        protected Transform VisualRoot => visualRoot;

        public virtual void Setup(int placementIndex, in LevelPlacement placement)
        {
            PlacementIndex = placementIndex;
            transform.position = new Vector3(placement.X, placement.Y, 0f);

            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(true);
                if (authoredWidth > 0f && placement.Width > 0f)
                {
                    Vector3 scale = visualRoot.localScale;
                    scale.x = placement.Width / authoredWidth;
                    visualRoot.localScale = scale;
                }
            }
        }

        /// <summary>Collected or smashed. Hides the visual and plays the burst; the runner despawns it later.</summary>
        public virtual void OnConsumed()
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }

            if (consumeBurst != null)
            {
                consumeBurst.Play(true);
            }
        }

        /// <summary>One-shot reaction (dolphin jump, whale spout, kraken recoil). Default: nothing.</summary>
        public virtual void OnTriggered()
        {
        }

        /// <summary>Continuous state such as a surfaced whale, circling shark or risen Kraken. Default: nothing.</summary>
        public virtual void SetEngaged(bool engaged)
        {
        }

        public virtual void OnSpawned()
        {
        }

        public virtual void OnDespawned()
        {
            PlacementIndex = -1;
            if (consumeBurst != null)
            {
                consumeBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
