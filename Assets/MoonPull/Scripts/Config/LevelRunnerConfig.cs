using UnityEngine;

namespace MoonPull.Config
{
    [CreateAssetMenu(fileName = "LevelRunnerConfig", menuName = "MoonPull/Config/Level Runner")]
    public sealed class LevelRunnerConfig : ScriptableObject
    {
        [Tooltip("Placements spawn this far ahead of the boat. Must exceed the visible width in portrait.")]
        [SerializeField, Min(1f)] private float spawnAhead = 26f;
        [SerializeField, Min(1f)] private float despawnBehind = 10f;

        public float SpawnAhead => spawnAhead;
        public float DespawnBehind => despawnBehind;
    }
}
