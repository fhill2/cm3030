using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Chase behaviour: pursue the player at chase speed using the NavMeshAgent.
    /// Movement logic to be implemented. Transitions to AttackState when the
    /// player is within <c>attackRange</c>, and back to PatrolState when the
    /// player is farther than <c>chaseQuitDistance</c>.
    /// </summary>
    public class ChaseState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            // TODO: base.EnterState(npc);
            //       agent.speed = chaseSpeed;
            //       begin chase status check coroutine.
        }

        public override void UpdateState(NpcFSM npc)
        {
            // TODO: agent.destination = player.transform.position;
            //       if Distance(npc, player) <= attackRange        -> npc.MoveToState(npc.s_Attack);
            //       else if Distance(npc, player) > chaseQuitDistance -> npc.MoveToState(npc.s_Patrol).
        }

        public override void ExitState(NpcFSM npc)
        {
            // TODO: base.ExitState(npc); stop chase coroutines.
        }
    }
}
