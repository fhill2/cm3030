using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
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

        // Stamina gate. Optional — without it everything behaves as before.
        private StaminaSystem stamina;

        /// <summary>True while the player is holding block. Exposed so other
        /// systems (stamina, animation) read one source instead of each
        /// polling the mouse themselves.</summary>
        public bool IsBlocking { get; private set; }

        /// <summary>True while the player is actually sprinting, i.e. holding
        /// Shift AND able to pay for it.</summary>
        public bool IsSprintingNow { get; private set; }

        /// <summary>
        /// False while the game loop is in a state the player shouldn't be
        /// driving the character — the start menu, the shop
        /// </summary>
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
            // Resolve the ShieldCollider AFTER Equipment.Start() has loaded the shield.
            // If we do this in Awake, the shield doesn't exist yet (Equipment loads it in Start).
            if (shieldCollider == null)
                shieldCollider = GetComponentInChildren<ShieldCollider>();
        }

        // base.OnEnable must be called: CharacterMotor subscribes OnDeath there,
        // and losing it would stop the player dying at all.
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

            // One owner for the cursor. Locked only while the player is actually
            // driving the character; free in the menu, the shop and after death
            SetCursorLocked(ControlEnabled);
        }

        // Movement stays live through WaveComplete — that's the brief lull
        // between waves, and freezing the player there feels like a hitch.
        private static bool AllowsControl(GameStateId state)
        {
            return state == GameStateId.WaveActive || state == GameStateId.WaveComplete;
        }

        protected override void Update()
        {
            if (!ControlEnabled)
            {
                // Menu, shop or game over: no input, but gravity keeps running
                // so the character rests on the ground instead of hovering.
                SettleUncontrolled();
                return;
            }

            if (isDead)
            {
                // Dead players don't walk, sprint, jump or block. Gravity still
                // runs so the body settles rather than hanging where it died.
                SettleUncontrolled();
                return;
            }

                        // Shop and menu states freeze the knight. Gravity still runs below
            // so he settles on the ground rather than hanging mid-step.
            if (PlayerInputLock.InputLocked)
            {
                IsSprintingNow = false;
                IsBlocking = false;
                if (animator != null)
                {
                    animator.SetBool(AnimParams.Sprint, false);
                    animator.SetBool(AnimParams.Block, false);
                }
                if (shieldCollider != null) shieldCollider.IsBlocking = false;

                SetMoveInput(0f, 0f);
                ApplyGravity(controller.isGrounded);
                MoveActor(Vector3.zero);
                UpdateAnimator(0f, controller.isGrounded, false);
                return;
            }

            bool grounded = controller.isGrounded;

            // Work out sprint first, since Move() needs to know the speed and
            // sprinting costs stamina every frame it's held.
            IsSprintingNow = ResolveSprint();

            Vector2 input = ReadMoveInput();
            Vector3 horizontal = Move(input);      // strafe movement (no turning)
            ApplyGravity(grounded);

            // Jump costs a chunk up front, so a stunned or exhausted player
            // stays on the ground.
            bool jumped = grounded && JumpPressed() && CanAffordJump();
            if (jumped)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            wasGrounded = grounded;

            MoveActor(horizontal);

            float speed01 = sprintSpeed > 0f ? horizontal.magnitude / sprintSpeed : 0f;
            UpdateAnimator(speed01, grounded, jumped);

            // Feed the directional locomotion blend.
            // Sprinting: forward/backward wins — W+A/D plays forward/backward while the
            // camera turns the character slightly (FollowCamera diagonalTurnAngle).
            // Walking: strafe wins — W+A/D plays RunLeft/RunRight only.
            Vector2 animInput;
            if (IsSprintingNow)
            {
                if (Mathf.Abs(input.y) > 0.1f)
                    animInput = new Vector2(0f, input.y);
                else
                    animInput = new Vector2(input.x, 0f);
            }
            else if (Mathf.Abs(input.x) > 0.1f)
                animInput = new Vector2(input.x, 0f);
            else
                animInput = new Vector2(0f, input.y);
            SetMoveInput(animInput.x, animInput.y);

            // Sprint: Shift toggles between walk and run animations.
            if (animator != null) animator.SetBool(AnimParams.Sprint, IsSprintingNow);

            // Block stance: hold right-click to raise the shield.
            IsBlocking = ResolveBlock();
            if (animator != null) animator.SetBool(AnimParams.Block, IsBlocking);
            if (shieldCollider != null) shieldCollider.IsBlocking = IsBlocking;
        }

        // Sprinting is held down, so it drains continuously. The drain returns
        // false the moment stamina runs out, which drops us back to a walk in
        // the same frame.
        private bool ResolveSprint()
        {
            if (!SprintHeld()) return false;
            if (!MovingOnFoot()) return false;   // no drain while standing still
            if (stamina == null) return true;

            return stamina.DrainSprint(Time.deltaTime);
        }

        // The shield is free to hold — blocking never touches stamina.
        private bool ResolveBlock()
        {
            if (Mouse.current == null) return false;
            return Mouse.current.rightButton.isPressed;
        }

        private bool CanAffordJump()
        {
            if (stamina == null) return true;
            return stamina.TrySpendJump();
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

            float speed = (IsSprintingNow ? sprintSpeed : walkSpeed) * speedScale;
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

        bool SprintHeld()
        {
            return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        }

        // Sprinting only counts when there's actual movement input, so standing
        // on the spot holding Shift doesn't burn the bar.
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

            // Clear the guard explicitly. Update stops running from here, so a
            // player who died holding right-click would otherwise keep the block
            // animation and an armed shield collider on the corpse forever.
            IsBlocking = false;
            IsSprintingNow = false;
            if (animator != null) animator.SetBool(AnimParams.Block, false);
            if (shieldCollider != null) shieldCollider.IsBlocking = false;
        }
    }
}