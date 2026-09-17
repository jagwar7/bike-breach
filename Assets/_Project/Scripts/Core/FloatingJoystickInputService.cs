using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Runtime.Core.Input
{
    public class FloatingJoystickInputService : MonoBehaviour, IInputService
    {
        [Header("Joystick Settings")]
        [Tooltip("Distance in pixels from touch origin to register 100% input.")]
        [SerializeField] private float joystickRadiusPixels = 100f;

        [Tooltip("Deadzone radius in pixels to ignore accidental finger micro-twitches.")]
        [SerializeField] private float deadzonePixels = 10f;

        public Vector2 MoveInput { get; private set; }
        public bool IsTouching { get; private set; }
        public bool HasTapOccurred { get; private set; }
        public Vector2 TapScreenPosition { get; private set; }

        private Vector2 _touchStartPos;
        private bool _isDragging;
        

        private void Update()
        {
            UpdateInput();
        }

        public void UpdateInput()
        {
            HasTapOccurred = false;

            var pointer = Pointer.current;
            if (pointer == null) {
                Debug.Log("No point value");
                return;
            }
          

            bool wasPressedThisFrame = pointer.press.wasPressedThisFrame;
            bool isHeld = pointer.press.isPressed;
            bool wasReleasedThisFrame = pointer.press.wasReleasedThisFrame;
            Vector2 currentPointerPos = pointer.position.ReadValue();

            if (wasPressedThisFrame)
            {
                _touchStartPos = currentPointerPos;
                _isDragging = true;
                IsTouching = true;
                HasTapOccurred = true;
                TapScreenPosition = currentPointerPos;
                MoveInput = Vector2.zero;
            }
            else if (isHeld && _isDragging)
            {
                Vector2 rawDelta = currentPointerPos - _touchStartPos;
                float distance = rawDelta.magnitude;

                if (distance > deadzonePixels)
                {
                    Vector2 clampedDir = rawDelta.normalized;
                    float clampedMagnitude = Mathf.Clamp01((distance - deadzonePixels) / (joystickRadiusPixels - deadzonePixels));
                    MoveInput = clampedDir * clampedMagnitude;
                }
                else
                {
                    MoveInput = Vector2.zero;
                }

                TapScreenPosition = currentPointerPos;
            }
            else if (wasReleasedThisFrame)
            {
                _isDragging = false;
                IsTouching = false;
                MoveInput = Vector2.zero;
            }
        }

        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (_isDragging)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(10, 10, 300, 25), $"Joystick Move: {MoveInput}");
            }
        }
        #endif
    }
}