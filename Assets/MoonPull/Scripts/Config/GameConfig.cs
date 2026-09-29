using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MoonPull/Config/Game")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Runtime")]
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
        [Tooltip("Frame hitches are clamped to this step so the boat cannot tunnel through obstacles.")]
        [SerializeField, Range(0.01f, 0.1f)] private float maxSimulationDeltaTime = 1f / 30f;
        [SerializeField] private bool multiTouchEnabled;

        [Header("Time scale")]
        [Tooltip("Lowest timeScale used for fixedDeltaTime scaling during slow motion.")]
        [SerializeField, Range(0.01f, 1f)] private float minFixedStepScale = 0.05f;

        public int TargetFrameRate => targetFrameRate;
        public float MaxSimulationDeltaTime => maxSimulationDeltaTime;
        public bool MultiTouchEnabled => multiTouchEnabled;
        public float MinFixedStepScale => minFixedStepScale;
    }
}
