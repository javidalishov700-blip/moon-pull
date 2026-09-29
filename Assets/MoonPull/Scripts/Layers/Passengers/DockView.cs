using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Layers.Passengers
{
    /// <summary>Dock with waiting passengers. Consuming it boards them; the dock itself stays.</summary>
    public sealed class DockView : PlacementView
    {
        [SerializeField] private GameObject[] passengerFigures = new GameObject[0];
        [SerializeField] private ParticleSystem boardBurst;

        public override void Setup(int placementIndex, in LevelPlacement placement)
        {
            base.Setup(placementIndex, placement);
            for (int i = 0; i < passengerFigures.Length; i++)
            {
                passengerFigures[i].SetActive(i < placement.Value);
            }
        }

        public override void OnConsumed()
        {
            for (int i = 0; i < passengerFigures.Length; i++)
            {
                passengerFigures[i].SetActive(false);
            }

            if (boardBurst != null)
            {
                boardBurst.Play(true);
            }
        }
    }
}
