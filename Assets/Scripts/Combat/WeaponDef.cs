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

        [Header("Grip Offset")]
        [Tooltip("Local position offset from the hand bone.")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        [Tooltip("Local rotation offset (Euler angles).")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        // Damage and Speed are settable so shop upgrades can raise them.
        // Only ever set on a runtime copy made with Instantiate(), never on
        // the shared asset — see PlayerLoadout.
        public float Damage { get => damage; set => damage = value; }
        public float Speed { get => speed; set => speed = value; }

        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;
    }
}