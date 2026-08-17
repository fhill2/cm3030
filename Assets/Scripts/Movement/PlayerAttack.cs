using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Combat;
using Game.Core;
using Game.Health;

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

        private CharacterController controller;
        private Melee melee;
        private StaminaSystem stamina;
        private int comboStep;
        private float lastAttackTime = -999f;
        private bool inCombat;
        private bool isDead;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            melee = GetComponent<Melee>();
            stamina = GetComponent<StaminaSystem>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;
            isDead = true;
        }

        void Update()
        {
            if (isDead || animator == null) return;

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

            // Ask Melee whether a swing would actually start before paying for
            // it. Clicking during the weapon's cooldown then costs nothing,
            // rather than draining stamina on attacks that never happen.
            if (melee == null || !melee.CanAttack) return;

            // Stamina gate. Spending happens here rather than inside Melee so the
            // enemy, which shares Melee, isn't affected. If there isn't enough,
            // or we're stunned, the swing never starts.
            if (stamina != null && !stamina.TrySpendAttack()) return;

            // Feed the combo step to the animator, then ask Melee to swing.
            animator.SetInteger(AnimParams.ComboStep, comboStep);
            if (melee.TryAttack())
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
    }
}