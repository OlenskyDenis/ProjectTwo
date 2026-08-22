namespace ProjectTwo.Terrain.Presentation.Components
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Smooth free-flying camera controller supporting the new Unity Input System package.
    /// Controls:
    /// - WASD / Arrow Keys: Fly Forward, Left, Back, Right
    /// - Q / E or C / Space: Fly Down / Up
    /// - Right Mouse Button (hold + drag): Look around
    /// - Left Shift (hold): Speed boost (Sprint)
    /// - Mouse Scrollwheel: Adjust base fly speed
    /// </summary>
    public class FreeFlyCameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base movement speed in units per second.")]
        public float MoveSpeed = 60f;

        [Tooltip("Speed multiplier when holding Left Shift.")]
        public float BoostMultiplier = 3f;

        [Tooltip("Minimum and maximum move speed when scrolling wheel.")]
        public Vector2 SpeedRange = new Vector2(10f, 300f);

        [Header("Look Settings")]
        [Tooltip("Mouse look sensitivity.")]
        public float LookSensitivity = 0.15f;

        [Tooltip("Smoothing factor for look rotation.")]
        public float LookSmoothing = 15f;

        [Header("Terrain Alignment")]
        [Tooltip("Automatically position camera above the terrain on start.")]
        public bool AutoPositionAboveTerrain = true;

        [Tooltip("Height clearance above the terrain when auto-positioning.")]
        public float SpawnHeightOffset = 40f;

        private float _yaw;
        private float _pitch;
        private Vector3 _targetRotation;
        private Vector3 _currentRotation;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
            _targetRotation = new Vector3(_pitch, _yaw, 0f);
            _currentRotation = _targetRotation;

            if (AutoPositionAboveTerrain)
            {
                TerrainGenerator generator = FindAnyObjectByType<TerrainGenerator>();
                if (generator != null)
                {
                    float surfaceHeight = generator.GetHeight(transform.position.x, transform.position.z);
                    transform.position = new Vector3(transform.position.x, surfaceHeight + SpawnHeightOffset, transform.position.z);
                    _pitch = 20f;
                    transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }
                else if (transform.position.y < 30f)
                {
                    transform.position = new Vector3(transform.position.x, 60f, transform.position.z);
                    _pitch = 20f;
                    transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }
            }
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
            HandleSpeedAdjustment();
        }

        private void HandleMouseLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Vector2 delta = mouse.delta.ReadValue() * LookSensitivity;
                _yaw += delta.x;
                _pitch -= delta.y;
                _pitch = Mathf.Clamp(_pitch, -89f, 89f);

                _targetRotation = new Vector3(_pitch, _yaw, 0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            _currentRotation = Vector3.Lerp(_currentRotation, _targetRotation, Time.deltaTime * LookSmoothing);
            transform.rotation = Quaternion.Euler(_currentRotation);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float speed = MoveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                speed *= BoostMultiplier;
            }

            Vector3 moveDirection = Vector3.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveDirection += transform.forward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveDirection -= transform.forward;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveDirection += transform.right;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveDirection -= transform.right;
            if (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed) moveDirection += Vector3.up;
            if (keyboard.qKey.isPressed || keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed) moveDirection -= Vector3.up;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                transform.position += moveDirection.normalized * (speed * Time.deltaTime);
            }
        }

        private void HandleSpeedAdjustment()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float scrollDelta = Mathf.Sign(scroll) * 10f;
                MoveSpeed = Mathf.Clamp(MoveSpeed + scrollDelta, SpeedRange.x, SpeedRange.y);
            }
        }
    }
}
