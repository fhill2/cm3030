using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    // Player stamina. Attacking, sprinting, jumping and blocking all cost
    // stamina, and running out stuns you for a fixed window where none of
    // them work.
    //
    // Nothing here reaches into the movement or attack code. Those ask this
    // component for permission and report their cost to it. The UI reads the
    // value through OnStaminaChanged rather than holding a reference.
    //
    // Goes on the player object.
    public class StaminaSystem : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private float maxStamina = 100f;

        [Header("One-off Costs")]
        [Tooltip("Stamina spent per swing.")]
        [SerializeField] private float attackCost = 25f;

        [Tooltip("Stamina spent per jump.")]
        [SerializeField] private float jumpCost = 20f;

        [Tooltip("Stamina spent when a hit lands on your shield, on top of the block drain.")]
        [SerializeField] private float blockedHitCost = 15f;

        [Header("Continuous Costs")]
        [Tooltip("Stamina drained per second while the shield is up.")]
        [SerializeField] private float blockDrainPerSecond = 20f;

        [Tooltip("Stamina drained per second while sprinting.")]
        [SerializeField] private float sprintDrainPerSecond = 12f;

        [Header("Regeneration")]
        [Tooltip("Stamina recovered per second once regeneration starts.")]
        [SerializeField] private float regenPerSecond = 18f;

        [Tooltip("Seconds of not spending stamina before it starts coming back.")]
        [SerializeField] private float regenDelay = 1f;

        [Header("Stun")]
        [Tooltip("Seconds of being unable to act after running out.")]
        [SerializeField] private float stunDuration = 2f;

        [Tooltip("Fraction of the pool that must refill before the stun lifts, even if the timer is up. 0 disables this.")]
        [Range(0f, 1f)]
        [SerializeField] private float stunRecoveryFraction = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool logChanges;

        private float current;
        private float lastSpendTime = float.NegativeInfinity;
        private float stunEndsAt;
        private bool stunned;
        private bool isDead;

        public float Current => current;
        public float Max => maxStamina;
        public bool IsStunned => stunned;

        // The one question the movement and attack code should ask before acting.
        public bool CanAct => !stunned && !isDead;

        private void Awake()
        {
            current = maxStamina;
        }

        private void Start()
        {
            // Announced in Start so a UI that subscribed in OnEnable has a
            // value to show from the first frame.
            Announce();
        }

        private void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;
            isDead = true;
        }

        private void Update()
        {
            if (isDead) return;

            TickStun();
            TickRegen();
        }

        private void TickStun()
        {
            if (!stunned) return;

            // Both conditions have to be met: the timer has to expire AND enough
            // stamina has to be back. Without the second the player leaves the
            // stun with nothing in the tank and immediately re-stuns.
            if (Time.time < stunEndsAt) return;
            if (current < maxStamina * stunRecoveryFraction) return;

            stunned = false;
            if (logChanges) Debug.Log("[Stamina] Stun over");

            EventManager.RaiseStun(new StunArgs(gameObject, false));
        }

        private void TickRegen()
        {
            if (current >= maxStamina) return;
            if (Time.time - lastSpendTime < regenDelay) return;

            current = Mathf.Min(maxStamina, current + regenPerSecond * Time.deltaTime);
            Announce();
        }

        // ── One-off costs ────────────────────────────────────────────────────
        // Each returns false if stunned or short, and the caller refuses the action.

        public bool TrySpendAttack()
        {
            return TrySpend(attackCost);
        }

        public bool TrySpendJump()
        {
            return TrySpend(jumpCost);
        }

        public bool TrySpendBlockedHit()
        {
            return TrySpend(blockedHitCost);
        }

        // ── Continuous costs ─────────────────────────────────────────────────
        // Called every frame while the action is held. Return false once there's
        // nothing left, which is the caller's signal to stop.

        public bool DrainBlock(float deltaTime)
        {
            return Drain(blockDrainPerSecond * deltaTime);
        }

        public bool DrainSprint(float deltaTime)
        {
            return Drain(sprintDrainPerSecond * deltaTime);
        }

        private bool Drain(float amount)
        {
            if (!CanAct) return false;

            Spend(amount);
            return !stunned;
        }

        private bool TrySpend(float amount)
        {
            if (!CanAct) return false;
            if (current < amount) return false;

            Spend(amount);
            return true;
        }

        // Takes stamina and triggers the stun if it empties the pool. Used by
        // both the all-or-nothing costs and the continuous drains.
        private void Spend(float amount)
        {
            if (amount <= 0f) return;

            current = Mathf.Max(0f, current - amount);
            lastSpendTime = Time.time;

            Announce();

            if (current <= 0f && !stunned) BeginStun();
        }

        private void BeginStun()
        {
            stunned = true;
            stunEndsAt = Time.time + stunDuration;

            if (logChanges) Debug.Log("[Stamina] Out of stamina, stunned");

            EventManager.RaiseStun(new StunArgs(gameObject, true));
        }

        // Back to full, for a new run.
        public void Refill()
        {
            current = maxStamina;
            stunned = false;
            isDead = false;
            lastSpendTime = float.NegativeInfinity;

            Announce();
            EventManager.RaiseStun(new StunArgs(gameObject, false));
        }

        private void Announce()
        {
            if (logChanges) Debug.Log($"[Stamina] {current:0} / {maxStamina:0}");

            EventManager.RaiseStaminaChanged(new StaminaChangedArgs(gameObject, current, maxStamina));
        }
    }
}