using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    // Player stamina. Attacking, sprinting, jumping and blocking all cost
    // stamina, and actions simply fail while the pool is short.
    //
    // Regeneration runs continuously; the only pause is a short wait after
    // each swing.
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
        [SerializeField] private float attackCost = 10f;

        [Tooltip("Stamina spent per jump.")]
        [SerializeField] private float jumpCost = 20f;

        [Header("Continuous Costs")]
        [Tooltip("Stamina drained per second while sprinting.")]
        [SerializeField] private float sprintDrainPerSecond = 12f;

        [Tooltip("Stamina drained per second while holding block. Lower than sprint's cost — blocking should be sustainable through a short exchange, not free.")]
        [SerializeField] private float blockDrainPerSecond = 1f;

        [Header("Regeneration")]
        [Tooltip("Stamina recovered per second.")]
        [SerializeField] private float regenPerSecond = 18f;

        [Tooltip("Seconds after a swing before regeneration resumes. Everything else regenerates immediately.")]
        [SerializeField] private float regenDelayAfterSwing = 0f;

        [Header("Debug")]
        [SerializeField] private bool logChanges;

        private float current;
        private float regenPausedUntil = float.NegativeInfinity;
        private int suppressRegenFrame = -1;
        private bool isDead;

        public float Current => current;
        public float Max => maxStamina;

        // The one question the movement and attack code should ask before acting.
        public bool CanAct => !isDead;

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

            TickRegen();
        }

        private void TickRegen()
        {
            if (current >= maxStamina) return;
            if (Time.time < regenPausedUntil) return;
            if (Time.frameCount - suppressRegenFrame < 2) return;

            current = Mathf.Min(maxStamina, current + regenPerSecond * Time.deltaTime);
            Announce();
        }

        // ── One-off costs ────────────────────────────────────────────────────
        // Each returns false if short, and the caller refuses the action.

        public bool TrySpendAttack()
        {
            if (!TrySpend(attackCost)) return false;

            regenPausedUntil = Time.time + regenDelayAfterSwing;
            return true;
        }
        
        // Spells cost a variable amount, unlike attacks and jumps which have
        // fixed costs on this component, so the caller passes the amount in.
        public bool TrySpendSpell(float cost)
        {
            return TrySpend(cost);
        }

        public bool TrySpendJump()
        {
            return TrySpend(jumpCost);
        }

        // ── Continuous costs ─────────────────────────────────────────────────
        // Called every frame while the action is held. Return false once there's
        // nothing left, which is the caller's signal to stop.

        public bool DrainSprint(float deltaTime)
        {
            return Drain(sprintDrainPerSecond * deltaTime);
        }

        public bool DrainBlock(float deltaTime)
        {
            return Drain(blockDrainPerSecond * deltaTime);
        }

        private bool Drain(float amount)
        {
            if (!CanAct) return false;

            suppressRegenFrame = Time.frameCount;
            Spend(amount);
            return current > 0f;
        }

        private bool TrySpend(float amount)
        {
            if (!CanAct) return false;
            if (current < amount) return false;

            Spend(amount);
            return true;
        }

        private void Spend(float amount)
        {
            if (amount <= 0f) return;

            current = Mathf.Max(0f, current - amount);

            Announce();
        }

        // Back to full, for a new run.
        public void Refill()
        {
            current = maxStamina;
            isDead = false;
            regenPausedUntil = float.NegativeInfinity;
            suppressRegenFrame = -1;

            Announce();
        }

        public void Restore(float amount)
        {
            if (amount <= 0f) return;

            current = Mathf.Min(maxStamina, current + amount);
            Announce();
        }

        private void Announce()
        {
            if (logChanges) Debug.Log($"[Stamina] {current:0} / {maxStamina:0}");

            EventManager.RaiseStaminaChanged(new StaminaChangedArgs(gameObject, current, maxStamina));
        }
    }
}
