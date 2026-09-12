using UnityEngine;
using Game.Shared;

namespace Game.Health
{
    // Sent whenever anything takes damage.
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

    // Sent when something dies.
    public readonly struct DeathArgs
    {
        public readonly GameObject Entity;

        public DeathArgs(GameObject entity)
        {
            Entity = entity;
        }
    }

    // Sent when something swings an attack.
    public readonly struct HitArgs
    {
        public readonly GameObject Entity;

        public HitArgs(GameObject entity)
        {
            Entity = entity;
        }
    }

    // Sent when a hit lands on a shield instead of the body.
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