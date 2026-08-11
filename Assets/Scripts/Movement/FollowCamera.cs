using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Health;

namespace Game.Movement
{
    /// <summary>
    /// Simple third-person orbit camera. The mouse looks around the player
    /// (yaw + pitch), and the player is rotated to face the view heading so the
    /// aim follows the camera. Replaces the Cinemachine FreeLook setup.
    ///
    /// Place this on the Main Camera. It runs in LateUpdate so it follows the
    /// player's movement frame-accurately.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The transform to follow. Leave empty to auto-find the Player.")]
        [SerializeField] private Transform target;
        [Tooltip("Look-at height above the target's origin (chest/head height).")]
        [SerializeField] private float targetHeight = 1.5f;

        [Header("Orbit")]
        [Tooltip("Distance from the target.")]
        [SerializeField] private float distance = 4f;
        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float sensitivity = 0.1f;
        [Tooltip("Lowest the camera can pitch (looking down at the player).")]
        [SerializeField] private float minPitch = -10f;
        [Tooltip("Highest the camera can pitch (looking down from above).")]
        [SerializeField] private float maxPitch = 60f;

        [Header("Aim Follow")]
        [Tooltip("If on, the player yaws to match the camera's heading (aim follows view).")]
        [SerializeField] private bool rotateTarget = true;

        [Header("Diagonal Turn")]
        [Tooltip("Degrees the player turns when pressing W+A or W+D.")]
        [SerializeField] private float diagonalTurnAngle = 35f;
        [Tooltip("How fast the turn blends in/out (higher = snappier).")]
        [SerializeField] private float turnSmooth = 8f;

        [Header("First Person")]
        [Tooltip("Toggle first-person view. Camera moves to the player's head and looks outward.")]
        [SerializeField] private bool firstPerson;
        [Tooltip("Eye height above the player's origin.")]
        [SerializeField] private float firstPersonHeight = 1.7f;

        [Header("Death View")]
        [Tooltip("On player death, pull the camera up and back into a bird's-eye view.")]
        [SerializeField] private bool deathView = true;
        [Tooltip("Camera pitch at the end of the climb. 90 looks straight down; ~80 keeps a sliver of horizon so the castle still reads.")]
        [SerializeField] private float deathPitch = 80f;
        [Tooltip("How far the camera pulls away from the body. Combined with the pitch this sets the height — 200 at 80 degrees puts the camera ~197m up. The scene camera's far clip is 1000, so there is room to go higher still.")]
        [SerializeField] private float deathDistance = 200f;
        [Tooltip("Seconds the climb takes. Keep PlayerUI's fadeDuration in step so the screen blacks out as the camera settles.")]
        [SerializeField] private float deathTransitionTime = 3f;

        private float yaw;
        private float pitch = 15f;
        private float turnOffset;

        private bool isDead;
        private float deathBlend;
        private Vector3 deathStartPos;
        private Quaternion deathStartRot;

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        void HandleDeath(DeathArgs e)
        {
            if (!deathView || isDead) return;
            // Only the followed player's death changes the shot — an enemy dying
            // must not yank the camera skyward.
            if (!IsFollowedTarget(e.Entity)) return;

            isDead = true;
            deathBlend = 0f;
            deathStartPos = transform.position;
            deathStartRot = transform.rotation;
        }

        /// <summary>
        /// True if <paramref name="entity"/> is the character we're following.
        /// The player prefab nests several levels, so the transform assigned as
        /// the follow target is not guaranteed to be the exact GameObject that
        /// carries the health component. Accepting anything on the same branch
        /// keeps this working however the prefab is rearranged.
        /// </summary>
        bool IsFollowedTarget(GameObject entity)
        {
            if (target == null || entity == null) return false;
            Transform t = entity.transform;
            return t == target || target.IsChildOf(t) || t.IsChildOf(target);
        }

        void Awake()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            // Seed yaw/pitch from the camera's current orientation so it
            // doesn't snap to defaults on the first frame.
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;   // wrap to -180..180
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (isDead)
            {
                // Returning here also stops the mouse-look and, critically, the
                // rotateTarget block below — otherwise the corpse would keep
                // spinning to follow the mouse.
                UpdateDeathView();
                return;
            }

            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw   += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;                       // mouse up -> look up
                pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            if (firstPerson)
            {
                Vector3 head = target.position + Vector3.up * firstPersonHeight;
                Quaternion fpRot = Quaternion.Euler(pitch, yaw, 0f);
                transform.position = head;
                transform.rotation = fpRot;
            }
            else
            {
                // Position the camera on a sphere around the focus point.
                Vector3 focus = target.position + Vector3.up * targetHeight;
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                transform.position = focus + rot * new Vector3(0f, 0f, -distance);
                transform.rotation = rot;   // looks straight at the focus point
            }

            // Player faces the camera heading plus a diagonal turn offset.
            if (rotateTarget)
            {
                float targetOffset = 0f;
                var kb = Keyboard.current;
                if (kb != null && kb.leftShiftKey.isPressed)
                {
                    bool w = kb.wKey.isPressed || kb.upArrowKey.isPressed;
                    bool a = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                    bool d = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                    bool s = kb.sKey.isPressed || kb.downArrowKey.isPressed;

                    if (w && a && !d) targetOffset = -diagonalTurnAngle;
                    else if (w && d && !a) targetOffset = diagonalTurnAngle;
                }
                turnOffset = Mathf.Lerp(turnOffset, targetOffset, turnSmooth * Time.deltaTime);
                target.rotation = Quaternion.Euler(0f, yaw + turnOffset, 0f);
            }
        }

        /// <summary>
        /// Ease the camera from wherever it was when the player died up to a
        /// bird's-eye view of the body.
        /// </summary>
        void UpdateDeathView()
        {
            deathBlend = Mathf.Clamp01(
                deathBlend + Time.deltaTime / Mathf.Max(0.01f, deathTransitionTime));

            // SmoothStep so the climb eases out of the gameplay shot and settles,
            // rather than starting and stopping abruptly.
            float t = Mathf.SmoothStep(0f, 1f, deathBlend);

            // Same orbit maths as the live camera, just a steeper pitch on a
            // longer arm. Keeping the yaw means the view rises from behind
            // wherever the player was facing instead of swinging round first.
            // Recomputed every frame so the shot stays centred while the body
            // finishes falling.
            Vector3 focus = target.position + Vector3.up * targetHeight;
            Quaternion rot = Quaternion.Euler(deathPitch, yaw, 0f);
            Vector3 pos = focus + rot * new Vector3(0f, 0f, -deathDistance);

            transform.position = Vector3.Lerp(deathStartPos, pos, t);
            transform.rotation = Quaternion.Slerp(deathStartRot, rot, t);
        }
    }
}
