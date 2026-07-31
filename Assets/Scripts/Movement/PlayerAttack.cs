using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;

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

        private CharacterController controller;
        private int comboStep;
        private float lastAttackTime = -999f;

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

            lastAttackTime = Time.time;
            comboStep = (comboStep + 1) % Mathf.Max(1, comboLength);
        }
    }
}
