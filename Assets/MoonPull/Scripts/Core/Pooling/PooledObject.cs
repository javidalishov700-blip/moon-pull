using UnityEngine;

namespace MoonPull.Core.Pooling
{
    /// <summary>Marks a prefab as poolable and remembers which pool owns each instance.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        private IPoolable[] poolables;

        internal PrefabPool Owner { get; set; }
        internal int ActiveIndex { get; set; } = -1;

        public bool IsSpawned => ActiveIndex >= 0;

        private void Awake()
        {
            CachePoolables();
        }

        /// <summary>Returns this instance to its pool, or destroys it if it was never pooled.</summary>
        public void Despawn()
        {
            if (Owner != null)
            {
                Owner.Despawn(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        internal void NotifySpawned()
        {
            CachePoolables();
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned();
            }
        }

        internal void NotifyDespawned()
        {
            CachePoolables();
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }
        }

        private void CachePoolables()
        {
            if (poolables == null)
            {
                poolables = GetComponentsInChildren<IPoolable>(true);
            }
        }
    }
}
