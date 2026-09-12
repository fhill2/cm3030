using UnityEngine;
using UnityEngine.AI;
using Game.Movement;

namespace Game.Enemy
{
    // Drives the enemy's locomotion animation from the NavMeshAgent's velocity,
    // using the same shared animation code as the player. 
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : AnimationMotor
    {
        private NavMeshAgent agent;
        private bool agentWasEnabled = true;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
        }

        protected void Update()
        {
            if (agent == null) return;

            // DeathState disables the agent. Push one last Speed of 0 so the
            // blend tree settles instead of holding its last value.
            if (!agent.enabled)
            {
                if (agentWasEnabled)
                {
                    UpdateAnimator(0f, true, false);
                    agentWasEnabled = false;
                }
                return;
            }
            agentWasEnabled = true;

            // 0 is idle, 1 is full chase.
            float normalisedSpeed = agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;

            UpdateAnimator(normalisedSpeed, true, false);
        }
    }
}