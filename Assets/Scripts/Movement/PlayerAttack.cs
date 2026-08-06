using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Core;
using Game.Health;
using Game.Combat;

namespace Game.Movement
{
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

        [Header("Hitbox")]
        [Tooltip("The weapon hitbox armed during a swing. Leave empty for animation-only.")]
        [SerializeField] private WeaponHitbox weaponHitbox;
        [Tooltip("How long the weapon hitbox stays armed (the damage window).")]
        [SerializeField] private float swingWindow = 0.5f;

        [Header("Combat Idle")]
        [Tooltip("Seconds after the last swing before the stance relaxes back to Idle_Ready.")]
        [SerializeField] private float idleTimeout = 5f;

        [Header("Movement")]
        [Tooltip("Movement-speed multiplier while a swing is in progress (1 = normal).")]
        [SerializeField] private float swingSlowFactor = 0.5f;

        private CharacterController controller;
        private PlayerMovement movement;
        private int comboStep;
        private float lastAttackTime = -999f;
        private bool inCombat;
        private Coroutine swingRoutine;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
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

            // Let the chain lapse if they stopped clicking, so the next swing
            if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
                comboStep = 0;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (Time.time - lastAttackTime < EventManager.AttackWindow) return;
            if (requireGrounded && controller != null && !controller.isGrounded) return;
            animator.SetInteger(AnimParams.ComboStep, comboStep);
            animator.SetTrigger(AnimParams.Attack);

            // Entering/refreshing combat raises the idle stance to Idle_Battle.
            if (!inCombat)
            {
                inCombat = true;
                animator.SetBool(AnimParams.InCombat, true);
            }

            EventManager.RaiseHit(new HitArgs(gameObject));

            // Slow the player and arm the weapon hitbox for the damage window.
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(SwingRoutine());

            lastAttackTime = Time.time;
            comboStep = (comboStep + 1) % Mathf.Max(1, comboLength);
        }

        private IEnumerator SwingRoutine()
        {
            if (movement != null) movement.speedScale = swingSlowFactor;
            if (weaponHitbox != null) weaponHitbox.BeginSwing();
            yield return new WaitForSeconds(swingWindow);
            if (weaponHitbox != null) weaponHitbox.EndSwing();
            if (movement != null) movement.speedScale = 1f;
        }
    }
}
