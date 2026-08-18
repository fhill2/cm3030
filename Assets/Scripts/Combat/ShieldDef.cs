using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "Shield", menuName = "Equipment/Shield")]
    public class ShieldDef : ScriptableObject
    {
        [Header("Attributes")]
        [Tooltip("Power tier. EnemySets pick equipment by level range.")]
        [SerializeField] private int level = 1;

        [Tooltip("Shop price in gold.")]
        [SerializeField] private int cost = 50;

        [Header("Grip Fine-Tune")]
        [Tooltip("Added to the base shield grip rotation (Euler degrees) for this shield.")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        public int Level => level;
        public int Cost => cost;
        public Vector3 RotationOffset => rotationOffset;
    }
}
