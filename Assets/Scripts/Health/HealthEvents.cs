using UnityEngine;
using Game.Shared;

namespace Game.Health
{
    /// <summary>
    /// Published by HealthSystem whenever any entity takes damage.
    /// Consumers: ActorAudio (hurt sound), UI (health bar), etc.
    /// </summary>
    public readonly struct DamageArgs
    {
        public readonly GameObject Target;
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly GameObject Source;

        public DamageArgs(GameObject target, float amount, DamageType type, GameObject source)
        {
            Target = target;
            Amount = amount;
            Type = type;
            Source = source;
        }
    }

    /// <summary>
    /// Published by HealthSystem when an entity dies.
    /// Consumers: ActorAudio (death sound), Movement (stop), Camera, etc.
    /// </summary>
    public readonly struct DeathArgs
    {
        public readonly GameObject Entity;

        public DeathArgs(GameObject entity)
        {
            Entity = entity;
        }
    }

    /// <summary>
    /// Raised when an entity performs an attack/hit action.
    /// Consumers: ActorAudio (effort grunt + weapon swing).
    /// </summary>
    public readonly struct HitArgs
    {
        public readonly GameObject Entity;

        public HitArgs(GameObject entity)
        {
            Entity = entity;
        }
    }

    /// <summary>
    /// Raised when a weapon hit is blocked by a shield collider.
    /// Consumers: ActorAudio (shield clang), UI (stamina/blocked flash).
    /// </summary>
    public readonly struct BlockArgs
    {
        public readonly GameObject Defender;
        public readonly GameObject Attacker;

        public BlockArgs(GameObject defender, GameObject attacker)
        {
            Defender = defender;
            Attacker = attacker;
        }
    }
}
