using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;
using Game.Audio;

namespace Game.Movement
{
    /// <summary>
    /// Shared movement foundation: gravity, ground detection, animation
    /// parameters, and death handling.
    /// PlayerMovement inherits and adds input-based control; enemies can use
    /// this directly for gravity/idle or subclass it for AI-driven movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] protected float gravity = -20f;

        [Header("References")]
        [Tooltip("Leave empty to auto-resolve from children.")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected ActorAudio actorAudio;

        [Header("Animation")]
        [Tooltip("Smoothing on the Speed parameter, so blends aren't instant.")]
        [SerializeField] protected float speedDampTime = 0.1f;

        protected CharacterController controller;
        protected Vector3 velocity;
        protected bool wasGrounded = true;

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (actorAudio == null) actorAudio = GetComponent<ActorAudio>();
        }

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        protected virtual void Update()
        {
            bool grounded = controller.isGrounded;
            ApplyGravity(grounded);
            wasGrounded = grounded;
            MoveActor(Vector3.zero);
            UpdateAnimator(0f, grounded, false);
        }

        /// <summary>Accumulate downward velocity. Call before MoveActor.</summary>
        protected void ApplyGravity(bool grounded)
        {
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
        }

        /// <summary>Move the character with combined horizontal + vertical motion.</summary>
        protected void MoveActor(Vector3 horizontal)
        {
            Vector3 motion = horizontal + Vector3.up * velocity.y;
            controller.Move(motion * Time.deltaTime);
        }

        /// <summary>Push Speed, Grounded, and Jump params to the Animator.</summary>
        protected void UpdateAnimator(float speed01, bool grounded, bool jumped)
        {
            if (animator == null) return;
            animator.SetFloat(AnimParams.Speed, speed01, speedDampTime, Time.deltaTime);
            animator.SetBool(AnimParams.Grounded, grounded && !jumped);
            if (jumped) animator.SetTrigger(AnimParams.Jump);
        }

        protected virtual void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)
                enabled = false;
        }
    }
}
