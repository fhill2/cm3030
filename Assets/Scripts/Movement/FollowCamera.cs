using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Health;

namespace Game.Movement
{
    // Third-person orbit camera. The mouse looks around the player, and the
    // player is rotated to match so the aim follows the view.
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Leave empty to auto-find the Player by tag.")]
        [SerializeField] private Transform target;
        [SerializeField] private float targetHeight = 1.5f;

        [Header("Orbit")]
        [SerializeField] private float distance = 4f;
        [SerializeField] private float sensitivity = 0.1f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Shoulder")]
        [Tooltip("Shifts the camera to the player's right so the body doesn't sit under the crosshair. Negative for the left shoulder.")]
        [SerializeField] private float shoulderOffset = 0.6f;
        [Tooltip("Extra height on top of Target Height, to look over the shoulder rather than through it.")]
        [SerializeField] private float shoulderHeight = 0.2f;

        [Header("Collision")]
        [SerializeField] private bool collideWithGeometry = true;
        [Tooltip("Set this to the Environment layer only.")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float minDistance = 1f;

        [Header("Aim Follow")]
        [Tooltip("Player yaws to match the camera heading.")]
        [SerializeField] private bool rotateTarget = true;

        [Header("Diagonal Turn")]
        [Tooltip("Degrees the player turns when sprinting W+A or W+D.")]
        [SerializeField] private float diagonalTurnAngle = 35f;
        [SerializeField] private float turnSmooth = 8f;

        [Header("First Person")]
        [SerializeField] private bool firstPerson;
        [SerializeField] private float firstPersonHeight = 1.7f;

        [Header("Death View")]
        [SerializeField] private bool deathView = true;
        [Tooltip("90 looks straight down. 80 keeps some horizon so the castle still reads.")]
        [SerializeField] private float deathPitch = 80f;
        [SerializeField] private float deathDistance = 200f;
        [Tooltip("Keep PlayerUI's fadeDuration in step with this.")]
        [SerializeField] private float deathTransitionTime = 3f;

        [Header("Start Screen")]
        [Tooltip("Transform framing the opening shot. Position and rotate it in the Scene view; the camera copies its pose while the game sits in the Menu state. Leave empty to skip the start-screen shot entirely.")]
        [SerializeField] private Transform menuCameraAnchor;
        [Tooltip("Seconds the zoom from the opening shot down to the player takes.")]
        [SerializeField] private float introDuration = 2.5f;

        private enum CameraMode { Menu, Intro, Gameplay, Death }

        private CameraMode mode = CameraMode.Gameplay;

        private float yaw;
        private float pitch = 15f;
        private float turnOffset;

        private float deathBlend;
        private Vector3 deathStartPos;
        private Quaternion deathStartRot;

        private float introBlend;
        private Vector3 introStartPos;
        private Quaternion introStartRot;

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            // Death outranks everything.
            if (mode == CameraMode.Death) return;
            if (menuCameraAnchor == null) return;

            if (e.Current == GameStateId.Menu)
            {
                mode = CameraMode.Menu;
            }
            else if (mode == CameraMode.Menu)
            {
                StartIntro();
            }
        }

        void StartIntro()
        {
            introBlend = 0f;
            introStartPos = transform.position;
            introStartRot = transform.rotation;

            // Seed the orbit angles to the pose the blend lands on.
            if (target != null) yaw = target.eulerAngles.y;

            mode = CameraMode.Intro;
        }

        void HandleDeath(DeathArgs e)
        {
            if (!deathView || mode == CameraMode.Death) return;
            // An enemy dying must not yank the camera skyward.
            if (!IsFollowedTarget(e.Entity)) return;

            mode = CameraMode.Death;
            deathBlend = 0f;
            deathStartPos = transform.position;
            deathStartRot = transform.rotation;
        }

        // The health component sits on a child, not on the follow target, so
        // compare against the whole branch.
        bool IsFollowedTarget(GameObject entity)
        {
            if (target == null || entity == null) return false;
            Transform entityTransform = entity.transform;
            return entityTransform == target
                || target.IsChildOf(entityTransform)
                || entityTransform.IsChildOf(target);
        }

        void Awake()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            // Seed from the current orientation so it doesn't snap on frame one.
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Each non-gameplay mode returns early, which also suppresses the
            // mouse-look and the rotateTarget block below. Otherwise the
            // character would keep spinning to follow the mouse on the start
            // screen or while lying dead.
            switch (mode)
            {
                case CameraMode.Menu:
                    UpdateMenuView();
                    return;
                case CameraMode.Intro:
                    UpdateIntroBlend();
                    return;
                case CameraMode.Death:
                    UpdateDeathView();
                    return;
            }

            // Both stop while the shop is open, otherwise clicking through the
            // market spins the knight.
            bool locked = PlayerInputLock.InputLocked;

            if (!locked && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            GameplayPose(out Vector3 position, out Quaternion rotation);
            transform.position = position;
            transform.rotation = rotation;

            if (rotateTarget && !locked)
            {
                float targetOffset = 0f;
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.leftShiftKey.isPressed)
                {
                    bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                    bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                    bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

                    if (forward && left && !right) targetOffset = -diagonalTurnAngle;
                    else if (forward && right && !left) targetOffset = diagonalTurnAngle;
                }
                turnOffset = Mathf.Lerp(turnOffset, targetOffset, turnSmooth * Time.deltaTime);
                target.rotation = Quaternion.Euler(0f, yaw + turnOffset, 0f);
            }
        }

        // Shared by the gameplay path and the intro blend, so the fly-in lands
        // exactly where gameplay picks up.
        void GameplayPose(out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (firstPerson)
            {
                position = target.position + Vector3.up * firstPersonHeight;
            }
            else
            {
                // Focus point pushed right and up, so the knight sits to the
                // left of frame and the crosshair looks past him.
                Vector3 focus = target.position
                                + Vector3.up * (targetHeight + shoulderHeight)
                                + rotation * Vector3.right * shoulderOffset;

                Vector3 back = rotation * Vector3.back;

                position = focus + back * ArmLength(focus, back);
            }
        }

        // Shortens the arm when a wall is in the way, so indoors the camera
        // stays in the room. 
        float ArmLength(Vector3 focus, Vector3 direction)
        {
            if (!collideWithGeometry) return distance;

            if (Physics.SphereCast(focus, collisionRadius, direction,
                    out RaycastHit hit, distance,
                    collisionMask, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Max(minDistance, hit.distance);
            }

            return distance;
        }

        void UpdateMenuView()
        {
            if (menuCameraAnchor == null) return;
            transform.position = menuCameraAnchor.position;
            transform.rotation = menuCameraAnchor.rotation;
        }

        void UpdateIntroBlend()
        {
            introBlend = Mathf.Clamp01(
                introBlend + Time.deltaTime / Mathf.Max(0.01f, introDuration));

            float blend = Mathf.SmoothStep(0f, 1f, introBlend);

            // Recomputed every frame so the shot still lands correctly if the
            // player settles onto the ground during the fly-in.
            GameplayPose(out Vector3 position, out Quaternion rotation);

            transform.position = Vector3.Lerp(introStartPos, position, blend);
            transform.rotation = Quaternion.Slerp(introStartRot, rotation, blend);

            if (introBlend >= 1f) mode = CameraMode.Gameplay;
        }

        // Eases from wherever the camera was when the player died up to a
        // bird's-eye view of the body.
        void UpdateDeathView()
        {
            deathBlend = Mathf.Clamp01(
                deathBlend + Time.deltaTime / Mathf.Max(0.01f, deathTransitionTime));

            float blend = Mathf.SmoothStep(0f, 1f, deathBlend);

            // Same orbit maths, steeper pitch, no shoulder offset, so the body
            // sits centre frame.
            Vector3 focus = target.position + Vector3.up * targetHeight;
            Quaternion rotation = Quaternion.Euler(deathPitch, yaw, 0f);
            Vector3 position = focus + rotation * new Vector3(0f, 0f, -deathDistance);

            transform.position = Vector3.Lerp(deathStartPos, position, blend);
            transform.rotation = Quaternion.Slerp(deathStartRot, rotation, blend);
        }
    }
}