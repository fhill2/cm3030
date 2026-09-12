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
    public enum InitialEnemyState { Patrol, Chase, Attack, Death }

    // State machine context for enemy AI. 
    public class NpcFSM : MonoBehaviour
    {
        // Unity can't serialize plain C# classes, so these are created with
        // field initializers and read at runtime.
        [Header("States")]
        public PatrolState s_Patrol = new PatrolState();
        public ChaseState  s_Chase  = new ChaseState();
        public AttackState s_Attack = new AttackState();
        public FleeState   s_Flee   = new FleeState();
        public DeathState  s_Death  = new DeathState();

        [Tooltip("State the enemy enters on spawn. Defaults to Patrol.")]
        [SerializeField] private InitialEnemyState initialState = InitialEnemyState.Patrol;

        [Header("Patrol Targets")]
        [Tooltip("Waypoints cycled through while in PatrolState.")]
        public List<Transform> patrolTargets;
        public int targetIndex;

        [Header("Distances")]
        [Tooltip("Player within this distance flips Patrol to Chase.")]
        public float chaseTriggerDistance = 8f;
        [Tooltip("Player farther than this ends Chase and returns to Patrol.")]
        public float chaseQuitDistance = 12f;
        [Tooltip("Within this distance the enemy attacks instead of chasing.")]
        public float attackRange = 2f;

        [Header("Speeds")]
        [Tooltip("NavMeshAgent speed while patrolling.")]
        public float npcPatrolSpeed = 1.5f;
        [Tooltip("NavMeshAgent speed while chasing.")]
        public float npcChaseSpeed = 3.5f;
        [Tooltip("NavMeshAgent speed while circling the player during Attack. Set to 0 to stand still instead.")]
        public float npcCircleSpeed = 2f;

        // Attack timing and damage live on the weapon and are run by Melee.
        [Header("Attack")]
        [Tooltip("Require an unobstructed ray to the player before applying damage.")]
        public bool requireLineOfSight = true;

        [Header("Blocking")]
        [Tooltip("Chance (0-1) this enemy raises its shield when it detects the player winding up a nearby swing. WaveConfig can scale this per wave for difficulty.")]
        [Range(0f, 1f)]
        public float blockChance = 0.3f;
        [Tooltip("How long the shield stays raised after a successful block roll.")]
        public float blockHoldDuration = 0.6f;

        [Header("Taunt")]
        [Tooltip("Seconds after hearing a taunt before the enemy breaks and flees.")]
        public float tauntReactionDelay = 1f;

        [Header("Timers")]
        [Tooltip("Seconds between patrol/chase status checks.")]
        public float checkTime = 0.2f;
        [Tooltip("Seconds the enemy idles at each waypoint.")]
        public float waitTime = 1.5f;
        [Tooltip("Stop this close to a destination.")]
        public float distanceToTarget = 0.5f;

        // Assigned in Start.
        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public GameObject player;
        [HideInInspector] public WeaponCollider weaponCollider;
        [HideInInspector] public ShieldCollider shieldCollider;
        [HideInInspector] public Animator animator;

        // Set by the event handlers, read by the states.
        [HideInInspector] public bool playerAlive = true;
        [HideInInspector] public bool wasHit;

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
            EventManager.OnTaunt  += HandleTaunt;
        }

        void OnDisable()
        {
            EventManager.OnDeath  -= HandleDeath;
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnHit    -= HandleHit;
            EventManager.OnTaunt  -= HandleTaunt;
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)
            {
                // A corpse caught mid-BlockRoutine would keep its shield up and
                // the Block bool stuck true.
                StopCoroutine(nameof(BlockRoutine));
                StopCoroutine(nameof(TauntReactionRoutine));
                if (shieldCollider != null) shieldCollider.IsBlocking = false;
                if (animator != null) animator.SetBool(AnimParams.Block, false);
                MoveToState(s_Death);
            }
            else if (e.Entity == player) playerAlive = false;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) wasHit = true;
        }

        // OnHit fires at the start of the player's windup, before the blade
        // goes live, so the enemy has time to raise its shield.
        void HandleHit(HitArgs e)
        {
            if (e.Entity != player || shieldCollider == null) return;
            if (!playerAlive || CurrentState == s_Death) return;

            // Only bother if the swing could plausibly reach us.
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > attackRange * 1.5f) return;

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

        void HandleTaunt()
        {
            if (!playerAlive) return;
            if (CurrentState != s_Chase && CurrentState != s_Attack) return;

            StopCoroutine(nameof(TauntReactionRoutine));
            StartCoroutine(nameof(TauntReactionRoutine));
        }

        private IEnumerator TauntReactionRoutine()
        {
            yield return new WaitForSeconds(tauntReactionDelay);

            if (!playerAlive) yield break;
            if (CurrentState != s_Chase && CurrentState != s_Attack) yield break;

            MoveToState(s_Flee);
        }

        public void MoveToState(BaseState state)
        {
            CurrentState?.ExitState(this);
            CurrentState = state;
            state.EnterState(this);
        }
    }
}