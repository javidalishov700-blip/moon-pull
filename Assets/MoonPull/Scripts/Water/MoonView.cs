using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>Positions the moon in the sky, drives its star trail and brightness (eclipse dark, full moon bright).</summary>
    public sealed class MoonView : MonoBehaviour
    {
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        [SerializeField] private MoonConfig config;
        [SerializeField] private MoonController moon;
        [SerializeField] private Transform moonTransform;
        [SerializeField] private TrailRenderer starTrail;
        [SerializeField] private Renderer moonRenderer;
        [SerializeField] private Light moonLight;
        [SerializeField, Min(0.1f)] private float brightnessSharpness = 6f;

        private MaterialPropertyBlock block;
        private float brightness = 1f;
        private float targetBrightness = 1f;
        private float baseLightIntensity;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (moonLight != null)
            {
                baseLightIntensity = moonLight.intensity;
            }
        }

        /// <summary>1 = normal, 0 = eclipsed, &gt;1 = full moon glow.</summary>
        public void SetTargetBrightness(float value)
        {
            targetBrightness = value;
        }

        private void LateUpdate()
        {
            Vector2 range = config.VisualLocalYRange;
            Vector3 local = moonTransform.localPosition;
            local.y = Mathf.Lerp(range.x, range.y, moon.Height01);
            moonTransform.localPosition = local;

            if (starTrail != null)
            {
                starTrail.emitting = Mathf.Abs(moon.Velocity01) > config.TrailSpeedThreshold;
            }

            brightness = Mathf.Lerp(brightness, targetBrightness, 1f - Mathf.Exp(-brightnessSharpness * Time.unscaledDeltaTime));
            if (moonRenderer != null)
            {
                moonRenderer.GetPropertyBlock(block);
                block.SetFloat(BrightnessId, brightness);
                moonRenderer.SetPropertyBlock(block);
            }

            if (moonLight != null)
            {
                moonLight.intensity = baseLightIntensity * brightness;
            }
        }
    }
}
