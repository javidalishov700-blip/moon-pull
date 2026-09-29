using UnityEngine;

namespace MoonPull.Gameplay.Visuals
{
    /// <summary>Spins and bobs a pickup so it reads as collectible at a glance.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 120f, 0f);
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
        [SerializeField, Min(0.01f)] private float bobFrequency = 1.2f;

        private Vector3 basePosition;
        private float phase;

        private void OnEnable()
        {
            basePosition = transform.localPosition;
            phase = Random.value * 10f;
        }

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
            Vector3 p = basePosition;
            p.y += Mathf.Sin((Time.time + phase) * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
            transform.localPosition = p;
        }
    }
}
