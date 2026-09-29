using System.Collections.Generic;
using MoonPull.Config;
using UnityEngine;

namespace MoonPull.Core.Pooling
{
    /// <summary>Scene-level pool registry keyed by prefab. Prewarms from <see cref="PoolWarmupConfig"/>.</summary>
    public sealed class PoolManager : MonoBehaviour
    {
        [SerializeField] private PoolWarmupConfig warmup;

        private readonly Dictionary<PooledObject, PrefabPool> pools = new Dictionary<PooledObject, PrefabPool>();
        private readonly Dictionary<Component, PooledObject> prefabKeys = new Dictionary<Component, PooledObject>();

        private void Awake()
        {
            if (warmup == null)
            {
                return;
            }

            PoolWarmupConfig.Entry[] entries = warmup.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Prefab != null)
                {
                    GetPool(entries[i].Prefab).Prewarm(entries[i].Count);
                }
            }
        }

        /// <summary>Spawns an instance of <paramref name="prefab"/> and returns its component of the same type.</summary>
        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            PooledObject key = GetKey(prefab);
            PooledObject instance = GetPool(key).Spawn(position, rotation, parent);
            return typeof(T) == typeof(PooledObject) ? instance as T : instance.GetComponent<T>();
        }

        /// <summary>Returns every active instance of every pool. Used when a level is torn down or rewound.</summary>
        public void DespawnAll()
        {
            foreach (KeyValuePair<PooledObject, PrefabPool> pair in pools)
            {
                pair.Value.DespawnAll();
            }
        }

        private PooledObject GetKey<T>(T prefab) where T : Component
        {
            if (prefabKeys.TryGetValue(prefab, out PooledObject key))
            {
                return key;
            }

            key = prefab as PooledObject;
            if (key == null)
            {
                key = prefab.GetComponent<PooledObject>();
            }

            if (key == null)
            {
                throw new MissingComponentException($"Prefab {prefab.name} needs a PooledObject component to be pooled.");
            }

            prefabKeys.Add(prefab, key);
            return key;
        }

        private PrefabPool GetPool(PooledObject prefab)
        {
            if (!pools.TryGetValue(prefab, out PrefabPool pool))
            {
                var root = new GameObject($"Pool_{prefab.name}").transform;
                root.SetParent(transform, false);
                pool = new PrefabPool(prefab, root, 0);
                pools.Add(prefab, pool);
            }

            return pool;
        }
    }
}
