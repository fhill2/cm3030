using UnityEngine;

namespace Game.Enemy
{
    // Terminal state. Stops navigation so the corpse stays where it fell.
    public class DeathState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                // Zero the velocity before disabling. A disabled agent freezes
                // its velocity at the last value rather than resetting, and
                // EnemyMotor would keep reading it and animate a walking corpse.
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false;
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
        }
    }
}