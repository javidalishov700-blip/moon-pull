using MoonPull.Level;
using UnityEngine;

namespace MoonPull.Obstacles
{
    /// <summary>Chest on the seabed. Sparkles only when the tide is low enough to reach it, teaching the risk visually.</summary>
    public sealed class ChestView : PlacementView
    {
        private static readonly int WaterLevelId = Shader.PropertyToID("_MP_WaterLevel");

        [SerializeField] private GameObject revealSparkle;
        [Tooltip("Sparkle when the sea level is within this distance above the chest.")]
        [SerializeField, Min(0f)] private float revealDepth = 1.2f;

        private float chestY;

        public override void Setup(int placementIndex, in LevelPlacement placement)
        {
            base.Setup(placementIndex, placement);
            chestY = placement.Y;
            if (revealSparkle != null)
            {
                revealSparkle.SetActive(false);
            }
        }

        public override void OnConsumed()
        {
            base.OnConsumed();
            if (revealSparkle != null)
            {
                revealSparkle.SetActive(false);
            }
        }

        private void Update()
        {
            if (revealSparkle == null || !VisualRoot.gameObject.activeSelf)
            {
                return;
            }

            bool revealed = Shader.GetGlobalFloat(WaterLevelId) - chestY < revealDepth;
            if (revealSparkle.activeSelf != revealed)
            {
                revealSparkle.SetActive(revealed);
            }
        }
    }
}
