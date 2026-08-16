using System.Collections;
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
        [Tooltip("NavMeshAgent speed while circling the player during Attack. Set to 0 to disable circling and stand still instead (old behavior).")]
        public float npcCircleSpeed = 2f;

        // ── Attack tuning ────────────────────────────────────────────
        // Attack timing/damage now live on the equipped weapon (WeaponDef) and are
        // executed by Melee. The FSM only keeps AI-side attack knobs here.
        [Header("Attack")]
        [Tooltip("Require an unobstructed ray to the player before applying damage.")]
        public bool requireLineOfSight = true;

        [Header("Blocking")]
        [Tooltip("Chance (0-1) this enemy raises its shield when it detects the player winding up a nearby swing. WaveConfig can scale this per wave for difficulty.")]
        [Range(0f, 1f)]
        public float blockChance = 0.3f;
        [Tooltip("How long the shield stays raised after a successful block roll.")]
        public float blockHoldDuration = 0.6f;

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
        [HideInInspector] public ShieldCollider shieldCollider; // on the enemy's shield, if equipped
        [HideInInspector] public Animator animator; // drives the shared Block bool, same as PlayerMovement

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
            shieldCollider = GetComponentInChildren<ShieldCollider>();
            animator = GetComponentInChildren<Animator>();

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
            EventManager.OnHit    += HandleHit;
        }

        void OnDisable()
        {
            EventManager.OnDeath  -= HandleDeath;
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnHit    -= HandleHit;
        }

        // ── EventManager handlers ────────────────────────────────────

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)
            {
                // A corpse mid-BlockRoutine would otherwise keep its shield raised
                // and the Block animator bool stuck true forever — same guard
                // PlayerMovement applies on death.
                StopCoroutine(nameof(BlockRoutine));
                if (shieldCollider != null) shieldCollider.IsBlocking = false;
                if (animator != null) animator.SetBool(AnimParams.Block, false);
                MoveToState(s_Death);
            }
            else if (e.Entity == player)      playerAlive = false;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) wasHit = true;
        }

        // Reacts to the PLAYER starting a swing (Melee/AttackState both raise
        // OnHit at the start of windup, before the blade goes active) by rolling
        // blockChance and, on success, raising the shield for blockHoldDuration.
        // Reuses WeaponCollider's existing "Shield" tag + IsBlocking check, so
        // this doesn't need any new hit-detection — just toggles the same
        // collider the player's own blocking uses.
        void HandleHit(HitArgs e)
        {
            if (e.Entity != player || shieldCollider == null) return;
            if (!playerAlive || CurrentState == s_Death) return;

            // Only bother if the player is actually close enough for this swing
            // to plausibly reach us.
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > attackRange * 1.5f) return;

            if (Random.value <= blockChance)
            {
                StopCoroutine(nameof(BlockRoutine));
                StartCoroutine(BlockRoutine());
            }
        }

        private IEnumerator BlockRoutine()
        {
            shieldCollider.IsBlocking = true;
            if (animator != null) animator.SetBool(AnimParams.Block, true);
            yield return new WaitForSeconds(blockHoldDuration);
            shieldCollider.IsBlocking = false;
            if (animator != null) animator.SetBool(AnimParams.Block, false);
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
