using UnityEngine;
using Game.Shared;
using Game.Core;

namespace Game.Health
{
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float hitImmunityWindow = 0.1f;

        protected float currentHealth;
        protected bool alive = true;
        protected Animator animator;
        protected float lastHitTime = float.NegativeInfinity;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => alive;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponentInChildren<Animator>();
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
                if (animator != null) animator.SetTrigger(AnimParams.Hit);
            }
        }

        protected virtual void Die()
        {
            if (!alive) return;
            alive = false;
            if (animator != null) animator.SetTrigger(AnimParams.Die);
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
