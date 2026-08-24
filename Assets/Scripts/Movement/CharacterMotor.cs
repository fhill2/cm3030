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
        [Tooltip("Downward speed held while grounded. More negative keeps the controller pressed into the ground on descending stairs and slopes, so it stops losing contact between steps.")]
        [SerializeField] protected float groundStickSpeed = -8f;

        [Header("Grounding")]
        [Tooltip("Seconds the character still counts as grounded for ANIMATION after losing contact. Bridges the frame-long gaps that walking down stairs creates, which would otherwise trigger the fall animation on every step. Physics is unaffected.")]
        [SerializeField] protected float groundedGrace = 0.15f;
        [Tooltip("How far below the feet to look for ground before accepting the character is really falling. Only Raise it if the fall animation still flickers &lower it if short drops stop animating.")]
        [SerializeField] protected float groundProbeDistance = 0.6f;

        private float lastGroundedTime = float.NegativeInfinity;
        // Reused so the per-frame probe doesn't allocate.
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

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
            UpdateAnimator(0f, GroundedForAnimation(grounded, false), false);
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
                velocity.y = groundStickSpeed;
            velocity.y += gravity * Time.deltaTime;
        }

        /// <summary>      
        /// Walking down stairs or off a lip makes isGrounded drop for a frame or
        /// two between steps, which flips the animator into the fall state on
        /// every single step. This keeps the character reading as grounded for a
        /// moment after contact is lost, which is long enough to bridge those
        /// gaps but far shorter than a real fall
        /// </summary>
        protected bool GroundedForAnimation(bool grounded, bool jumped)
        {
            if (jumped)
            {
                // Launching: forget we were ever grounded, or the grace window
                // would cut the jump animation off almost immediately.
                lastGroundedTime = float.NegativeInfinity;
                return false;
            }

            if (grounded)
            {
                lastGroundedTime = Time.time;
                return true;
            }

            // Rising means a real jump, never a stair. Checked before the probe,
            // which would otherwise report the ground we have just left.
            if (velocity.y > 0f) return false;

            if (Time.time - lastGroundedTime <= groundedGrace) return true;

            // Sprinting clears a longer gap per step than any fixed time window
            // covers, so fall back to a distance test: if there is ground just
            // beneath the feet we are on stairs, however fast we crossed the gap.
            return ProbeGround();
        }

        /// <summary>
        /// True if there is ground within <see cref="groundProbeDistance"/> below
        /// the capsule. Cast from the bottom sphere's centre and ignores anything
        /// belonging to this character, so the player's own capsule, weapon and
        /// shield colliders can't register as floor.
        /// </summary>
        private bool ProbeGround()
        {
            float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
            Vector3 origin = transform.position + controller.center
                           - Vector3.up * (controller.height * 0.5f - controller.radius);

            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down,
                groundHits, groundProbeDistance, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i].collider;
                if (hit == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;   // ourselves
                return true;
            }
            return false;
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
