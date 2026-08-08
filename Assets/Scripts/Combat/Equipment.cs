using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Equipment/Weapon")]
    public class WeaponDef : ScriptableObject
    {
        [Header("Attributes")]
        [Tooltip("Damage dealt per hit.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Seconds between attacks (lower = faster).")]
        [SerializeField] private float attackCooldown = 1f;

        public float Damage => damage;

        // The weapon's attack speed, expressed as the gap between swings.
        public float AttackCooldown => attackCooldown;
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
