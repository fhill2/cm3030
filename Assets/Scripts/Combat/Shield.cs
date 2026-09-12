using UnityEngine;

namespace Game.Combat
{
    // Sits on a shield prefab, holding its stats and the grip point.
    public class Shield : MonoBehaviour
    {
        [SerializeField] private ShieldDef def;
        [SerializeField] private Transform anchor;

        // Settable so the shop can swap in an upgraded runtime copy.
        public ShieldDef Def { get => def; set => def = value; }

        public Transform Anchor => anchor;
    }
}