using System.Collections;
using UnityEngine;
using Game.Combat;

namespace Game.Enemy
{
    // In range of the player. Only the enemy holding the attack slot actually
    // swings; the rest circle at a spaced-out spot on the shared ring and wait their turn.
    public class AttackState : BaseState
    {
        private const float FaceSpeed = 540f;               // degrees per second
        private const float CircleRetargetInterval = 0.2f;  // seconds between destination updates

        // Randomised per engagement so a group doesn't rotate attackers in
        // lockstep.
        private const float MinEngagementTime = 3f;
        private const float MaxEngagementTime = 6f;

        private Melee melee;
        private Coroutine driverRoutine;

        private float nextCircleRetargetTime;
        private bool circlingEnabled;

        private bool hasSlot;
        private float slotReleaseTime;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            circlingEnabled = circleSpeed > 0f;
            hasSlot = false;

            if (agent != null)
            {
                agent.isStopped = !circlingEnabled;
                agent.updateRotation = false;   // we face the player manually
                agent.speed = circleSpeed;
            }

            nextCircleRetargetTime = 0f;
            AttackSlotManager.JoinRing(npc);

            melee = FSM.GetComponent<Melee>();
            driverRoutine = npc.StartCoroutine(AttackDriver());
        }

        public override void UpdateState(NpcFSM npc)
        {
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

        // Melee gates its own rate, so this just polls it. An enemy waiting
        // its turn stays in range but never calls TryAttack.
        private IEnumerator AttackDriver()
        {
            while (true)
            {
                // Player out of range, dead, or behind cover. Hand back to
                // Chase so the enemy repositions.
                if (!WithinAttackRange() || !FSM.playerAlive || !HasLineOfSight())
                {
                    FSM.MoveToState(FSM.s_Chase);
                    yield break;
                }

                if (hasSlot && melee != null) melee.TryAttack();

                yield return null;
            }
        }

        // Claims the slot while waiting, then gives it up after a randomised window. 
        private void UpdateAttackSlot(NpcFSM npc)
        {
            if (!hasSlot)
            {
                if (!AttackSlotManager.TryClaimSlot(npc)) return;

                hasSlot = true;
                slotReleaseTime = Time.time + Random.Range(MinEngagementTime, MaxEngagementTime);
                if (agent != null) agent.isStopped = true;   // plant and fight
                return;
            }

            if (Time.time >= slotReleaseTime)
            {
                AttackSlotManager.ReleaseSlot(npc);
                hasSlot = false;
                if (agent != null) agent.isStopped = !circlingEnabled;
            }
        }

        private bool WithinAttackRange()
        {
            if (player == null || FSM == null) return false;

            Vector3 enemyPosition = FSM.transform.position;
            Vector3 playerPosition = player.transform.position;
            enemyPosition.y = 0f;
            playerPosition.y = 0f;

            return Vector3.Distance(enemyPosition, playerPosition) <= attackRange;
        }

        private bool HasLineOfSight()
        {
            if (!requireLineOfSight) return true;
            if (player == null || FSM == null) return false;

            Vector3 from = FSM.transform.position + Vector3.up * 1.2f;
            Vector3 to = player.transform.position + Vector3.up * 1.2f;
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.01f) return true;

            Vector3 direction = delta / distance;
            if (Physics.Raycast(from, direction, out RaycastHit hit, distance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.transform == player.transform
                    || hit.transform.IsChildOf(player.transform)
                    || hit.transform == FSM.transform
                    || hit.transform.IsChildOf(FSM.transform);

            return true;
        }

        // Moves to this enemy's spot on the shared ring, kept inside
        // attackRange so the swing loop's range check keeps passing.
        private void UpdateCircling()
        {
            if (agent == null || player == null || circleSpeed <= 0f) return;
            if (Time.time < nextCircleRetargetTime) return;
            nextCircleRetargetTime = Time.time + CircleRetargetInterval;

            float angle = AttackSlotManager.GetRingAngle(FSM);
            float radius = attackRange * 0.75f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            if (agent.enabled)
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