using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Game.Core;

namespace Game.Enemy
{
    public class FleeState : BaseState
    {
        private const float FleeDuration = 5f;
        private const float MinFleeDistance = 6f;
        private const float MaxFleeDistance = 10f;
        private const float SampleRadius = 2f;

        private Coroutine fleeRoutine;

        public override void EnterState(NpcFSM npc)
        {
            base.EnterState(npc);

            EventManager.RaiseFlee(new FleeArgs(npc.gameObject));

            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.speed = chaseSpeed;
            }

            fleeRoutine = npc.StartCoroutine(FleeRoutine(npc));
        }

        public override void UpdateState(NpcFSM npc)
        {
        }

        public override void ExitState(NpcFSM npc)
        {
            if (fleeRoutine != null)
            {
                npc.StopCoroutine(fleeRoutine);
                fleeRoutine = null;
            }

            if (agent != null) agent.isStopped = false;
        }

        private IEnumerator FleeRoutine(NpcFSM npc)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(FleeDestination());

            yield return new WaitForSeconds(FleeDuration);

            npc.MoveToState(npc.s_Patrol);
        }

        private Vector3 FleeDestination()
        {
            float yaw = Random.Range(0f, 360f);
            var dir = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
            Vector3 target = FSM.transform.position + dir * Random.Range(MinFleeDistance, MaxFleeDistance);

            if (NavMesh.SamplePosition(target, out var hit, SampleRadius, NavMesh.AllAreas))
                return hit.position;

            return target;
        }
    }
}
