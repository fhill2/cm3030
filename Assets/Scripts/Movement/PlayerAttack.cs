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
        [SerializeField] private int comboLength;
        [Tooltip("Click again within this long to continue the chain, else it restarts.")]
        [SerializeField] private float comboResetTime;
        [Tooltip("Shortest gap between swings")]
        [SerializeField] private float minTimeBetweenSwings;

        [Header("Rules")]
        [Tooltip("Stop the player swinging while in mid-air.")]
        [SerializeField] private bool requireGrounded = true;

        [Header("Hitbox")]
        [Tooltip("The weapon hitbox armed during a swing. Leave empty for animation-only.")]
        [SerializeField] private WeaponHitbox weaponHitbox;
        [Tooltip("How long the weapon hitbox stays armed (the damage window).")]
        [SerializeField] private float swingWindow = 0.5f;

        private CharacterController controller;
        private int comboStep;
        private float lastAttackTime = -999f;
        private Coroutine swingRoutine;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null) return;

            // Let the chain lapse if they stopped clicking, so the next swing
            if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
                comboStep = 0;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (Time.time - lastAttackTime < minTimeBetweenSwings) return;
            if (requireGrounded && controller != null && !controller.isGrounded) return;
            animator.SetInteger(AnimParams.ComboStep, comboStep);
            animator.SetTrigger(AnimParams.Attack);

            EventManager.RaiseHit(new HitArgs(gameObject));

            // Arm the weapon hitbox for the damage window.
            if (weaponHitbox != null)
            {
                if (swingRoutine != null) StopCoroutine(swingRoutine);
                swingRoutine = StartCoroutine(SwingRoutine());
            }

            lastAttackTime = Time.time;
            comboStep = (comboStep + 1) % Mathf.Max(1, comboLength);
        }

        private IEnumerator SwingRoutine()
        {
            weaponHitbox.BeginSwing();
            yield return new WaitForSeconds(swingWindow);
            weaponHitbox.EndSwing();
        }
    }
}
