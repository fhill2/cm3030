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

        [Tooltip("Power tier. EnemySets pick equipment by level range.")]
        [SerializeField] private int level = 1;

        [Tooltip("Shop price in gold.")]
        [SerializeField] private int cost = 50;

        [Header("Grip Fine-Tune")]
        [Tooltip("Added to the base weapon grip rotation (Euler degrees) for this weapon.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        // Damage and Speed are settable so shop upgrades can raise them.
        // Only ever set on a runtime copy made with Instantiate(), never on
        // the shared asset — see PlayerLoadout.
        public float Damage { get => damage; set => damage = value; }
        public float Speed { get => speed; set => speed = value; }
        public int Level => level;
        public int Cost => cost;
        public Vector3 RotationOffset => rotationOffset;
    }
}