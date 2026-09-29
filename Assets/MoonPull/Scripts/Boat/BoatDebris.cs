using MoonPull.Core.Pooling;
using UnityEngine;

namespace MoonPull.Boat
{
    /// <summary>
    /// Pre-fractured plank set. Pieces are reset to their authored pose on every spawn, so one pooled instance can
    /// shatter forever without Instantiate.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public sealed class BoatDebris : MonoBehaviour, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Rigidbody[] pieces = new Rigidbody[0];
        [SerializeField, Min(0f)] private float explosionForce = 6f;
        [SerializeField, Min(0f)] private float explosionRadius = 2f;
        [SerializeField, Min(0f)] private float upwardsModifier = 0.8f;
        [SerializeField, Min(0f)] private float randomTorque = 4f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;

        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private PooledObject pooled;
        private float age;

        private void Awake()
        {
            pooled = GetComponent<PooledObject>();
            block = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>(true);
            localPositions = new Vector3[pieces.Length];
            localRotations = new Quaternion[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                localPositions[i] = pieces[i].transform.localPosition;
                localRotations[i] = pieces[i].transform.localRotation;
            }
        }

        public void Explode(Color tint)
        {
            block.SetColor(BaseColorId, tint);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(block);
            }

            Vector3 origin = transform.position;
            for (int i = 0; i < pieces.Length; i++)
            {
                Rigidbody body = pieces[i];
                body.isKinematic = false;
                body.AddExplosionForce(explosionForce, origin, explosionRadius, upwardsModifier, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
            }
        }

        public void OnSpawned()
        {
            age = 0f;
            for (int i = 0; i < pieces.Length; i++)
            {
                Rigidbody body = pieces[i];
                body.isKinematic = true;
                body.transform.SetLocalPositionAndRotation(localPositions[i], localRotations[i]);
            }
        }

        public void OnDespawned()
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].isKinematic = true;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                pooled.Despawn();
            }
        }
    }
}
