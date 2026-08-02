using System.Collections;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Enemy
{
    /// <summary>
    /// Attack behaviour: stop, face the player, swing on a cooldown, and apply
    /// damage after a windup delay. Exits to ChaseState when the player leaves
    /// <c>attackRange</c> or dies.
    ///
    /// Integration:
    ///   - <see cref="AnimParams.Attack"/>       trigger on the animator
    ///   - <see cref="EventManager.RaiseHit"/>   plays effort grunt + swing SFX
    ///   - <see cref="IDamageable.TakeDamage"/>  on the player after windup
    /// </summary>
    public class AttackState : BaseState
    {
        private const float FaceSpeed = 540f; // degrees per second

        private Coroutine attackRoutine;
        private float lastAttackTime = float.NegativeInfinity;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                agent.isStopped = true;
                agent.updateRotation = false; // we face the player manually
            }

            lastAttackTime = float.NegativeInfinity;
            attackRoutine = npc.StartCoroutine(AttackLoop());
        }

        public override void UpdateState(NpcFSM npc)
        {
            // Keep facing the player smoothly between swings.
            FacePlayer();
        }

        public override void ExitState(NpcFSM npc)
        {
            if (attackRoutine != null)
            {
                npc.StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }

        // ── Attack loop ──────────────────────────────────────────────

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                // Player slipped out of range (or died) — hand back to Chase.
                if (!WithinAttackRange() || !FSM.playerAlive)
                {
                    FSM.MoveToState(FSM.s_Chase);
                    yield break;
                }

                if (Time.time - lastAttackTime < attackCooldown)
                {
                    yield return null;
                    continue;
                }

                // Swing.
                lastAttackTime = Time.time;
                if (animator != null) animator.SetTrigger(AnimParams.Attack);
                EventManager.RaiseHit(new HitArgs(FSM.gameObject));

                // Wait for the swing to connect (synced to the windup).
                yield return new WaitForSeconds(attackWindup);

                // Apply damage if the player is still reachable.
                if (WithinAttackRange() && HasLineOfSight() && FSM.playerAlive)
                {
                    var target = player.GetComponent<IDamageable>();
                    target?.TakeDamage(attackDamage, damageType, FSM.gameObject);
                }

                yield return null;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private bool WithinAttackRange()
        {
            if (player == null || FSM == null) return false;
            Vector3 a = FSM.transform.position;
            Vector3 b = player.transform.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b) <= attackRange;
        }

        private bool HasLineOfSight()
        {
            if (!requireLineOfSight) return true;
            if (player == null || FSM == null) return false;

            Vector3 from  = FSM.transform.position + Vector3.up * 1.2f;
            Vector3 to    = player.transform.position + Vector3.up * 1.2f;
            Vector3 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 0.01f) return true;

            Vector3 dir = delta / dist;
            if (Physics.Raycast(from, dir, out RaycastHit hit, dist))
                return hit.transform == player.transform
                    || hit.transform.IsChildOf(player.transform);

            return true;
        }

        private void FacePlayer()
        {
            if (player == null || FSM == null) return;

            Vector3 toPlayer = player.transform.position - FSM.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(toPlayer);
            FSM.transform.rotation = Quaternion.RotateTowards(
                FSM.transform.rotation, target, FaceSpeed * Time.deltaTime);
        }
    }
}
