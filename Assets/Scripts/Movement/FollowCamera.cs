using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Health;

namespace Game.Movement
{
    // Third-person orbit camera. Mouse looks around the player, and the player
    // yaws to match the view so aim follows the camera.
    // Goes on the Main Camera. Runs in LateUpdate so it follows movement
    // frame-accurately.
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
            if (!IsFollowedTarget(e.Entity)) return;

            isDead = true;
            deathBlend = 0f;
            deathStartPos = transform.position;
            deathStartRot = transform.rotation;
        }

        // The health component sits on a child, not on the object assigned as
        // the follow target, so compare against the whole branch.
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

            // Seed from the current orientation so it doesn't snap on frame one.
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (isDead)
            {
                // Returning here also skips rotateTarget below, otherwise the
                // corpse keeps spinning with the mouse.
                UpdateDeathView();
                return;
            }

            // Mouse-look and player rotation both stop while the shop is open,
            // otherwise clicking through the market spins the knight.
            bool locked = PlayerInputLock.InputLocked;

            if (!locked && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            if (firstPerson)
            {
                transform.position = target.position + Vector3.up * firstPersonHeight;
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else
            {
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

                // Push the focus point out to the right and up, so the knight
                // sits to the left of frame and the crosshair looks past him
                // instead of through his head.
                Vector3 focus = target.position
                                + Vector3.up * (targetHeight + shoulderHeight)
                                + rot * Vector3.right * shoulderOffset;

                Vector3 back = rot * Vector3.back;

                transform.position = focus + back * ArmLength(focus, back);
                transform.rotation = rot;
            }

            if (rotateTarget && !locked)
            {
                float targetOffset = 0f;
                var kb = Keyboard.current;
                if (kb != null && kb.leftShiftKey.isPressed)
                {
                    bool w = kb.wKey.isPressed || kb.upArrowKey.isPressed;
                    bool a = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                    bool d = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

                    if (w && a && !d) targetOffset = -diagonalTurnAngle;
                    else if (w && d && !a) targetOffset = diagonalTurnAngle;
                }
                turnOffset = Mathf.Lerp(turnOffset, targetOffset, turnSmooth * Time.deltaTime);
                target.rotation = Quaternion.Euler(0f, yaw + turnOffset, 0f);
            }
        }

        // Shortens the arm when a wall is in the way, so indoors the camera
        // stays in the room instead of ending up behind the geometry. A sphere
        // rather than a ray, so it catches doorframes and corners the camera
        // would otherwise slip past.
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

        void UpdateDeathView()
        {
            deathBlend = Mathf.Clamp01(
                deathBlend + Time.deltaTime / Mathf.Max(0.01f, deathTransitionTime));

            float t = Mathf.SmoothStep(0f, 1f, deathBlend);

            // Recomputed each frame so the shot stays centred while the body
            // finishes falling. No collision here, the climb goes up through
            // the roof rather than jamming against it. No shoulder offset
            // either, since the body should sit centre frame when you die.
            Vector3 focus = target.position + Vector3.up * targetHeight;
            Quaternion rot = Quaternion.Euler(deathPitch, yaw, 0f);
            Vector3 pos = focus + rot * new Vector3(0f, 0f, -deathDistance);

            transform.position = Vector3.Lerp(deathStartPos, pos, t);
            transform.rotation = Quaternion.Slerp(deathStartRot, rot, t);
        }
    }
}