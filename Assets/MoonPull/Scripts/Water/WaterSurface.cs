using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>
    /// Answers "how high is the water at X" for gameplay and pushes the identical parameters to the water shader
    /// as globals, so rocks and foam materials can react to sea level too.
    /// </summary>
    public sealed class WaterSurface : MonoBehaviour, ISimulationTickable
    {
        private static readonly int WaterLevelId = Shader.PropertyToID("_MP_WaterLevel");
        private static readonly int WaveTimeId = Shader.PropertyToID("_MP_WaveTime");
        private static readonly int WaveAmpId = Shader.PropertyToID("_MP_WaveAmp");
        private static readonly int WaveKId = Shader.PropertyToID("_MP_WaveK");
        private static readonly int WaveOmegaId = Shader.PropertyToID("_MP_WaveOmega");
        private static readonly int WaveDirXId = Shader.PropertyToID("_MP_WaveDirX");
        private static readonly int WaveDirZId = Shader.PropertyToID("_MP_WaveDirZ");
        private static readonly int WavePhaseId = Shader.PropertyToID("_MP_WavePhase");
        private static readonly int PulseId = Shader.PropertyToID("_MP_Pulse");
        private static readonly int FullMoonId = Shader.PropertyToID("_MP_FullMoon");
        private static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
        private static readonly int FoamColorId = Shader.PropertyToID("_FoamColor");

        [SerializeField] private WaterConfig config;
        [SerializeField] private TideModel tide;
        [Tooltip("Usually the gameplay camera. The water mesh follows it on X.")]
        [SerializeField] private Transform follow;
        [SerializeField] private Transform surfaceMesh;
        [SerializeField] private Renderer surfaceRenderer;

        private WaveComponent waveA;
        private WaveComponent waveB;
        private WaveComponent waveC;
        private WaterState state;
        private float stormIntensity;
        private float fullMoonGlow;
        private bool animateIdle = true;
        private MaterialPropertyBlock block;

        public float WaveTime => state.WaveTime;

        private float AmplitudeScale => Mathf.Lerp(1f, config.StormAmplitudeMultiplier, stormIntensity);

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            RebuildWaves();
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
        }

        /// <summary>World Y of the water surface at <paramref name="x"/> in the gameplay lane (z = 0).</summary>
        public float GetHeight(float x)
        {
            return tide.Level
                   + WaveMath.Height(x, 0f, state.WaveTime, waveA, waveB, waveC, AmplitudeScale)
                   + WaveMath.Pulse(x, state.PulseX, state.PulseAmplitude, config.PulseWidth, state.PulseAge, config.PulseDecay);
        }

        /// <summary>Raises a swell under a launched boat so the launch reads as the ocean throwing it.</summary>
        public void EmitPulse(float x, float amplitude)
        {
            state.PulseX = x;
            state.PulseAmplitude = amplitude;
            state.PulseAge = 0f;
        }

        public void SetStormIntensity(float intensity01) => stormIntensity = Mathf.Clamp01(intensity01);

        public void SetFullMoonGlow(float glow01) => fullMoonGlow = Mathf.Clamp01(glow01);

        public void ApplyPalette(Color shallow, Color deep, Color foam)
        {
            if (surfaceRenderer == null)
            {
                return;
            }

            surfaceRenderer.GetPropertyBlock(block);
            block.SetColor(ShallowColorId, shallow);
            block.SetColor(DeepColorId, deep);
            block.SetColor(FoamColorId, foam);
            surfaceRenderer.SetPropertyBlock(block);
        }

        public void ResetForLevel()
        {
            state = new WaterState { WaveTime = state.WaveTime };
            stormIntensity = 0f;
            fullMoonGlow = 0f;
        }

        public WaterState CaptureState() => state;

        public void RestoreState(WaterState restored) => state = restored;

        public void SimulationTick(float deltaTime, float levelTime)
        {
            Advance(deltaTime);
        }

        /// <summary>Re-reads wave settings. Call after editing WaterConfig at runtime.</summary>
        public void RebuildWaves()
        {
            waveA = new WaveComponent(config.WaveA);
            waveB = new WaveComponent(config.WaveB);
            waveC = new WaveComponent(config.WaveC);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            // The sea freezes on death for a dramatic beat and during rewind (snapshots drive it); menus keep it alive.
            animateIdle = to == GameState.Menu || to == GameState.Win || to == GameState.Shop || to == GameState.Boot || to == GameState.Consent;
        }

        private void Advance(float deltaTime)
        {
            state.WaveTime += deltaTime;
            state.PulseAge += deltaTime;
        }

        private void Update()
        {
            if (animateIdle)
            {
                Advance(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            PushShaderGlobals();
            FollowTarget();
        }

        private void PushShaderGlobals()
        {
            float scale = AmplitudeScale;
            Shader.SetGlobalFloat(WaterLevelId, tide.Level);
            Shader.SetGlobalFloat(WaveTimeId, state.WaveTime);
            Shader.SetGlobalVector(WaveAmpId, new Vector4(waveA.Amplitude * scale, waveB.Amplitude * scale, waveC.Amplitude * scale, 0f));
            Shader.SetGlobalVector(WaveKId, new Vector4(waveA.WaveNumber, waveB.WaveNumber, waveC.WaveNumber, 0f));
            Shader.SetGlobalVector(WaveOmegaId, new Vector4(waveA.AngularSpeed, waveB.AngularSpeed, waveC.AngularSpeed, 0f));
            Shader.SetGlobalVector(WaveDirXId, new Vector4(waveA.DirX, waveB.DirX, waveC.DirX, 0f));
            Shader.SetGlobalVector(WaveDirZId, new Vector4(waveA.DirZ, waveB.DirZ, waveC.DirZ, 0f));
            Shader.SetGlobalVector(WavePhaseId, new Vector4(waveA.Phase, waveB.Phase, waveC.Phase, 0f));
            float pulseAmplitude = state.PulseAmplitude * Mathf.Exp(-config.PulseDecay * state.PulseAge);
            Shader.SetGlobalVector(PulseId, new Vector4(state.PulseX, pulseAmplitude, 1f / config.PulseWidth, 0f));
            Shader.SetGlobalFloat(FullMoonId, fullMoonGlow);
        }

        private void FollowTarget()
        {
            if (follow == null || surfaceMesh == null)
            {
                return;
            }

            float snap = config.FollowSnap;
            Vector3 position = surfaceMesh.position;
            position.x = Mathf.Round(follow.position.x / snap) * snap;
            surfaceMesh.position = position;
        }
    }
}
