using UnityEngine;
using UnityEngine.InputSystem;
using Game.Health;

namespace Game.Movement
{
    /// <summary>
    /// Input-driven movement for the player. Inherits gravity, ground detection,
    /// animation updates, and death handling from <see cref="CharacterMotor"/>.
    /// Adds WASD movement, sprint, jump, camera-relative turning, and cursor lock.
    /// </summary>
    public class PlayerMovement : CharacterMotor
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float sprintSpeed = 5f;
        [SerializeField] private float turnTime = 0.1f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("References")]
        [Tooltip("Leave empty to auto-resolve to Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Cursor")]
        [Tooltip("Hide and lock the cursor so mouse-look isn't interrupted.")]
        [SerializeField] private bool lockCursor = true;

        private float turnSmoothVelocity;

        protected override void Awake()
        {
            base.Awake();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (lockCursor) SetCursorLocked(true);
        }

        protected override void Update()
        {
            bool grounded = controller.isGrounded;

            Vector3 horizontal = Move();
            ApplyGravity(grounded);

            bool jumped = grounded && JumpPressed();
            if (jumped)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            wasGrounded = grounded;

            MoveActor(horizontal);

            float speed01 = sprintSpeed > 0f ? horizontal.magnitude / sprintSpeed : 0f;
            UpdateAnimator(speed01, grounded, jumped);
        }

        Vector3 Move()
        {
            Vector2 input = ReadMoveInput();

            // Flatten the camera
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 direction = camForward * input.y + camRight * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                    ref turnSmoothVelocity, turnTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                float speed = IsSprinting() ? sprintSpeed : walkSpeed;
                return direction * speed;
            }

            return Vector3.zero;
        }

        Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb == null) return input;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;

            return input;
        }

        bool IsSprinting()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        }

        bool JumpPressed()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        protected override void HandleDeath(DeathArgs e)
        {
            base.HandleDeath(e);
            if (e.Entity == gameObject)
                SetCursorLocked(false);
        }
    }
}
