using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Terminal state entered when the enemy dies (driven by the
    /// <see cref="Game.Core.EventManager.OnDeath"/> subscription on NpcFSM).
    /// Stops navigation and freezes the FSM.
    /// </summary>
    public class DeathState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            // TODO: base.EnterState(npc);
            //       agent.isStopped = true;
            //       agent.enabled = false.
        }

        public override void UpdateState(NpcFSM npc)
        {
            // No per-frame work — enemy is dead.
        }
    }
}
