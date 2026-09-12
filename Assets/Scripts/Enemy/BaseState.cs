using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Game.Shared;
using Game.Combat;

namespace Game.Enemy
{
    // Base for every enemy state. Not a MonoBehaviour, so states are plain
    // objects made by NpcFSM and coroutines have to go through FSM.StartCoroutine.
    public abstract class BaseState
    {
        // Shared tuning, cached from NpcFSM in EnterState.
        protected List<Transform> patrolTargets;
        protected int targetIndex;
        protected float waitTime;
        protected float checkTime;
        protected float distanceToTarget;

        protected float chaseTriggerDistance;
        protected float chaseQuitDistance;
        protected float attackRange;

        protected float patrolSpeed;
        protected float chaseSpeed;
        protected float circleSpeed;

        protected bool requireLineOfSight;

        // Runtime references.
        protected NavMeshAgent agent;
        protected GameObject player;
        protected WeaponCollider weaponCollider;
        protected NpcFSM FSM;

        // States override this and call base first, so the shared setup isn't
        // repeated in every one.
        public virtual void EnterState(NpcFSM npc)
        {
            patrolTargets = npc.patrolTargets;
            targetIndex = npc.targetIndex;
            waitTime = npc.waitTime;
            checkTime = npc.checkTime;
            distanceToTarget = npc.distanceToTarget;

            chaseTriggerDistance = npc.chaseTriggerDistance;
            chaseQuitDistance = npc.chaseQuitDistance;
            attackRange = npc.attackRange;

            patrolSpeed = npc.npcPatrolSpeed;
            chaseSpeed = npc.npcChaseSpeed;
            circleSpeed = npc.npcCircleSpeed;

            requireLineOfSight = npc.requireLineOfSight;

            agent = npc.agent;
            player = npc.player;
            weaponCollider = npc.weaponCollider;
            FSM = npc;
        }

        public abstract void UpdateState(NpcFSM npc);

        public virtual void ExitState(NpcFSM npc)
        {
        }
    }
}