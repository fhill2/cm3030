using UnityEngine;
using Game.Shared;
using Game.Core;

namespace Game.Health
{
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float hitImmunityWindow = 0.1f;
        [SerializeField] private int deathAnimationCount = 9;

        protected float currentHealth;
        protected bool alive = true;
        protected Animator animator;
        protected float lastHitTime = float.NegativeInfinity;
        private int getHitIndex;
        private int deathLayer = -1;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => alive;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                deathLayer = animator.GetLayerIndex("Death");
                if (deathLayer >= 0) animator.SetLayerWeight(deathLayer, 0f);
            }
        }

        public virtual void TakeDamage(float amount, DamageType type, GameObject source)
        {
            if (!alive || amount <= 0f) return;
            if (Time.time - lastHitTime < hitImmunityWindow) return;
            lastHitTime = Time.time;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            EventManager.RaiseDamage(new DamageArgs(gameObject, amount, type, source));

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                if (animator != null)
                {
                    animator.SetTrigger(AnimParams.Hit);
                    getHitIndex = (getHitIndex + 1) % 2;
                    animator.SetInteger(AnimParams.GetHitIndex, getHitIndex);
                }
            }
        }

        // Restores health, capped at max. Raises a damage event with a
        // negative amount so the health bar refreshes off the same signal it
        // already listens to, rather than needing a separate one.
        public virtual void Heal(float amount)
        {
            if (!alive || amount <= 0f) return;

            float before = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            float healed = currentHealth - before;
            if (healed <= 0f) return;   // already at full

            EventManager.RaiseDamage(
                new DamageArgs(gameObject, -healed, DamageType.Magic, gameObject));
        }

        public virtual void HealToFull()
        {
            Heal(maxHealth - currentHealth);
        }

        protected virtual void Die()
        {
            if (!alive) return;
            alive = false;
            if (animator != null)
            {
                animator.SetFloat(AnimParams.DeathIndex, Random.Range(0, deathAnimationCount));
                animator.SetBool(AnimParams.Dead, true);
                if (deathLayer >= 0) animator.SetLayerWeight(deathLayer, 1f);
            }
            EventManager.RaiseDeath(new DeathArgs(gameObject));
            OnDeath();
        }

        /// <summary>Immediately set HP to zero and trigger death. Useful for testing or kill zones.</summary>
        public virtual void Kill()
        {
            if (!alive) return;
            currentHealth = 0f;
            Die();
        }

        protected virtual void OnDeath() { }
    }
}