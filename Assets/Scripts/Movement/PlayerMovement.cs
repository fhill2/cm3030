using UnityEngine;
using UnityEngine.InputSystem;
using Game.Health;
using Game.Shared;

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

        [Header("Block Aim")]
        [Tooltip("Constant base lean (degrees) always applied to the chest while blocking. Set 0 to disable.")]
        [SerializeField] private float blockBaseLean = 0f;
        [Tooltip("Max degrees the camera pitch adds on top of the base lean. Set 0 to disable.")]
        [SerializeField] private float blockAimPitchRange = 0f;
        [Tooltip("Max degrees the torso yaws (left/right) while blocking. Set 0 to disable.")]
        [SerializeField] private float blockAimYawRange = 0f;
        [Tooltip("How quickly the torso follows the camera aim (higher = snappier).")]
        [SerializeField] private float blockAimSpeed = 0f;
        [Tooltip("Mouse sensitivity for left/right block aiming (degrees per pixel).")]
        [SerializeField] private float blockYawSensitivity = 0f;
        [Tooltip("Constant yaw offset to center the guard (degrees). Set 0 for no offset.")]
        [SerializeField] private float blockCenterOffset = 0f;

        private Transform chestBone;
        private bool isBlocking;
        private float blockTorsoPitch;
        private float blockTorsoYaw;

        protected override void Awake()
        {
            base.Awake();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (animator != null && animator.isHuman)
            {
                chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                animator.stabilizeFeet = true;
            }

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

            // Feed the 2D directional locomotion blend (camera-relative input).
            Vector2 blend = input.sqrMagnitude > 1f ? input.normalized : input;
            SetMoveInput(blend.x, blend.y);

            // Block stance: hold right-click to raise the shield.
            isBlocking = Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (animator != null) animator.SetBool(AnimParams.Block, isBlocking);

            // While blocking, capture mouse X delta for independent torso yaw
            // (the camera rotates the whole body, so this adds a LOCAL torso twist
            // on top — lets the player angle the shield left/right without turning).
            if (isBlocking && Mouse.current != null)
            {
                float mouseX = Mouse.current.delta.x.ReadValue();
                blockTorsoYaw = Mathf.Clamp(blockTorsoYaw + mouseX * blockYawSensitivity, -blockAimYawRange, blockAimYawRange);
            }
        }

        /// <summary>
        /// Procedural block aim: ONLY while blocking — the chest bone pitches/yaws
        /// to follow the camera and mouse. When not blocking, nothing runs.
        /// </summary>
        private void LateUpdate()
        {
            if (!isBlocking)
            {
                blockTorsoPitch = 0f;
                blockTorsoYaw = 0f;
                return;
            }

            if (chestBone == null || cameraTransform == null) return;

            // Pitch: always apply blockAimPitchRange as a base lean (reversed),
            // then camera pitch adds variation on top.
            float camPitch = cameraTransform.eulerAngles.x;
            if (camPitch > 180f) camPitch -= 360f;
            float targetPitch = blockBaseLean + Mathf.Clamp(-camPitch, -blockAimPitchRange, blockAimPitchRange);
            blockTorsoPitch = Mathf.Lerp(blockTorsoPitch, targetPitch, blockAimSpeed * Time.deltaTime);

            // Apply torso aim (pitch + yaw + center offset) on top of the animation
            chestBone.localRotation *= Quaternion.Euler(blockTorsoPitch, blockTorsoYaw + blockCenterOffset, 0f);
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
