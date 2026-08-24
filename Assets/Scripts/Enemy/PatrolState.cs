using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Enemy
{
    /// <summary>
    /// Patrol behaviour: cycle through <c>patrolTargets</c> waypoints in order,
    /// pausing for <c>waitTime</c> seconds at each one, then wrapping back to the
    /// first once the last is reached. If no waypoints are assigned, falls back
    /// to standing guard on the spot.
    ///
    /// Player detection is polled on a <c>checkTime</c> interval instead of every
    /// frame (cheap distance check, but no point running it 60x/sec per enemy).
    /// As soon as the player is within <c>chaseTriggerDistance</c>, control hands
    /// off to <see cref="ChaseState"/>.
    /// </summary>
    public class PatrolState : BaseState
    {
        private const float TimeoutBuffer = 3f;      // grace period added on top of the estimated travel time for a valid path
        private const float UnreachableTimeout = 3f; // short bail-out when the path is partial/invalid — waiting longer won't help

        private const float StuckCheckInterval = 1f;   // how often we sample position to detect a jam
        private const float StuckMoveThreshold = 0.15f; // minimum real movement expected per interval to count as "making progress"
        private const float StuckTimeout = 3f;          // consecutive time with no real progress before bailing, regardless of path status

        private Coroutine patrolRoutine;
        private float nextCheckTime;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            nextCheckTime = 0f;

            bool hasWaypoints = patrolTargets != null && patrolTargets.Count > 0;

            if (agent != null)
            {
                agent.speed = patrolSpeed;
                agent.isStopped = !hasWaypoints; // nothing to patrol to -> stand guard
            }

            if (hasWaypoints)
            {
                patrolRoutine = npc.StartCoroutine(PatrolLoop(npc));
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
            if (player == null || agent == null) return;

            // Poll on the checkTime interval instead of every frame.
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + Mathf.Max(checkTime, 0.05f);

            // Player entered detection range -> break off patrol and chase.
            float dist = HorizontalDistance(npc.transform.position, player.transform.position);
            if (dist <= chaseTriggerDistance)
            {
                npc.MoveToState(npc.s_Chase);
            }
        }

        public override void ExitState(NpcFSM npc)
        {
            if (patrolRoutine != null)
            {
                npc.StopCoroutine(patrolRoutine);
                patrolRoutine = null;
            }

            if (agent != null) agent.isStopped = false;   // hand off to Chase
        }

        // ── Waypoint loop ────────────────────────────────────────────

        private IEnumerator PatrolLoop(NpcFSM npc)
        {
            while (true)
            {
                Transform target = npc.patrolTargets[npc.targetIndex];

                if (target != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(target.position);

                    // Let the path finish computing before sizing the timeout,
                    // so remainingDistance reflects the real leg length rather
                    // than whatever was left over from the previous waypoint.
                    while (agent.pathPending) yield return null;

                    // Partial/invalid paths mean this point genuinely isn't
                    // reachable from here (off-mesh, isolated island) — no
                    // point waiting long. Complete paths get time proportional
                    // to the real leg distance, however long that is.
                    float timeout = (agent.pathStatus == NavMeshPathStatus.PathComplete)
                        ? (agent.remainingDistance / Mathf.Max(agent.speed, 0.01f)) + TimeoutBuffer
                        : UnreachableTimeout;

                    yield return WaitForArrival(npc, timeout);
                }

                // Idle at the waypoint before moving on.
                agent.isStopped = true;
                yield return new WaitForSeconds(waitTime);

                // Advance to the next waypoint, wrapping back to the start.
                npc.targetIndex = (npc.targetIndex + 1) % npc.patrolTargets.Count;
            }
        }

        // Waits for the agent to arrive at its current destination, bailing
        // out on either the overall timeout or a lack of real physical
        // progress. The stuck check catches something a "complete" path
        // status doesn't: a technically valid route through a tight archway
        // or doorway that local avoidance can't actually push the agent
        // through, so it just sits there jittering instead of arriving.
        private IEnumerator WaitForArrival(NpcFSM npc, float timeout)
        {
            float elapsed = 0f;
            float stuckFor = 0f;
            Vector3 lastCheckPos = npc.transform.position;
            float nextStuckCheck = Time.time + StuckCheckInterval;

            while (agent.remainingDistance > Mathf.Max(distanceToTarget, agent.stoppingDistance))
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout) yield break;

                if (Time.time >= nextStuckCheck)
                {
                    float moved = Vector3.Distance(npc.transform.position, lastCheckPos);
                    stuckFor = moved < StuckMoveThreshold ? stuckFor + StuckCheckInterval : 0f;
                    if (stuckFor > StuckTimeout) yield break;

                    lastCheckPos = npc.transform.position;
                    nextStuckCheck = Time.time + StuckCheckInterval;
                }

                yield return null;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
