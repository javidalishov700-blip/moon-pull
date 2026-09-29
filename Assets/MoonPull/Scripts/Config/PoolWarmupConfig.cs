using System;
using MoonPull.Core.Pooling;
using UnityEngine;

namespace MoonPull.Config
{
    /// <summary>Prefabs instantiated during Boot so no Instantiate happens mid-level.</summary>
    [CreateAssetMenu(fileName = "PoolWarmupConfig", menuName = "MoonPull/Config/Pool Warmup")]
    public sealed class PoolWarmupConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PooledObject Prefab;
            [Min(0)] public int Count;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public Entry[] Entries => entries;
    }
}
