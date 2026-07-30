using UnityEngine;
using Game.Shared;

namespace Game.Health
{
    /// <summary>
    /// Published by HealthSystem whenever any entity takes damage.
    /// Consumers: AudioManager (hurt sound), UI (health bar), etc.
    /// </summary>
    public readonly struct DamageDealt
    {
        public readonly GameObject Target;
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly GameObject Source;

        public DamageDealt(GameObject target, float amount, DamageType type, GameObject source)
        {
            Target = target;
            Amount = amount;
            Type = type;
            Source = source;
        }
    }

    /// <summary>
    /// Published by HealthSystem when an entity dies.
    /// Consumers: AudioManager (death sound), Movement (stop), Camera, etc.
    /// </summary>
    public readonly struct EntityDied
    {
        public readonly GameObject Entity;

        public EntityDied(GameObject entity)
        {
            Entity = entity;
        }
    }
}
