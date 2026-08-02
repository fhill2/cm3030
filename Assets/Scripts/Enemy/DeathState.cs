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
