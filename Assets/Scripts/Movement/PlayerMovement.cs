using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Health;
using Game.Shared;
using Game.Combat;

namespace Game.Movement
{
    // Input-driven movement for the player. Gravity, ground detection,
    // animation and death handling are inherited. 
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

        private StaminaSystem stamina;

        // Exposed so stamina and animation read one source instead of each
        // polling the mouse.
        public bool IsBlocking { get; private set; }

        // Holding Shift and able to pay for it.
        public bool IsSprintingNow { get; private set; }

        // False in the states the player shouldn't be driving the character:
        // the start menu, the shop, game over.
        public bool ControlEnabled { get; private set; } = true;

        protected override void Awake()
        {
            base.Awake();

            stamina = GetComponent<StaminaSystem>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (animator != null && animator.isHuman)
                animator.stabilizeFeet = true;

            if (lockCursor) SetCursorLocked(true);
        }

        protected void Start()
        {
            // In Start, not Awake, because Equipment loads the shield in Start.
            if (shieldCollider == null)
                shieldCollider = GetComponentInChildren<ShieldCollider>();
        }

        // base.OnEnable must be called: CharacterMotor subscribes OnDeath
        // there, and losing it would stop the player dying at all.
        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameStateChangedArgs e)
        {
            ControlEnabled = AllowsControl(e.Current);

            // One owner for the cursor. Locked only while the player is driving
            // the character, free in the menu, the shop and after death.
            SetCursorLocked(ControlEnabled);
        }

        // Movement stays live through WaveComplete, the brief lull between
        // waves, since freezing the player there feels like a hitch.
        private static bool AllowsControl(GameStateId state)
        {
            return state == GameStateId.WaveActive || state == GameStateId.WaveComplete;
        }

        protected override void Update()
        {
            if (!ControlEnabled)
            {
                // No input, but gravity keeps running so the character rests on the ground. 
                if (!isDead) ClearLocomotion();

                SettleUncontrolled();
                return;
            }

            if (isDead)
            {
                SettleUncontrolled();
                return;
            }

            bool grounded = controller.isGrounded;

            // Sprint first, since Move needs the speed and sprinting costs
            // stamina every frame it's held.
            IsSprintingNow = ResolveSprint();

            Vector2 input = ReadMoveInput();
            Vector3 horizontal = Move(input);
            ApplyGravity(grounded);

            // Jump costs a chunk up front, so a stunned or exhausted player
            // stays on the ground.
            bool jumped = grounded && JumpPressed() && CanAffordJump();
            if (jumped)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            wasGrounded = grounded;

            MoveActor(horizontal);

            float normalisedSpeed = sprintSpeed > 0f ? horizontal.magnitude / sprintSpeed : 0f;
            // Grace-filtered so descending stairs doesn't fire the fall
            // animation on every step. 
            UpdateAnimator(normalisedSpeed, GroundedForAnimation(grounded, jumped), jumped);

            // Sprinting, forward and backward win, so W+A plays forward while
            // the camera turns the character slightly. 
            Vector2 blendInput;
            if (IsSprintingNow)
            {
                if (Mathf.Abs(input.y) > 0.1f)
                    blendInput = new Vector2(0f, input.y);
                else
                    blendInput = new Vector2(input.x, 0f);
            }
            else if (Mathf.Abs(input.x) > 0.1f)
                blendInput = new Vector2(input.x, 0f);
            else
                blendInput = new Vector2(0f, input.y);
            SetMoveInput(blendInput.x, blendInput.y);

            if (animator != null) animator.SetBool(AnimParams.Sprint, IsSprintingNow);

            IsBlocking = ResolveBlock();
            if (animator != null) animator.SetBool(AnimParams.Block, IsBlocking);
            if (shieldCollider != null) shieldCollider.IsBlocking = IsBlocking;
        }

        // The Set and Update calls damp toward zero rather than snapping, so
        // the run blends out over a few frames instead of cutting mid-stride.
        private void ClearLocomotion()
        {
            IsSprintingNow = false;
            IsBlocking = false;

            SetMoveInput(0f, 0f);
            UpdateAnimator(0f, controller.isGrounded, false);

            if (animator != null)
            {
                animator.SetBool(AnimParams.Sprint, false);
                animator.SetBool(AnimParams.Block, false);
            }

            if (shieldCollider != null) shieldCollider.IsBlocking = false;
        }

        // Held down, so it drains continuously. The drain returns false the
        // moment stamina runs out, dropping back to a walk in the same frame.
        private bool ResolveSprint()
        {
            if (!SprintHeld()) return false;
            if (!MovingOnFoot()) return false;
            if (stamina == null) return true;

            return stamina.DrainSprint(Time.deltaTime);
        }

        // Same as sprinting: run out of stamina and the shield drops.
        private bool ResolveBlock()
        {
            if (Mouse.current == null) return false;
            if (!Mouse.current.rightButton.isPressed) return false;
            if (stamina == null) return true;

            return stamina.DrainBlock(Time.deltaTime);
        }

        private bool CanAffordJump()
        {
            if (stamina == null) return true;
            return stamina.TrySpendJump();
        }

        // Camera-relative strafe. Doesn't rotate the character.
        Vector3 Move(Vector2 input)
        {
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 direction = cameraForward * input.y + cameraRight * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            float speed = (IsSprintingNow ? sprintSpeed : walkSpeed) * speedScale;
            return direction * speed;
        }

        Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return input;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

            return input;
        }

        bool SprintHeld()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        }

        // Standing on the spot holding Shift shouldn't burn the bar.
        bool MovingOnFoot()
        {
            return ReadMoveInput().sqrMagnitude > 0.01f;
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
            if (e.Entity != gameObject) return;

            SetCursorLocked(false);

            // Update stops running from here, so a player who died holding
            // right-click would otherwise keep the block animation and an armed shield collider on the corpse forever.
            IsBlocking = false;
            IsSprintingNow = false;
            if (animator != null) animator.SetBool(AnimParams.Block, false);
            if (shieldCollider != null) shieldCollider.IsBlocking = false;
        }
    }
}