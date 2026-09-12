using UnityEngine;
using Game.Core;

namespace Game.Enemy
{
    // Runs the player down. Hands off to Attack once in range, or back to
    // Patrol if the player gets away.
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

            if (!npc.playerAlive)
            {
                npc.MoveToState(npc.s_Patrol);
                return;
            }

            // Skipped while the agent is off, e.g. mid-knockback.
            if (agent.enabled)
                agent.SetDestination(player.transform.position);

            float distance = HorizontalDistance(npc.transform.position, player.transform.position);

            if (distance <= attackRange)
            {
                npc.MoveToState(npc.s_Attack);
            }
            // Hunters, the wave's last few enemies, never drop back to patrol.
            else if (distance > chaseQuitDistance && !WaveSpawner.HuntMode)
            {
                npc.MoveToState(npc.s_Patrol);
            }
        }

        public override void ExitState(NpcFSM npc)
        {
            // The next state drives movement itself.
            if (agent != null) agent.isStopped = true;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}