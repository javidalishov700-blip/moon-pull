using MoonPull.Config;
using MoonPull.Core;
using MoonPull.Core.Pooling;
using UnityEngine;

namespace MoonPull.Boat
{
    /// <summary>Presents <see cref="BoatController"/> state: transform, model skin, and shatter on crash.</summary>
    public sealed class BoatView : MonoBehaviour
    {
        [SerializeField] private BoatController controller;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private PoolManager pools;
        [SerializeField] private BoatDebris debrisPrefab;

        private GameObject currentModel;
        private BoatDefinition currentDefinition;
        private BoatDebris activeDebris;

        private void OnEnable()
        {
            controller.Crashed += OnCrashed;
            GameEvents.LevelStartRequested += OnLevelStartRequested;
            GameEvents.RewindStarted += ClearDebris;
        }

        private void OnDisable()
        {
            controller.Crashed -= OnCrashed;
            GameEvents.LevelStartRequested -= OnLevelStartRequested;
            GameEvents.RewindStarted -= ClearDebris;
        }

        /// <summary>Swaps the visible boat model. Instantiates only when the boat actually changes.</summary>
        public void SetBoat(BoatDefinition definition)
        {
            if (definition == currentDefinition && currentModel != null)
            {
                return;
            }

            if (currentModel != null)
            {
                Destroy(currentModel);
            }

            currentDefinition = definition;
            currentModel = definition != null && definition.ModelPrefab != null
                ? Instantiate(definition.ModelPrefab, modelRoot, false)
                : null;
        }

        private void LateUpdate()
        {
            BoatState state = controller.State;
            transform.SetPositionAndRotation(new Vector3(state.X, state.Y, 0f), Quaternion.Euler(0f, 0f, state.Tilt));
            modelRoot.gameObject.SetActive(!state.Crashed);
        }

        private void OnCrashed(FailReason reason)
        {
            ClearDebris();
            activeDebris = pools.Spawn(debrisPrefab, modelRoot.position, modelRoot.rotation);
            Color tint = currentDefinition != null ? currentDefinition.DebrisTint : Color.white;
            activeDebris.Explode(tint);
        }

        private void OnLevelStartRequested(LevelStartArgs args) => ClearDebris();

        private void ClearDebris()
        {
            if (activeDebris != null && activeDebris.gameObject.activeSelf)
            {
                activeDebris.GetComponent<PooledObject>().Despawn();
            }

            activeDebris = null;
        }
    }
}
