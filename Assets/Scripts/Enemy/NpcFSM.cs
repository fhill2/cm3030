using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Game.Shared;
using Game.Core;
using Game.Health;
using Game.Combat;

namespace Game.Enemy
{
    /// <summary>
    /// Finite State Machine context for enemy AI.
    ///
    /// Holds shared tuning fields and runtime references that every state reads
    /// via <see cref="BaseState.EnterState(NpcFSM)"/>. State instances are plain
    /// C# objects (they do NOT inherit from MonoBehaviour), so any coroutine work
    /// must be started through this component via <c>StartCoroutine(...)</c>.
    ///
    /// Talks to the rest of the game exclusively through
    /// <see cref="EventManager"/> events and the
    /// <see cref="IDamageable"/> interface — it never references
    /// <see cref="HealthSystem"/> or <see cref="Game.Audio.ActorAudio"/> directly.
    /// </summary>
    public enum InitialEnemyState { Patrol, Chase, Attack, Death }

    public class NpcFSM : MonoBehaviour
    {
        // ── State instances (plain C# objects) ───────────────────────
        // NOTE: Unity cannot serialize references to plain C# classes, so these
        // are created via field initializers (new ...) and read at runtime.
        [Header("States")]
        public PatrolState s_Patrol = new PatrolState();
        public ChaseState  s_Chase  = new ChaseState();
        public AttackState s_Attack = new AttackState();
        public DeathState  s_Death  = new DeathState();

        [Tooltip("State the enemy enters on spawn. Defaults to Patrol.")]
        [SerializeField] private InitialEnemyState initialState = InitialEnemyState.Patrol;

        // ── Patrol targets ───────────────────────────────────────────
        [Header("Patrol Targets")]
        [Tooltip("Waypoints cycled through while in PatrolState.")]
        public List<Transform> patrolTargets;
        public int targetIndex;

        // ── Distances ────────────────────────────────────────────────
        [Header("Distances")]
        [Tooltip("Player within this distance flips Patrol -> Chase.")]
        public float chaseTriggerDistance = 8f;
        [Tooltip("Player farther than this ends Chase -> Patrol.")]
        public float chaseQuitDistance = 12f;
        [Tooltip("Within this distance the enemy attacks instead of chasing.")]
        public float attackRange = 2f;

        // ── Speeds ───────────────────────────────────────────────────
        [Header("Speeds")]
        [Tooltip("NavMeshAgent speed while patrolling.")]
        public float npcPatrolSpeed = 1.5f;
        [Tooltip("NavMeshAgent speed while chasing.")]
        public float npcChaseSpeed = 3.5f;

        // ── Attack tuning ────────────────────────────────────────────
        // Attack timing/damage now live on the equipped weapon (WeaponDef) and are
        // executed by Melee. The FSM only keeps AI-side attack knobs here.
        [Header("Attack")]
        [Tooltip("Require an unobstructed ray to the player before applying damage.")]
        public bool requireLineOfSight = true;

        // ── Timing ───────────────────────────────────────────────────
        [Header("Timers")]
        [Tooltip("Seconds between patrol/chase status checks.")]
        public float checkTime = 0.2f;
        [Tooltip("Seconds the enemy idles at each waypoint.")]
        public float waitTime = 1.5f;
        [Tooltip("Stop this close to a destination.")]
        public float distanceToTarget = 0.5f;

        // ── Runtime references (assigned in Start, hidden from inspector) ──
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public GameObject player;
        [HideInInspector] public WeaponCollider weaponCollider; // on the enemy's weapon/hand

        // ── Runtime flags (set by event handlers, read by states) ────
        [HideInInspector] public bool playerAlive = true;
        [HideInInspector] public bool wasHit; // extension hook for a future stagger state

        /// <summary>Currently active state.</summary>
        public BaseState CurrentState { get; private set; }

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            player = GameObject.FindGameObjectWithTag("Player");
            weaponCollider = GetComponentInChildren<WeaponCollider>();

            BaseState startState;
            switch (initialState)
            {
                case InitialEnemyState.Chase:  startState = s_Chase;  break;
                case InitialEnemyState.Attack: startState = s_Attack; break;
                case InitialEnemyState.Death:  startState = s_Death;  break;
                default:                       startState = s_Patrol; break;
            }
            MoveToState(startState);
        }

        void Update()
        {
            CurrentState?.UpdateState(this);
        }

        void OnEnable()
        {
            EventManager.OnDeath  += HandleDeath;
            EventManager.OnDamage += HandleDamage;
        }

        void OnDisable()
        {
            EventManager.OnDeath  -= HandleDeath;
            EventManager.OnDamage -= HandleDamage;
        }

        // ── EventManager handlers ────────────────────────────────────

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)       MoveToState(s_Death);
            else if (e.Entity == player)      playerAlive = false;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) wasHit = true;
        }

        // ── State transitions ────────────────────────────────────────

        /// <summary>
        /// Transition to a new state. Calls <see cref="BaseState.ExitState"/> on
        /// the outgoing state, swaps the reference, then calls
        /// <see cref="BaseState.EnterState"/> on the incoming state.
        /// </summary>
        public void MoveToState(BaseState state)
        {
            CurrentState?.ExitState(this);
            CurrentState = state;
            state.EnterState(this);
        }
    }
}
