using UnityEngine;

namespace Game.Shared
{
    public interface IDamageable
    {
        void TakeDamage(float amount, DamageType type, GameObject source);
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }
    }
}
