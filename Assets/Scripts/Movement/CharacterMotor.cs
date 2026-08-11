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

        /// <summary>True once this character's death event has fired. Subclasses
        /// must check it before reading input.</summary>
        protected bool isDead;

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
            if (isDead)
            {
                SettleDead();
                return;
            }

            bool grounded = controller.isGrounded;
            ApplyGravity(grounded);
            wasGrounded = grounded;
            MoveActor(Vector3.zero);
            UpdateAnimator(0f, grounded, false);
        }

        /// <summary>
        /// Per-frame update for a dead character. Gravity keeps running so a body
        /// that died mid-air still falls and lands instead of freezing in place,
        /// but no input is read and no locomotion parameters are pushed — the
        /// animator is left alone so the death clip plays out uninterrupted.
        /// </summary>
        protected void SettleDead()
        {
            bool grounded = controller.isGrounded;
            ApplyGravity(grounded);
            wasGrounded = grounded;
            MoveActor(Vector3.zero);
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
            // Flag rather than `enabled = false`: disabling the component would
            // also stop gravity, leaving anyone who died in mid-air hanging there.
            if (e.Entity == gameObject)
                isDead = true;
        }
    }
}
