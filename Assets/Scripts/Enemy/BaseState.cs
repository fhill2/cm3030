using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Game.Shared;
using Game.Combat;

namespace Game.Enemy
{
    /// <summary>
    /// Abstract base for all enemy FSM states. Does NOT inherit from
    /// MonoBehaviour — states are plain C# objects instantiated by
    /// <see cref="NpcFSM"/> (e.g. <c>new PatrolState()</c>).
    ///
    /// <see cref="EnterState(NpcFSM)"/> caches every shared reference off the
    /// FSM so concrete states can read them without re-resolving. Because this
    /// class is not a MonoBehaviour, coroutines cannot be started here directly —
    /// they must go through the FSM component via <c>FSM.StartCoroutine(...)</c>.
    /// </summary>
    public abstract class BaseState
    {
        // ── Shared tuning, cached from NpcFSM in EnterState ──────────
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

        // ── Runtime references ───────────────────────────────────────
        protected NavMeshAgent agent;
        protected GameObject player;
        protected WeaponCollider weaponCollider;
        protected NpcFSM FSM;

        /// <summary>
        /// Cache all shared references off the FSM context. Marked virtual so
        /// concrete states can override and add their own setup after calling base.
        /// </summary>
        public virtual void EnterState(NpcFSM npc)
        {
            patrolTargets    = npc.patrolTargets;
            targetIndex      = npc.targetIndex;
            waitTime         = npc.waitTime;
            checkTime        = npc.checkTime;
            distanceToTarget = npc.distanceToTarget;

            chaseTriggerDistance = npc.chaseTriggerDistance;
            chaseQuitDistance    = npc.chaseQuitDistance;
            attackRange          = npc.attackRange;

            patrolSpeed = npc.npcPatrolSpeed;
            chaseSpeed  = npc.npcChaseSpeed;
            circleSpeed = npc.npcCircleSpeed;

            requireLineOfSight = npc.requireLineOfSight;

            agent        = npc.agent;
            player       = npc.player;
            weaponCollider = npc.weaponCollider;
            FSM          = npc;
        }

        /// <summary>
        /// Per-frame tick, driven by <see cref="NpcFSM.Update"/>.
        /// </summary>
        public abstract void UpdateState(NpcFSM npc);

        /// <summary>
        /// Cleanup when leaving this state. Marked virtual so subclasses can
        /// override only when they need to.
        /// </summary>
        public virtual void ExitState(NpcFSM npc)
        {
            // Default: no cleanup.
        }
    }
}
