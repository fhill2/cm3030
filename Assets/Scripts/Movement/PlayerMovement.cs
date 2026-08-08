using UnityEngine;
using UnityEngine.InputSystem;
using Game.Health;
using Game.Shared;
using Game.Combat;

namespace Game.Movement
{
    /// <summary>
    /// Input-driven movement for the player. Inherits gravity, ground detection,
    /// animation updates, and death handling from <see cref="CharacterMotor"/>.
    ///
    /// Control scheme: the <see cref="FollowCamera"/> handles mouse-look (orbit
    /// + player aim). WASD strafes camera-relative (A/D strafe, W/S forward/back);
    /// Shift sprints; Space jumps. This class does NOT rotate the character —
    /// facing is driven by the camera.
    /// </summary>
    public class PlayerMovement : CharacterMotor
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float sprintSpeed = 5f;
        [Tooltip("Multiplier applied to movement speed (used by PlayerAttack to slow the player during a swing).")]
        public float speedScale = 1f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("References")]
        [Tooltip("Leave empty to auto-resolve to Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Cursor")]
        [Tooltip("Hide and lock the cursor so mouse-look isn't interrupted.")]
        [SerializeField] private bool lockCursor = true;

        [Header("Block")]
        [Tooltip("ShieldCollider component on the shield. Leave empty to auto-find.")]
        [SerializeField] private ShieldCollider shieldCollider;

        protected override void Awake()
        {
            base.Awake();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (animator != null && animator.isHuman)
                animator.stabilizeFeet = true;

            if (shieldCollider == null)
                shieldCollider = GetComponentInChildren<ShieldCollider>();

            if (lockCursor) SetCursorLocked(true);
        }

        protected override void Update()
        {
            bool grounded = controller.isGrounded;

            Vector2 input = ReadMoveInput();
            Vector3 horizontal = Move(input);      // strafe movement (no turning)
            ApplyGravity(grounded);

            bool jumped = grounded && JumpPressed();
            if (jumped)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            wasGrounded = grounded;

            MoveActor(horizontal);

            float speed01 = sprintSpeed > 0f ? horizontal.magnitude / sprintSpeed : 0f;
            UpdateAnimator(speed01, grounded, jumped);

            // Feed the directional locomotion blend.
            // Forward/backward takes priority — W+S+A/D plays forward/backward only (no strafe blend).
            // Pure A/D (no W/S) plays strafe animations.
            Vector2 animInput;
            if (Mathf.Abs(input.y) > 0.1f)
                animInput = new Vector2(0f, input.y);
            else
                animInput = new Vector2(input.x, 0f);
            SetMoveInput(animInput.x, animInput.y);

            // Sprint: Shift toggles between walk and run animations.
            if (animator != null) animator.SetBool(AnimParams.Sprint, IsSprinting());

            // Block stance: hold right-click to raise the shield.
            bool isBlocking = Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (animator != null) animator.SetBool(AnimParams.Block, isBlocking);
            if (shieldCollider != null) shieldCollider.IsBlocking = isBlocking;
        }

        // Camera-relative strafe movement. Does NOT rotate the character —
        // facing is driven by the FollowCamera (aim follows the view).
        Vector3 Move(Vector2 input)
        {
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight   = cameraTransform != null ? cameraTransform.right   : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 direction = camForward * input.y + camRight * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            float speed = (IsSprinting() ? sprintSpeed : walkSpeed) * speedScale;
            return direction * speed;
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
