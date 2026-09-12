using UnityEngine;
using Game.Shared;

namespace Game.Movement
{
    // Shared animation driver. Both the player and the enemies inherit this so
    // locomotion animation is pushed the same way everywhere. 
    public class AnimationMotor : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Leave empty to auto-resolve from children.")]
        [SerializeField] protected Animator animator;

        [Tooltip("Smoothing on the Speed parameter, so blends aren't instant.")]
        [SerializeField] protected float speedDampTime = 0.1f;

        [Tooltip("Smoothing on MoveX/MoveZ. Higher = slower direction changes (less foot shuffle on rapid A/D), but strafes take longer to commit.")]
        [SerializeField] protected float moveDampTime = 0.15f;

        private bool hasMoveParams;

        protected virtual void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            hasMoveParams = HasParam(AnimParams.MoveX) && HasParam(AnimParams.MoveZ);
        }

        // Camera-relative input for the 2D locomotion blend. 
        protected void SetMoveInput(float moveX, float moveZ)
        {
            if (animator == null || !hasMoveParams) return;
            animator.SetFloat(AnimParams.MoveX, moveX, moveDampTime, Time.deltaTime);
            animator.SetFloat(AnimParams.MoveZ, moveZ, moveDampTime, Time.deltaTime);
        }

        bool HasParam(int id)
        {
            if (animator == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == id) return true;
            return false;
        }

        // normalisedSpeed is 0 for idle, 1 for full speed.
        protected void UpdateAnimator(float normalisedSpeed, bool grounded, bool jumped)
        {
            if (animator == null) return;
            animator.SetFloat(AnimParams.Speed, normalisedSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(AnimParams.Grounded, grounded && !jumped);
            if (jumped) animator.SetTrigger(AnimParams.Jump);
        }
    }
}