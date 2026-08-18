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

        // Virtual so subclasses can add their own subscriptions. 
        protected virtual void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        protected virtual void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        protected virtual void Update()
        {
            if (isDead)
            {
                SettleUncontrolled();
                return;
            }

            bool grounded = controller.isGrounded;
            ApplyGravity(grounded);
            wasGrounded = grounded;
            MoveActor(Vector3.zero);
            UpdateAnimator(0f, grounded, false);
        }

        /// <summary>
        /// Per-frame update for a character nobody is driving — dead, or the game
        /// loop is in a menu/shop state. Gravity keeps running so a body that died
        /// mid-air still falls and lands instead of freezing in place, but no
        /// input is read and no locomotion parameters are pushed, leaving the
        /// animator free to play the death clip out uninterrupted.
        /// </summary>
        protected void SettleUncontrolled()
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
