using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Movement
{
    // Not within this file but tranform  is changed due to weird animation

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float sprintSpeed = 5f;
        [SerializeField] private float turnTime = 0.1f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;

        [Header("References")]
        [Tooltip("Leave empty!")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Leave empty!")]
        [SerializeField] private Animator animator;

        [Header("Animation")]
        [Tooltip("Smoothing on the Speed parameter, so blends aren't instant.")]
        [SerializeField] private float speedDampTime = 0.1f;

        [Header("Cursor")]
        [Tooltip("Hide and lock the cursor so mouse-look isn't interrupted.")]
        [SerializeField] private bool lockCursor = true;

        private CharacterController controller;
        private Vector3 velocity;
        private float turnSmoothVelocity;
        private bool wasGrounded = true;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            // The Animator lives on the knight model
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (lockCursor) SetCursorLocked(true);
        }

        void OnEnable()
        {
            GameHub.Subscribe<EntityDied>(OnEntityDied);
        }

        void OnDisable()
        {
            GameHub.Unsubscribe<EntityDied>(OnEntityDied);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            //HandleCursorToggle();

            // Before any Move() call this frame can reset it
            bool grounded = controller.isGrounded;

            Vector3 horizontal = Move();
            bool jumped = ApplyGravity(grounded);

            if (jumped)
                GameHub.Publish(new Jump());

            if (!wasGrounded && grounded)
                GameHub.Publish(new Land());

            wasGrounded = grounded;

            Vector3 motion = horizontal + Vector3.up * velocity.y;
            controller.Move(motion * Time.deltaTime);

            UpdateAnimator(horizontal, grounded, jumped);
        }

        void UpdateAnimator(Vector3 horizontal, bool grounded, bool jumped)
        {
            if (animator == null) return;
            float speed01 = sprintSpeed > 0f ? horizontal.magnitude / sprintSpeed : 0f;

            animator.SetFloat(AnimParams.Speed, speed01, speedDampTime, Time.deltaTime);

            // Have falling and jumping be diff
            animator.SetBool(AnimParams.Grounded, grounded && !jumped);

            if (jumped) animator.SetTrigger(AnimParams.Jump);
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

            Vector3 direction = (camForward * input.y + camRight * input.x);
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

        bool ApplyGravity(bool grounded)
        {
            // Small downward bias
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;

            bool jumped = grounded && JumpPressed();
            if (jumped)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            velocity.y += gravity * Time.deltaTime;
            return jumped;
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

        void OnEntityDied(EntityDied e)
        {
            if (e.Entity == gameObject)
                enabled = false;
        }

        // For when the game is finished
        //void HandleCursorToggle()
        //{
        //    if (!lockCursor) return;

        //    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        //        SetCursorLocked(false);
        //    else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        //        SetCursorLocked(true);
        //}
    }
}
