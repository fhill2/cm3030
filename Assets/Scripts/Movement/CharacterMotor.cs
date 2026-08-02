using UnityEngine;
using Game.Core;
using Game.Health;
using Game.Audio;

namespace Game.Movement
{
    /// <summary>
    /// Player movement foundation: gravity, ground detection, CharacterController
    /// movement, and death handling. Inherits the animation driving
    /// (<see cref="AnimationMotor.UpdateAnimator"/>) from <see cref="AnimationMotor"/>.
    /// <see cref="PlayerMovement"/> inherits this and adds input-based control.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : AnimationMotor
    {
        [Header("Physics")]
        [SerializeField] protected float gravity = -20f;

        [Header("References")]
        [SerializeField] protected ActorAudio actorAudio;

        protected CharacterController controller;
        protected Vector3 velocity;
        protected bool wasGrounded = true;

        protected override void Awake()
        {
            base.Awake(); // AnimationMotor caches the animator
            controller = GetComponent<CharacterController>();
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

        protected virtual void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)
                enabled = false;
        }
    }
}
