using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Patrol behaviour: idle/guard until the player re-enters detection range,
    /// then resume chasing. Waypoint patrolling is left for a teammate.
    /// </summary>
    public class PatrolState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                agent.isStopped = true;   // stand still (no waypoints assigned yet)
                agent.speed = patrolSpeed;
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
            if (player == null || agent == null) return;

            // Player re-entered detection range -> resume the chase.
            float dist = HorizontalDistance(npc.transform.position, player.transform.position);
            if (dist <= chaseTriggerDistance)
            {
                npc.MoveToState(npc.s_Chase);
            }

            // TODO teammate: cycle between patrolTargets while no player is near.
        }

        public override void ExitState(NpcFSM npc)
        {
            if (agent != null) agent.isStopped = false;   // hand off to Chase
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
