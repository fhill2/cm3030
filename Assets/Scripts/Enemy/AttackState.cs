using System.Collections;
using UnityEngine;
using Game.Combat;

namespace Game.Enemy
{
    public class AttackState : BaseState
    {
        private const float FaceSpeed = 540f; // degrees per second
        private const float CircleRetargetInterval = 0.2f; // seconds between destination updates
        private const float CircleAngularSpeed = 0.6f; // radians/sec orbiting the player

        private Melee melee;
        private Coroutine driverRoutine;

        // Attack-phase movement: each enemy orbits the player at a fraction of
        // attackRange instead of standing rooted, so a group of attackers reads
        // as a dynamic scrum rather than a static ring. Direction and starting
        // angle are randomised per-enemy so a cluster doesn't move in lockstep.
        private float circleAngle;
        private int circleDirection;
        private float nextCircleRetargetTime;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            bool circling = circleSpeed > 0f;

            if (agent != null)
            {
                agent.isStopped = !circling; // stand still if circling is disabled (old behavior)
                agent.updateRotation = false; // we face the player manually
                agent.speed = circleSpeed;
            }

            circleAngle = Random.Range(0f, Mathf.PI * 2f);
            circleDirection = Random.value < 0.5f ? 1 : -1;
            nextCircleRetargetTime = 0f;

            melee = FSM.GetComponent<Melee>();
            driverRoutine = npc.StartCoroutine(AttackDriver());
        }

        public override void UpdateState(NpcFSM npc)
        {
            // Keep facing the player smoothly between swings.
            FacePlayer();
            UpdateCircling();
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
            if (Physics.Raycast(from, dir, out RaycastHit hit, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.transform == player.transform
                    || hit.transform.IsChildOf(player.transform)
                    || hit.transform == FSM.transform
                    || hit.transform.IsChildOf(FSM.transform);

            return true;
        }

        /// <summary>
        /// Moves the destination around the player in a slow orbit, staying
        /// inside attackRange so the swing loop's WithinAttackRange() check
        /// keeps passing. No-op when circleSpeed is 0 (agent stays isStopped).
        /// </summary>
        private void UpdateCircling()
        {
            if (agent == null || player == null || circleSpeed <= 0f) return;
            if (Time.time < nextCircleRetargetTime) return;
            nextCircleRetargetTime = Time.time + CircleRetargetInterval;

            circleAngle += circleDirection * CircleAngularSpeed * CircleRetargetInterval;

            float radius = attackRange * 0.75f;
            Vector3 offset = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle)) * radius;
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
