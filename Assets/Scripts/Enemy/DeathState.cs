using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Terminal state entered when the enemy dies (driven by the
    /// <see cref="Game.Core.EventManager.OnDeath"/> subscription on NpcFSM).
    /// Stops navigation and disables the NavMeshAgent so the corpse stays put.
    /// </summary>
    public class DeathState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                // Zero the velocity BEFORE disabling. NavMeshAgent.velocity freezes
                // at its last value once the component is disabled rather than
                // resetting to zero, so without this EnemyMotor keeps reading
                // whatever speed the agent had at the moment of death — which is
                // why the corpse kept sliding/walking instead of settling into
                // the death pose.
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false;
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
            // No per-frame work — enemy is dead.
        }
    }
}
