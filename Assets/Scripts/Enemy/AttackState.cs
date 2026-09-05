using System.Collections;
using UnityEngine;
using Game.Combat;

namespace Game.Enemy
{
    public class AttackState : BaseState
    {
        private const float FaceSpeed = 540f; // degrees per second
        private const float CircleRetargetInterval = 0.2f; // seconds between destination updates

        // How long an enemy holds its attack slot before voluntarily backing
        // off and giving a waiting enemy a turn. Randomised per-engagement so
        // a group doesn't rotate attackers in lockstep.
        private const float MinEngagementTime = 3f;
        private const float MaxEngagementTime = 6f;

        private Melee melee;
        private Coroutine driverRoutine;

        // Attack-phase movement: an enemy WITHOUT the attack slot orbits the
        // player at a fraction of attackRange instead of standing rooted. The
        // actual angle comes from AttackSlotManager's shared ring so waiting
        // enemies space themselves out evenly around the player instead of
        // each picking an independent random angle and clumping together.
        private float nextCircleRetargetTime;
        private bool circlingEnabled;

        // Attack-slot state: only an enemy holding the slot (see
        // AttackSlotManager) actually presses the attack. Everyone else in
        // this state circles and waits — the "one attacks while the rest
        // surround and watch for an opening" read from group fights like The
        // Witcher 3, instead of every enemy swinging at once.
        private bool hasSlot;
        private float slotReleaseTime;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            circlingEnabled = circleSpeed > 0f;
            hasSlot = false;

            if (agent != null)
            {
                agent.isStopped = !circlingEnabled; // stand still if circling is disabled (old behavior)
                agent.updateRotation = false; // we face the player manually
                agent.speed = circleSpeed;
            }

            nextCircleRetargetTime = 0f;
            AttackSlotManager.JoinRing(npc);

            melee = FSM.GetComponent<Melee>();
            driverRoutine = npc.StartCoroutine(AttackDriver());
        }

        public override void UpdateState(NpcFSM npc)
        {
            // Keep facing the player smoothly between swings.
            FacePlayer();
            UpdateAttackSlot(npc);

            if (!hasSlot) UpdateCircling();
        }

        public override void ExitState(NpcFSM npc)
        {
            if (driverRoutine != null)
            {
                npc.StopCoroutine(driverRoutine);
                driverRoutine = null;
            }

            if (hasSlot)
            {
                AttackSlotManager.ReleaseSlot(npc);
                hasSlot = false;
            }
            AttackSlotManager.LeaveRing(npc);

            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }

        // Polls the Melee component each tick; it gates the rate internally so the
        // enemy swings as soon as the cooldown allows. No timing lives here.
        // Only actually swings while holding the attack slot — an enemy still
        // waiting its turn stays in range but never calls TryAttack().
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

                if (hasSlot && melee != null) melee.TryAttack();

                yield return null;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        // Tries to claim the attack slot while waiting, and gives it up again
        // after a randomised engagement window so someone else gets a turn.
        // A solo enemy just keeps re-claiming the slot immediately since
        // nothing else is competing for it, so single-enemy fights still feel
        // as responsive as before.
        private void UpdateAttackSlot(NpcFSM npc)
        {
            if (!hasSlot)
            {
                if (!AttackSlotManager.TryClaimSlot(npc)) return;

                hasSlot = true;
                slotReleaseTime = Time.time + Random.Range(MinEngagementTime, MaxEngagementTime);
                if (agent != null) agent.isStopped = true; // plant and fight
                return;
            }

            if (Time.time >= slotReleaseTime)
            {
                AttackSlotManager.ReleaseSlot(npc);
                hasSlot = false;
                if (agent != null) agent.isStopped = !circlingEnabled; // back to waiting/circling
            }
        }

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
            if (Physics.Raycast(from, dir, out RaycastHit hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.transform == player.transform
                    || hit.transform.IsChildOf(player.transform)
                    || hit.transform == FSM.transform
                    || hit.transform.IsChildOf(FSM.transform);

            return true;
        }

        /// <summary>
        /// Moves the destination to this enemy's evenly-spaced spot on
        /// AttackSlotManager's shared ring, staying inside attackRange so the
        /// swing loop's WithinAttackRange() check keeps passing. Only runs
        /// while waiting for the attack slot — no-op when circleSpeed is 0
        /// (agent stays isStopped).
        /// </summary>
        private void UpdateCircling()
        {
            if (agent == null || player == null || circleSpeed <= 0f) return;
            if (Time.time < nextCircleRetargetTime) return;
            nextCircleRetargetTime = Time.time + CircleRetargetInterval;

            float angle = AttackSlotManager.GetRingAngle(FSM);
            float radius = attackRange * 0.75f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (agent != null && agent.enabled)
                agent.SetDestination(player.transform.position + offset);
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
