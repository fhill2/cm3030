using UnityEngine;
using UnityEngine.AI;
using Game.Movement;

namespace Game.Enemy
{
    /// <summary>
    /// Drives the enemy's locomotion animation from the NavMeshAgent's velocity.
    /// Inherits <see cref="AnimationMotor.UpdateAnimator"/> (the shared animation
    /// code) so Speed/Grounded/Jump are pushed exactly like the player does.
    ///
    /// This class does NOT apply gravity — the NavMeshAgent pins the enemy to the
    /// NavMesh, so the enemy is always grounded. Combat animation (the Attack
    /// trigger) is still fired by <see cref="AttackState"/>.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : AnimationMotor
    {
        private NavMeshAgent agent;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        protected void Update()
        {
            if (agent == null) return;

            // Normalise the agent's actual speed against its configured max speed,
            // so the blend tree gets 0 (idle) .. 1 (full chase).
            float speed01 = agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;

            // The NavMeshAgent keeps the enemy on the walkable surface, so it is
            // always grounded from the animator's point of view.
            UpdateAnimator(speed01, true, false);
        }
    }
}
