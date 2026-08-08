using UnityEngine;

namespace Game.Health
{
    // Health for enemies. Everything is inherited from HealthSystem —
    // we add cleanup on death, and a way for the spawner to scale
    // difficulty per wave.
    public class EnemyHealth : HealthSystem
    {
        [SerializeField] private float destroyDelay = 2f;   // time for the death animation to play

        // Called by WaveSpawner straight after Instantiate. Has to run before
        // any damage lands, which it does, because Awake has already set
        // currentHealth from maxHealth by this point.
        public void ApplyHealthMultiplier(float multiplier)
        {
            if (multiplier <= 0f) return;

            maxHealth *= multiplier;
            currentHealth = maxHealth;
        }

        protected override void OnDeath()
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}