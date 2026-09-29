using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Layers.Weather;
using MoonPull.Level;
using MoonPull.Meta;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Gameplay.Visuals
{
    /// <summary>
    /// Applies the region palette (sky gradient, water, fog) on level start and on the menu, blending so a region
    /// change feels like sailing into new waters.
    /// </summary>
    public sealed class RegionAmbience : MonoBehaviour
    {
        private static readonly int SkyTopId = Shader.PropertyToID("_MP_SkyTop");
        private static readonly int SkyBottomId = Shader.PropertyToID("_MP_SkyBottom");

        [SerializeField] private LevelSession session;
        [SerializeField] private MetaGame meta;
        [SerializeField] private WaterSurface water;
        [SerializeField] private WeatherSystem weather;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(0.01f)] private float blendSeconds = 1.2f;

        private Color skyTop;
        private Color skyBottom;
        private Color targetTop;
        private Color targetBottom;

        private void OnEnable()
        {
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.StateChanged -= OnStateChanged;
        }

        private void OnLevelStarted(LevelStartArgs args) => Apply(session.Region);

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu && meta.IsInitialized)
            {
                int level = meta.Progress.NextLevelToPlay();
                Apply(meta.Regions.ForLevel(level, meta.Progress.LevelsPerRegion));
            }
        }

        private void Apply(RegionDefinition region)
        {
            if (region == null)
            {
                return;
            }

            targetTop = region.SkyTop;
            targetBottom = region.SkyBottom;
            water.ApplyPalette(region.WaterShallow, region.WaterDeep, region.Foam);
            weather.SetFogColor(region.Fog);
        }

        private void Update()
        {
            float t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / blendSeconds * 3f);
            skyTop = Color.Lerp(skyTop, targetTop, t);
            skyBottom = Color.Lerp(skyBottom, targetBottom, t);
            Shader.SetGlobalColor(SkyTopId, skyTop);
            Shader.SetGlobalColor(SkyBottomId, skyBottom);
            if (gameplayCamera != null)
            {
                gameplayCamera.backgroundColor = skyBottom;
            }
        }
    }
}
