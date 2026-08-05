using UnityEngine;
using UnityEngine.InputSystem;

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

        private float yaw;
        private float pitch = 15f;
        private float turnOffset;

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

            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw   += delta.x * sensitivity;
                pitch -= delta.y * sensitivity;                       // mouse up -> look up
                pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Position the camera on a sphere around the focus point.
            Vector3 focus = target.position + Vector3.up * targetHeight;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus + rot * new Vector3(0f, 0f, -distance);
            transform.rotation = rot;   // looks straight at the focus point

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
                    else if (s && a && !d) targetOffset = diagonalTurnAngle;
                    else if (s && d && !a) targetOffset = -diagonalTurnAngle;
                }
                turnOffset = Mathf.Lerp(turnOffset, targetOffset, turnSmooth * Time.deltaTime);
                target.rotation = Quaternion.Euler(0f, yaw + turnOffset, 0f);
            }
        }
    }
}
