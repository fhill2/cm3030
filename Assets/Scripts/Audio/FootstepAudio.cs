using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Drives footstep, jump and land SFX from <see cref="CharacterController"/>
    /// motion via <see cref="ActorAudio"/>.
    ///
    /// Steps are spaced by distance travelled rather than by a timer or by
    /// animation events. That keeps them in sync across the whole walk/run
    /// blend — running covers ground faster, so steps land faster — without
    /// needing an event marker on every locomotion clip.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FootstepAudio : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to use the ActorAudio on this object.")]
        [SerializeField] private ActorAudio actorAudio;

        [Header("Stride")]
        [Tooltip("Metres between steps at walking pace.")]
        [SerializeField] private float walkStride = 0.75f;
        [Tooltip("Metres between steps at full sprint. Longer: you cover more ground per stride when running.")]
        [SerializeField] private float runStride = 1.5f;
        [Tooltip("Speed treated as a full sprint. Match this to PlayerMovement's sprintSpeed.")]
        [SerializeField] private float sprintSpeed = 5f;
        [Tooltip("Below this speed the character counts as standing still.")]
        [SerializeField] private float minSpeed = 0.2f;

        [Header("Enable")]
        [SerializeField] private bool playFootsteps = true;
        [SerializeField] private bool playJumpAndLand = true;

        private CharacterController controller;
        private float distanceSinceStep;
        private bool wasGrounded = true;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (actorAudio == null) actorAudio = GetComponent<ActorAudio>();
        }

        void Update()
        {
            if (actorAudio == null) return;

            bool grounded = controller.isGrounded;

            if (playJumpAndLand)
            {
                if (grounded && !wasGrounded)
                {
                    actorAudio.PlayLand();
                    // Don't let distance banked mid-air fire a step on touchdown.
                    distanceSinceStep = 0f;
                }
                else if (!grounded && wasGrounded)
                {
                    actorAudio.PlayJump();
                }
            }

            wasGrounded = grounded;

            if (!playFootsteps || !grounded) return;

            // Horizontal only: settling onto the ground shouldn't read as walking.
            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            if (speed < minSpeed)
            {
                distanceSinceStep = 0f;
                return;
            }

            // A runner's stride is longer than a walker's, so scale it with speed.
            // A fixed stride makes sprinting sound frantic or walking sound sparse.
            float stride = Mathf.Lerp(walkStride, runStride,
                                      Mathf.InverseLerp(minSpeed, sprintSpeed, speed));

            distanceSinceStep += speed * Time.deltaTime;
            if (distanceSinceStep >= stride)
            {
                distanceSinceStep -= stride;
                actorAudio.PlayFootstep();
            }
        }
    }
}
