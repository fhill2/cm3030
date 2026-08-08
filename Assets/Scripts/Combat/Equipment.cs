using UnityEngine;
using Game.Shared;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Equipment/Weapon")]
    public class WeaponDef : ScriptableObject
    {
        [Header("Attributes")]
        [Tooltip("Damage dealt per hit.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Damage type passed to IDamageable.TakeDamage.")]
        [SerializeField] private DamageType damageType = DamageType.Light;

        [Tooltip("Seconds between attacks (lower = faster).")]
        [SerializeField] private float attackCooldown = 1f;

        [Tooltip("Delay after a swing starts before the weapon hitbox is armed.")]
        [SerializeField] private float windup = 0.3f;

        [Tooltip("How long the weapon hitbox stays armed (the damage window).")]
        [SerializeField] private float activeWindow = 0.3f;

        public float Damage => damage;
        public DamageType DamageType => damageType;

        // The weapon's attack speed, expressed as the gap between swings.
        public float AttackCooldown => attackCooldown;
        public float Windup => windup;
        public float ActiveWindow => activeWindow;
    }

    [CreateAssetMenu(fileName = "Shield", menuName = "Equipment/Shield")]
    public class ShieldDef : ScriptableObject
    {
    }

    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponDef def;

        public WeaponDef Def => def;
    }

    public class Shield : MonoBehaviour
    {
        [SerializeField] private ShieldDef def;

        public ShieldDef Def => def;
    }
}
