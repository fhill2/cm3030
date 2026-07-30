using UnityEngine;
using Game.Shared;

namespace Game.Systems
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
            GameEvents.OnDamageDealt?.Invoke(gameObject, amount, type);

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                if (animator != null) animator.SetTrigger(AnimParams.Hit);
            }

            OnHitTaken(type, source);
        }

        protected virtual void OnHitTaken(DamageType type, GameObject source) { }

        protected virtual void Die()
        {
            if (!alive) return;
            alive = false;
            if (animator != null) animator.SetTrigger(AnimParams.Die);
            OnDeath();
        }

        protected virtual void OnDeath() { }
    }
}
