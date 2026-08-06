using System.Collections;
using UnityEngine;

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
        private const float PathTimeout = 5f; // safety net if a waypoint is unreachable

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

                    // Wait until arrival; bail out via timeout if the point
                    // can't be reached (e.g. destination off the NavMesh).
                    float elapsed = 0f;
                    while (agent.pathPending ||
                           agent.remainingDistance > Mathf.Max(distanceToTarget, agent.stoppingDistance))
                    {
                        elapsed += Time.deltaTime;
                        if (elapsed > PathTimeout) break;
                        yield return null;
                    }
                }

                // Idle at the waypoint before moving on.
                agent.isStopped = true;
                yield return new WaitForSeconds(waitTime);

                // Advance to the next waypoint, wrapping back to the start.
                npc.targetIndex = (npc.targetIndex + 1) % npc.patrolTargets.Count;
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
