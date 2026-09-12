using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Game.Core;

namespace Game.Enemy
{
    // Cycles through the waypoints, pausing at each. With no waypoints
    // assigned, stands guard on the spot. 
    public class PatrolState : BaseState
    {
        private const float TimeoutBuffer = 3f;       // grace on top of the estimated travel time
        private const float UnreachableTimeout = 3f;  // short bail-out when the path is partial or invalid

        private const float StuckCheckInterval = 1f;
        private const float StuckMoveThreshold = 0.15f; // minimum movement per interval to count as progress
        private const float StuckTimeout = 3f;

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
                agent.isStopped = !hasWaypoints;
            }

            if (hasWaypoints)
            {
                patrolRoutine = npc.StartCoroutine(PatrolLoop(npc));
            }
        }

        public override void UpdateState(NpcFSM npc)
        {
            if (player == null || agent == null) return;

            // Down to the wave's last few enemies, so hunt the player whatever
            // the distance. The playerAlive guard stops a dead player
            // flip-flopping us with ChaseState.
            if (WaveSpawner.HuntMode && npc.playerAlive)
            {
                npc.MoveToState(npc.s_Chase);
                return;
            }

            // Polled on the checkTime interval, not every frame.
            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + Mathf.Max(checkTime, 0.05f);

            float distance = HorizontalDistance(npc.transform.position, player.transform.position);
            if (distance <= chaseTriggerDistance)
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

            if (agent != null) agent.isStopped = false;
        }

        private IEnumerator PatrolLoop(NpcFSM npc)
        {
            while (true)
            {
                Transform target = npc.patrolTargets[npc.targetIndex];

                if (target != null)
                {
                    agent.isStopped = false;
                    if (agent.enabled)
                        agent.SetDestination(target.position);

                    // Let the path finish computing before sizing the timeout,
                    // or remainingDistance is left over from the last waypoint.
                    while (agent.pathPending) yield return null;

                    // A partial path means the point isn't reachable from here,
                    // so there's no point waiting long.
                    float timeout = (agent.pathStatus == NavMeshPathStatus.PathComplete)
                        ? (agent.remainingDistance / Mathf.Max(agent.speed, 0.01f)) + TimeoutBuffer
                        : UnreachableTimeout;

                    yield return WaitForArrival(npc, timeout);
                }

                agent.isStopped = true;
                yield return new WaitForSeconds(waitTime);

                npc.targetIndex = (npc.targetIndex + 1) % npc.patrolTargets.Count;
            }
        }

        // Bails out on the timeout or on a lack of real movement. 
        private IEnumerator WaitForArrival(NpcFSM npc, float timeout)
        {
            float elapsed = 0f;
            float stuckFor = 0f;
            Vector3 lastCheckPosition = npc.transform.position;
            float nextStuckCheck = Time.time + StuckCheckInterval;

            while (agent.remainingDistance > Mathf.Max(distanceToTarget, agent.stoppingDistance))
            {
                elapsed += Time.deltaTime;
                if (elapsed > timeout) yield break;

                if (Time.time >= nextStuckCheck)
                {
                    float moved = Vector3.Distance(npc.transform.position, lastCheckPosition);
                    stuckFor = moved < StuckMoveThreshold ? stuckFor + StuckCheckInterval : 0f;
                    if (stuckFor > StuckTimeout) yield break;

                    lastCheckPosition = npc.transform.position;
                    nextStuckCheck = Time.time + StuckCheckInterval;
                }

                yield return null;
            }
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }
    }
}