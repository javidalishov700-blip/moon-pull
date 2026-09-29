using System.Collections.Generic;
using UnityEngine;

namespace MoonPull.Core.Pooling
{
    /// <summary>
    /// Stack-based pool for one prefab. Tracks active instances with swap-remove indices so DespawnAll is O(n)
    /// and single despawns are O(1) without allocations.
    /// </summary>
    public sealed class PrefabPool
    {
        private readonly PooledObject prefab;
        private readonly Transform root;
        private readonly Stack<PooledObject> free;
        private readonly List<PooledObject> active;

        public PrefabPool(PooledObject prefab, Transform root, int prewarm)
        {
            this.prefab = prefab;
            this.root = root;
            free = new Stack<PooledObject>(Mathf.Max(prewarm, 4));
            active = new List<PooledObject>(Mathf.Max(prewarm, 4));
            Prewarm(prewarm);
        }

        public int ActiveCount => active.Count;
        public int FreeCount => free.Count;

        public void Prewarm(int count)
        {
            for (int i = free.Count + active.Count; i < count; i++)
            {
                free.Push(CreateInstance());
            }
        }

        public PooledObject Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            PooledObject instance = free.Count > 0 ? free.Pop() : CreateInstance();
            Transform t = instance.transform;
            t.SetParent(parent != null ? parent : root, false);
            t.SetPositionAndRotation(position, rotation);
            instance.ActiveIndex = active.Count;
            active.Add(instance);
            instance.gameObject.SetActive(true);
            instance.NotifySpawned();
            return instance;
        }

        public void Despawn(PooledObject instance)
        {
            int index = instance.ActiveIndex;
            if (index < 0 || index >= active.Count || active[index] != instance)
            {
                return;
            }

            int last = active.Count - 1;
            PooledObject moved = active[last];
            active[index] = moved;
            moved.ActiveIndex = index;
            active.RemoveAt(last);
            instance.ActiveIndex = -1;

            instance.NotifyDespawned();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(root, false);
            free.Push(instance);
        }

        public void DespawnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Despawn(active[i]);
            }
        }

        private PooledObject CreateInstance()
        {
            PooledObject instance = Object.Instantiate(prefab, root);
            instance.name = prefab.name;
            instance.Owner = this;
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
