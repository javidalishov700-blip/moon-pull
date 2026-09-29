using MoonPull.Level;
using MoonPull.Obstacles;
using UnityEngine;

namespace MoonPull.Layers.Weather
{
    /// <summary>Lighthouse placed before obstacles inside fog. Its sweeping spotlight reveals what the fog hides.</summary>
    public sealed class LighthouseBeamView : PlacementView
    {
        private static readonly int FogAmountId = Shader.PropertyToID("_MP_FogAmount");

        [SerializeField] private Transform beamPivot;
        [SerializeField] private Light beamLight;
        [SerializeField] private Vector2 sweepDegrees = new Vector2(-10f, 35f);
        [SerializeField, Min(0.01f)] private float sweepPeriod = 2.4f;
        [SerializeField, Min(0f)] private float maxIntensity = 6f;

        private float phase;

        public override void Setup(int placementIndex, in LevelPlacement placement)
        {
            base.Setup(placementIndex, placement);
            phase = 0f;
        }

        private void Update()
        {
            phase += Time.deltaTime / sweepPeriod;
            float t = 0.5f + 0.5f * Mathf.Sin(phase * 2f * Mathf.PI);
            if (beamPivot != null)
            {
                beamPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(sweepDegrees.x, sweepDegrees.y, t));
            }

            if (beamLight != null)
            {
                beamLight.intensity = maxIntensity * Shader.GetGlobalFloat(FogAmountId);
            }
        }
    }
}
