using MoonPull.Config;
using MoonPull.Core.Simulation;
using MoonPull.Core;
using UnityEngine.EventSystems;
using UnityEngine;

namespace MoonPull.Water
{
    /// <summary>
    /// Converts a vertical drag anywhere on screen into a normalized moon height (0..1) and velocity.
    /// Relative drag keeps the thumb off the moon and lets play resume seamlessly after a rewind.
    /// </summary>
    public sealed class MoonController : MonoBehaviour, ISimulationTickable
    {
        [SerializeField] private MoonConfig config;

        private MoonState state;
        private bool dragging;
        private bool ignoringPointer;
        private float lastPointerY;

        public float Height01 => state.Height01;
        public float TargetHeight01 => state.TargetHeight01;

        /// <summary>Smoothed normalized velocity (height units per second). Positive = pulling up.</summary>
        public float Velocity01 => state.Velocity01;

        /// <summary>When true (eclipse), input is read but ignored so unlocking never causes a jump.</summary>
        public bool ControlLocked { get; set; }

        /// <summary>True while a finger is down this frame.</summary>
        public bool IsDragging => dragging;

        public void ResetForLevel()
        {
            state = new MoonState
            {
                Height01 = config.StartHeight01,
                TargetHeight01 = config.StartHeight01,
                Velocity01 = 0f
            };
            dragging = false;
            ignoringPointer = false;
            ControlLocked = false;
        }

        public MoonState CaptureState() => state;

        public void RestoreState(MoonState restored)
        {
            state = restored;
            state.Velocity01 = 0f;
            // Force re-anchoring on the next touch so the current finger position maps to the restored height.
            dragging = false;
        }

        public void SimulationTick(float deltaTime, float levelTime)
        {
            ReadInput();

            float previous = state.Height01;
            state.Height01 = Mathf.Lerp(state.Height01, state.TargetHeight01, Spring.DampFactor(config.FollowSharpness, deltaTime));
            float rawVelocity = (state.Height01 - previous) / deltaTime;
            state.Velocity01 = Mathf.Lerp(state.Velocity01, rawVelocity, Spring.DampFactor(config.VelocitySharpness, deltaTime));
        }

        private void ReadInput()
        {
            if (!TryGetPointer(out float pointerY, out bool began, out int pointerId))
            {
                dragging = false;
                ignoringPointer = false;
                return;
            }

            // A touch that starts on a button (pause, etc.) never drives the tide until it is released.
            if (began && IsOverUi(pointerId))
            {
                ignoringPointer = true;
            }

            if (ignoringPointer)
            {
                return;
            }

            if (!dragging || began)
            {
                dragging = true;
                lastPointerY = pointerY;
                return;
            }

            float delta = (pointerY - lastPointerY) / (Screen.height * config.DragRangeScreenFraction);
            lastPointerY = pointerY;

            if (!ControlLocked)
            {
                state.TargetHeight01 = Mathf.Clamp01(state.TargetHeight01 + delta);
            }
        }

        private static bool TryGetPointer(out float y, out bool began, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                began = touch.phase == TouchPhase.Began;
                y = touch.position.y;
                pointerId = touch.fingerId;
                return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }

            pointerId = -1;
            if (Input.GetMouseButton(0))
            {
                began = Input.GetMouseButtonDown(0);
                y = Input.mousePosition.y;
                return true;
            }

            began = false;
            y = 0f;
            return false;
        }

        private static bool IsOverUi(int pointerId)
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
        }
    }
}
