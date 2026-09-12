using UnityEngine;

namespace Game.Combat
{
    // Sits on a weapon prefab, holding its stats and the grip point.
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponDef def;
        [SerializeField] private Transform anchor;

        // Settable so the shop can swap in an upgraded runtime copy.
        public WeaponDef Def { get => def; set => def = value; }

        public Transform Anchor => anchor;
    }
}