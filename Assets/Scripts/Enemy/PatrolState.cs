using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Patrol behaviour: cycle between waypoints at patrol speed.
    /// Movement logic to be implemented. Transitions to ChaseState when the
    /// player enters <c>chaseTriggerDistance</c>.
    /// </summary>
    public class PatrolState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            // TODO: base.EnterState(npc);
            //       agent.speed = patrolSpeed;
            //       pick/continue current waypoint and set destination.
        }

        public override void UpdateState(NpcFSM npc)
        {
            // TODO: advance waypoints on arrival;
            //       if Distance(npc, player) < chaseTriggerDistance -> npc.MoveToState(npc.s_Chase).
        }

        public override void ExitState(NpcFSM npc)
        {
            // TODO: base.ExitState(npc); stop any patrol coroutines.
        }
    }
}
