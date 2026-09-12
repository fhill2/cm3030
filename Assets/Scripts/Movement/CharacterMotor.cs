using UnityEngine;
using Game.Core;
using Game.Health;
using Game.Audio;

namespace Game.Movement
{
    // Movement foundation for the player: gravity, ground detection,
    // CharacterController movement, death handling.
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : AnimationMotor
    {
        [Header("Physics")]
        [SerializeField] protected float gravity = -20f;
        [Tooltip("Downward speed held while grounded. More negative keeps the controller pressed into the ground on descending stairs and slopes, so it stops losing contact between steps.")]
        [SerializeField] protected float groundStickSpeed = -12f;

        [Header("Grounding")]
        [Tooltip("Seconds the character still counts as grounded for animation after losing contact. Bridges the frame-long gaps that walking down stairs creates, which would otherwise trigger the fall animation on every step.")]
        [SerializeField] protected float groundedGrace = 0.3f;
        [Tooltip("How far below the feet to look for ground before accepting the character is really falling. Raise it if the fall animation still flickers, lower it if short drops stop animating.")]
        [SerializeField] protected float groundProbeDistance = 0.6f;

        private float lastGroundedTime = float.NegativeInfinity;

        // Reused so the per-frame probe doesn't allocate.
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        [Header("References")]
        [SerializeField] protected ActorAudio actorAudio;

        protected CharacterController controller;
        protected Vector3 velocity;
        protected bool wasGrounded = true;

        // Subclasses must check this before reading input.
        protected bool isDead;

        protected override void Awake()
        {
            base.Awake();
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

        // For a character nobody is driving: dead, or the game is in a menu or
        // shop state. Gravity keeps running so a body that died mid-air still lands
        protected void SettleUncontrolled()
        {
            bool grounded = controller.isGrounded;
            ApplyGravity(grounded);
            wasGrounded = grounded;
            MoveActor(Vector3.zero);
        }

        // Call before MoveActor.
        protected void ApplyGravity(bool grounded)
        {
            if (grounded && velocity.y < 0f)
                velocity.y = groundStickSpeed;
            velocity.y += gravity * Time.deltaTime;
        }

        // Walking down stairs makes isGrounded drop for a frame or two between
        // steps, which flips the animator into the fall state on every step.
        protected bool GroundedForAnimation(bool grounded, bool jumped)
        {
            if (jumped)
            {
                lastGroundedTime = float.NegativeInfinity;
                return false;
            }

            if (grounded)
            {
                lastGroundedTime = Time.time;
                return true;
            }

            // Rising means a real jump, never a stair. Checked before the probe.
            if (velocity.y > 0f) return false;

            if (Time.time - lastGroundedTime <= groundedGrace) return true;

            // Sprinting clears a longer gap per step than a fixed time window
            // covers, so fall back to a distance test.
            return ProbeGround();
        }

        // Cast from the bottom sphere's centre, ignoring anything belonging to
        // this character so its own capsule and gear can't register as floor.
        private bool ProbeGround()
        {
            float radius = Mathf.Max(0.01f, controller.radius - controller.skinWidth);
            Vector3 origin = transform.position + controller.center
                           - Vector3.up * (controller.height * 0.5f - controller.radius);

            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down,
                groundHits, groundProbeDistance, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = groundHits[i].collider;
                if (hit == null) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        protected void MoveActor(Vector3 horizontal)
        {
            Vector3 motion = horizontal + Vector3.up * velocity.y;
            controller.Move(motion * Time.deltaTime);
        }

        protected virtual void HandleDeath(DeathArgs e)
        {
            // A flag rather than disabling the component, which would also stop
            // gravity and leave anyone who died mid-air hanging there.
            if (e.Entity == gameObject)
                isDead = true;
        }
    }
}