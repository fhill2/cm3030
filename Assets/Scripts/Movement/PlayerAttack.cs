using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Combat;

namespace Game.Movement
{
    // Player attack input + combo bookkeeping. The actual swing (rate, windup,
    // damage window, blade arming) is delegated to Melee so the timing is shared
    // with the enemy. This class only decides WHEN (left click + the combo/
    // grounded rules) and feeds the combo step to the animator.
    public class PlayerAttack : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the Animator on the character model.")]
        [SerializeField] private Animator animator;

        [Header("Combo")]
        [Tooltip("How many swings are in the chain before it loops back to the first.")]
        [SerializeField] private int comboLength = 4;
        [Tooltip("Click again within this long to continue the chain, else it restarts.")]
        [SerializeField] private float comboResetTime;

        [Header("Rules")]
        [Tooltip("Stop the player swinging while in mid-air.")]
        [SerializeField] private bool requireGrounded = true;

        [Header("Combat Idle")]
        [Tooltip("Seconds after the last swing before the stance relaxes back to Idle_Ready.")]
        [SerializeField] private float idleTimeout = 5f;

        [Header("Movement")]
        [Tooltip("Movement-speed multiplier while a swing is in progress (1 = normal).")]
        [SerializeField] private float swingSlowFactor = 0.5f;

        private CharacterController controller;
        private PlayerMovement movement;
        private Melee melee;
        private int comboStep;
        private float lastAttackTime = -999f;
        private bool inCombat;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            melee = GetComponent<Melee>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void OnEnable()
        {
            if (melee != null)
            {
                melee.OnAttackStart += OnSwingStart;
                melee.OnAttackEnd += OnSwingEnd;
            }
        }

        void OnDisable()
        {
            if (melee != null)
            {
                melee.OnAttackStart -= OnSwingStart;
                melee.OnAttackEnd -= OnSwingEnd;
            }
        }

        void Update()
        {
            if (animator == null) return;

            // Relax the combat stance once the player hasn't attacked for a while.
            if (inCombat && Time.time - lastAttackTime > idleTimeout)
            {
                inCombat = false;
                animator.SetBool(AnimParams.InCombat, false);
            }

            // Let the chain lapse if they stopped clicking.
            if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
                comboStep = 0;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (requireGrounded && controller != null && !controller.isGrounded) return;

            // Feed the combo step to the animator, then ask Melee to swing. It owns
            // the rate/timing; we only advance the combo if it actually did.
            animator.SetInteger(AnimParams.ComboStep, comboStep);
            if (melee != null && melee.TryAttack())
            {
                lastAttackTime = Time.time;
                comboStep = (comboStep + 1) % Mathf.Max(1, comboLength);

                // Entering/refreshing combat raises the idle stance to Idle_Battle.
                if (!inCombat)
                {
                    inCombat = true;
                    animator.SetBool(AnimParams.InCombat, true);
                }
            }
        }

        // Melee fires these at the start/end of the swing so the player slows down
        // for its duration without any timing logic living here.
        private void OnSwingStart()
        {
            if (movement != null) movement.speedScale = swingSlowFactor;
        }

        private void OnSwingEnd()
        {
            if (movement != null) movement.speedScale = 1f;
        }
    }
}
