using System.Collections.Generic;
using UnityEngine;

namespace Game.Health
{
    // Health for enemies. Adds cleanup on death, a way for the spawner to
    // scale difficulty per wave, and a live count of who's still standing.
    public class EnemyHealth : HealthSystem
    {
        // A set rather than a counter, so a double add or remove can't drift
        // the number.
        private static readonly HashSet<EnemyHealth> aliveEnemies = new HashSet<EnemyHealth>();

        public static int AliveCount => aliveEnemies.Count;

        // Lets the HUD refresh on change instead of polling every frame.
        public static event System.Action OnAliveCountChanged;

        public int GoldReward { get; set; }

        // Static state survives leaving play mode when domain reload is off,
        // which would carry a stale count into the next run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            aliveEnemies.Clear();
            OnAliveCountChanged = null;
        }

        private void OnEnable()
        {
            if (alive && aliveEnemies.Add(this)) OnAliveCountChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (aliveEnemies.Remove(this)) OnAliveCountChanged?.Invoke();
        }

        // Called by WaveSpawner straight after Instantiate, which is after
        // Awake has set currentHealth from maxHealth.
        public void ApplyHealthMultiplier(float multiplier)
        {
            if (multiplier <= 0f) return;

            maxHealth *= multiplier;
            currentHealth = maxHealth;
        }

        protected override void OnDeath()
        {
            // The body stays on the floor; only a scene reload clears corpses.
            if (aliveEnemies.Remove(this)) OnAliveCountChanged?.Invoke();
        }
    }
}