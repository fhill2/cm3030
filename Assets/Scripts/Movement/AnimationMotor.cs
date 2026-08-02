using UnityEngine;
using Game.Shared;

namespace Game.Movement
{
    /// <summary>
    /// Shared animation driver. Caches the Animator and exposes
    /// <see cref="UpdateAnimator"/> to push the Speed / Grounded / Jump params.
    ///
    /// Both the player (via <see cref="CharacterMotor"/>) and enemies
    /// (via <see cref="Game.Enemy.EnemyMotor"/>) inherit this so locomotion
    /// animation is driven the same way everywhere. This class holds ONLY the
    /// animation concern — gravity and movement live on the subclasses.
    /// </summary>
    public class AnimationMotor : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("Leave empty to auto-resolve from children.")]
        [SerializeField] protected Animator animator;

        [Tooltip("Smoothing on the Speed parameter, so blends aren't instant.")]
        [SerializeField] protected float speedDampTime = 0.1f;

        protected virtual void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Push Speed, Grounded, and Jump params to the Animator.
        /// <paramref name="speed01"/> is normalized (0 = idle, 1 = full speed).
        /// </summary>
        protected void UpdateAnimator(float speed01, bool grounded, bool jumped)
        {
            if (animator == null) return;
            animator.SetFloat(AnimParams.Speed, speed01, speedDampTime, Time.deltaTime);
            animator.SetBool(AnimParams.Grounded, grounded && !jumped);
            if (jumped) animator.SetTrigger(AnimParams.Jump);
        }
    }
}
