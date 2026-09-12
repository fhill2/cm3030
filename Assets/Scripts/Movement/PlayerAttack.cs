using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Combat;
using Game.Core;
using Game.Health;

namespace Game.Movement
{
    // Attack input and combo bookkeeping.
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
        private PlayerMovement movement;
        private int comboStep;
        private float lastAttackTime = -999f;
        private bool inCombat;
        private bool isDead;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            melee = GetComponent<Melee>();
            stamina = GetComponent<StaminaSystem>();
            movement = GetComponent<PlayerMovement>();
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
            if (PlayerInputLock.InputLocked) return;

            // PlayerMovement owns whether the player is driving the character.
            if (movement != null && !movement.ControlEnabled) return;

            // Relax the combat stance after a while without attacking.
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

            // Ask Melee whether a swing would start before paying for it, so
            // clicking during the cooldown costs no stamina.
            if (melee == null || !melee.CanAttack) return;

            // Spent here rather than inside Melee, so the enemies sharing Melee
            // aren't affected.
            if (stamina != null && !stamina.TrySpendAttack()) return;

            animator.SetInteger(AnimParams.ComboStep, comboStep);
            if (melee.TryAttack())
            {
                lastAttackTime = Time.time;
                comboStep = (comboStep + 1) % Mathf.Max(1, comboLength);

                if (!inCombat)
                {
                    inCombat = true;
                    animator.SetBool(AnimParams.InCombat, true);
                }
            }
        }
    }
}