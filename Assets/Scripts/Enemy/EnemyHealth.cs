using System.Collections.Generic;
using UnityEngine;

namespace Game.Health
{
    // Health for enemies. Everything is inherited from HealthSystem —
    // we add cleanup on death, a way for the spawner to scale difficulty
    // per wave, and a live count of how many enemies are still standing.
    public class EnemyHealth : HealthSystem
    {
        // Every enemy still alive. A set rather than an int counter so a double
        // add or remove (dying, then being disabled, then destroyed) can't drift
        // the number — the same instance simply won't be counted twice.
        private static readonly HashSet<EnemyHealth> s_alive = new HashSet<EnemyHealth>();

        /// <summary>How many enemies are still alive, across the whole scene.</summary>
        public static int AliveCount => s_alive.Count;

        /// <summary>
        /// Raised whenever an enemy spawns or dies. Lets the HUD refresh on
        /// change instead of polling a count every frame.
        /// </summary>
        public static event System.Action OnAliveCountChanged;

        // Static state survives leaving play mode when Enter Play Mode Options
        // has domain reload turned off, which would carry a stale count into the
        // next run. Wiping it before the first scene loads keeps runs independent.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_alive.Clear();
            OnAliveCountChanged = null;
        }

        private void OnEnable()
        {
            if (alive && s_alive.Add(this)) OnAliveCountChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (s_alive.Remove(this)) OnAliveCountChanged?.Invoke();
        }

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
            // Leave the count now. The body stays on the floor as a scene-lifetime
            // object — only a restart (scene reload) cleans up corpses.
            if (s_alive.Remove(this)) OnAliveCountChanged?.Invoke();
        }
    }
}
