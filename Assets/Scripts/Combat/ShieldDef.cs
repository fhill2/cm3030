using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Shield", menuName = "Equipment/Shield")]
    public class ShieldDef : ScriptableObject
    {
        [Header("Grip Offset")]
        [Tooltip("Local position offset from the hand bone.")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        [Tooltip("Local rotation offset (Euler angles).")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;
    }
}
