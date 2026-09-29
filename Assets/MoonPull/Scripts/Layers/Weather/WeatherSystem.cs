using MoonPull.Boat;
using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using MoonPull.Level;
using MoonPull.Water;
using UnityEngine;

namespace MoonPull.Layers.Weather
{
    /// <summary>
    /// Applies storm, fog and eclipse from the level plan. Effects are recomputed from the boat's X every tick, so
    /// rewinding into or out of weather is always consistent without extra snapshot data.
    /// </summary>
    public sealed class WeatherSystem : MonoBehaviour, ISimulationTickable
    {
        private const int MaxEvents = 4;
        private static readonly int FogAmountId = Shader.PropertyToID("_MP_FogAmount");

        private enum Phase : byte
        {
            Idle,
            Warning,
            Active,
            Ended
        }

        [SerializeField] private WeatherConfig config;
        [SerializeField] private LevelRunner runner;
        [SerializeField] private BoatController boat;
        [SerializeField] private TideModel tide;
        [SerializeField] private WaterSurface water;
        [SerializeField] private MoonController moon;
        [SerializeField] private MoonView moonView;

        private readonly Phase[] phases = new Phase[MaxEvents];
        private Color fogColor = Color.gray;

        /// <summary>Current weather affecting play, for HUD icons and audio.</summary>
        public WeatherKind ActiveWeather { get; private set; }

        public float FogAmount { get; private set; }

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

        public void SetFogColor(Color color) => fogColor = color;

        private void OnLevelStarted(LevelStartArgs args)
        {
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = Phase.Idle;
            }

            ClearEffects();
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Menu || to == GameState.Shop || to == GameState.Win)
            {
                ClearEffects();
            }
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            LevelPlan plan = runner.Plan;
            if (plan == null)
            {
                return;
            }

            float x = boat.X;
            float storm = 0f;
            float fog = 0f;
            bool eclipse = false;
            ActiveWeather = WeatherKind.None;

            int count = Mathf.Min(plan.Weather.Count, MaxEvents);
            for (int i = 0; i < count; i++)
            {
                WeatherEvent weather = plan.Weather[i];
                Phase phase = PhaseAt(weather, x);
                RaiseTransitions(i, weather, phase, x);

                if (phase != Phase.Active)
                {
                    continue;
                }

                ActiveWeather = weather.Kind;
                float ramp = RampAt(weather, x);
                switch (weather.Kind)
                {
                    case WeatherKind.Storm:
                        storm = Mathf.Max(storm, ramp);
                        break;
                    case WeatherKind.Fog:
                        fog = Mathf.Max(fog, ramp);
                        break;
                    case WeatherKind.Eclipse:
                        eclipse = true;
                        break;
                }
            }

            ApplyStorm(storm, levelTime);
            ApplyFog(fog);
            moon.ControlLocked = eclipse;
            moonView.Eclipsed = eclipse;
        }

        private static Phase PhaseAt(in WeatherEvent weather, float x)
        {
            if (x < weather.WarningX)
            {
                return Phase.Idle;
            }

            if (x < weather.StartX)
            {
                return Phase.Warning;
            }

            return x < weather.EndX ? Phase.Active : Phase.Ended;
        }

        private void RaiseTransitions(int index, in WeatherEvent weather, Phase phase, float x)
        {
            Phase previous = phases[index];
            if (previous == phase)
            {
                return;
            }

            phases[index] = phase;
            switch (phase)
            {
                case Phase.Warning:
                    float speed = Mathf.Max(0.01f, boat.Speed);
                    GameEvents.RaiseWeatherWarning(weather.Kind, (weather.StartX - x) / speed);
                    break;
                case Phase.Active:
                    GameEvents.RaiseWeatherStarted(weather.Kind);
                    break;
                case Phase.Ended:
                    GameEvents.RaiseWeatherEnded(weather.Kind);
                    break;
            }
        }

        private float RampAt(in WeatherEvent weather, float x)
        {
            float fadeIn = Mathf.Clamp01((x - weather.StartX) / config.RampDistance);
            float fadeOut = Mathf.Clamp01((weather.EndX - x) / config.RampDistance);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private void ApplyStorm(float intensity, float levelTime)
        {
            water.SetStormIntensity(intensity);
            if (intensity <= 0f)
            {
                tide.SetDisturbance(0f);
                return;
            }

            Vector2 f = config.StormFrequencies;
            float swell = Mathf.Sin(levelTime * f.x * 2f * Mathf.PI) + 0.5f * Mathf.Sin(levelTime * f.y * 2f * Mathf.PI + 1.3f);
            tide.SetDisturbance(swell * config.StormLevelAmplitude * intensity);
        }

        private void ApplyFog(float amount)
        {
            FogAmount = amount;
            bool enabled = amount > 0.001f;
            RenderSettings.fog = enabled;
            if (enabled)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = config.FogDensity * amount;
            }

            Shader.SetGlobalFloat(FogAmountId, amount);
        }

        private void ClearEffects()
        {
            ActiveWeather = WeatherKind.None;
            water.SetStormIntensity(0f);
            tide.SetDisturbance(0f);
            ApplyFog(0f);
            moon.ControlLocked = false;
            moonView.Eclipsed = false;
        }
    }
}
