using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Chase behaviour: pursue the player at chase speed using the NavMeshAgent.
    /// Transitions to AttackState when within <c>attackRange</c>, and back to
    /// PatrolState when the player escapes beyond <c>chaseQuitDistance</c>.
    /// </summary>
    public class ChaseState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                agent.isStopped = false;
                agent.speed = chaseSpeed;
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
            if (player == null || agent == null) return;

            // Player died — stop chasing, drop back to patrol (idle).
            if (!npc.playerAlive)
            {
                npc.MoveToState(npc.s_Patrol);
                return;
            }

            // Pursue the player's current position.
            agent.SetDestination(player.transform.position);

            float dist = HorizontalDistance(npc.transform.position, player.transform.position);

            // Close enough to swing.
            if (dist <= attackRange)
            {
                npc.MoveToState(npc.s_Attack);
            }
            // Player escaped beyond give-up range.
            else if (dist > chaseQuitDistance)
            {
                npc.MoveToState(npc.s_Patrol);
            }
        }

        public override void ExitState(NpcFSM npc)
        {
            // Halt when leaving chase; the next state drives movement itself.
            if (agent != null) agent.isStopped = true;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
