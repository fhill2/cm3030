using System.Collections;
using UnityEngine;
using Game.Combat;

namespace Game.Enemy
{
    public class AttackState : BaseState
    {
        private const float FaceSpeed = 540f; // degrees per second

        private Melee melee;
        private Coroutine driverRoutine;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            if (agent != null)
            {
                agent.isStopped = true;
                agent.updateRotation = false; // we face the player manually
            }

            melee = FSM.GetComponent<Melee>();
            driverRoutine = npc.StartCoroutine(AttackDriver());
        }

        public override void UpdateState(NpcFSM npc)
        {
            // Keep facing the player smoothly between swings.
            FacePlayer();
        }

        public override void ExitState(NpcFSM npc)
        {
            if (driverRoutine != null)
            {
                npc.StopCoroutine(driverRoutine);
                driverRoutine = null;
            }

            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }

        // Polls the Melee component each tick; it gates the rate internally so the
        // enemy swings as soon as the cooldown allows. No timing lives here.
        private IEnumerator AttackDriver()
        {
            while (true)
            {
                // Player slipped out of range, died, or is no longer visible —
                // hand back to Chase so the enemy repositions.
                if (!WithinAttackRange() || !FSM.playerAlive || !HasLineOfSight())
                {
                    FSM.MoveToState(FSM.s_Chase);
                    yield break;
                }

                if (melee != null) melee.TryAttack();

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
