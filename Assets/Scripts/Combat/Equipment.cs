using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Equipment/Weapon")]
    public class WeaponDef : ScriptableObject
    {
        [Header("Attributes")]
        [Tooltip("Damage dealt per hit.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Full swing duration in seconds — the blade stays armed for this long, and the next attack can't start until it elapses.")]
        [SerializeField] private float speed = 3f;

        public float Damage => damage;
        public float Speed => speed;
    }
}
