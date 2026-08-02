using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Attack behaviour: stop, face the player, swing on a cooldown, and apply
    /// damage after a windup delay. Exits to ChaseState when the player leaves
    /// <c>attackRange</c>.
    ///
    /// CONTRACT (Phase 2 integration):
    ///   - <see cref="Game.Shared.AnimParams.Attack"/>     trigger on the animator
    ///   - <see cref="Game.Core.EventManager.RaiseHit"/>   plays effort grunt + swing SFX
    ///   - <see cref="Game.Shared.IDamageable.TakeDamage"/> on the player after windup
    /// </summary>
    public class AttackState : BaseState
    {
        public override void EnterState(NpcFSM npc)
        {
            // TODO: base.EnterState(npc);
            //       agent.isStopped = true;
            //       FSM.StartCoroutine(AttackLoop()).
        }

        public override void UpdateState(NpcFSM npc)
        {
            // Per-frame work (if any) outside the coroutine goes here.
            // TODO: face the player / evaluate exit conditions.
        }

        public override void ExitState(NpcFSM npc)
        {
            // TODO: base.ExitState(npc);
            //       agent.isStopped = false;
            //       npc.StopAllCoroutines().
        }
    }
}
