using UnityEngine;

namespace Game.Audio
{
    public class FootContactAudio : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to auto-find in children. Must be a humanoid rig.")]
        [SerializeField] private Animator animator;
        [Tooltip("Leave empty to use the ActorAudio on this GameObject.")]
        [SerializeField] private ActorAudio actorAudio;

        [Header("Step Detection")]
        [Tooltip("How far (metres) a foot must fall from its peak before the turnaround counts as a step. Filters blend jitter and idle sway.")]
        [SerializeField] private float minDescent = 0.03f;
        [Tooltip("Minimum seconds between footsteps, shared by both feet. Extra guard against double-firing.")]
        [SerializeField] private float minInterval = 0.12f;

        [Header("Jump / Land")]
        [Tooltip("Play a grunt on leaving the ground and a thump on touching down.")]
        [SerializeField] private bool playJumpAndLand = true;

        private const float RiseEpsilon = 0.002f;

        private struct FootTracker
        {
            public float peak;
            public float lowest;
            public bool descending;

            public void Reset(float y)
            {
                peak = y;
                lowest = y;
                descending = false;
            }
        }

        private Transform leftFoot;
        private Transform rightFoot;
        private FootTracker left;
        private FootTracker right;
        private bool feetInitialised;

        private CharacterController controller;
        private bool wasGrounded = true;
        private float lastStepTime = float.NegativeInfinity;

        void Awake()
        {
            actorAudio = GetComponent<ActorAudio>();
            controller = GetComponentInParent<CharacterController>();

            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning($"[FootContactAudio] {gameObject.name} has no humanoid Animator — footstep audio disabled.");
                enabled = false;
                return;
            }

            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        void LateUpdate()
        {
            if (actorAudio == null || leftFoot == null || rightFoot == null) return;

            bool grounded = controller == null || controller.isGrounded;

            if (playJumpAndLand)
            {
                if (!grounded && wasGrounded)
                {
                    actorAudio.PlayJump();
                }
                else if (grounded && !wasGrounded)
                {
                    actorAudio.PlayLand();
                    ResetFeet();
                }
            }
            wasGrounded = grounded;

            if (!grounded)
            {
                ResetFeet();
                return;
            }

            if (!feetInitialised)
            {
                left.Reset(leftFoot.position.y);
                right.Reset(rightFoot.position.y);
                feetInitialised = true;
            }

            TrackFoot(leftFoot, ref left);
            TrackFoot(rightFoot, ref right);
        }

        void TrackFoot(Transform foot, ref FootTracker tracker)
        {
            float y = foot.position.y;

            if (tracker.descending)
            {
                if (y <= tracker.lowest + RiseEpsilon)
                {
                    tracker.lowest = y;
                    return;
                }

                if (tracker.peak - tracker.lowest >= minDescent)
                    FireStep();

                tracker.descending = false;
                tracker.peak = y;
                tracker.lowest = y;
            }
            else
            {
                if (y > tracker.peak)
                {
                    tracker.peak = y;
                }
                else if (tracker.peak - y >= minDescent * 0.5f)
                {
                    tracker.descending = true;
                    tracker.lowest = y;
                }
            }
        }

        void FireStep()
        {
            if (Time.time - lastStepTime < minInterval) return;
            lastStepTime = Time.time;
            actorAudio.PlayFootstep();
        }

        void ResetFeet()
        {
            if (leftFoot == null || rightFoot == null) return;
            left.Reset(leftFoot.position.y);
            right.Reset(rightFoot.position.y);
        }
    }
}
