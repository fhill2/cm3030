using UnityEngine;

namespace Game.Health
{
    public class PlayerHealth : HealthSystem
    {
        protected override void OnDeath()
        {
            // Death event is already published by the base class.
            // Add player-specific death logic here (disable input, show UI, etc.).
            Debug.Log($"[PlayerHealth] Player died at {gameObject.name}");
        }
    }
}
