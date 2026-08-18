using UnityEngine;

namespace Game.Combat
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponDef def;
        [SerializeField] private Transform anchor;

        // Settable so the shop can swap in an upgraded runtime copy of the def.
        public WeaponDef Def { get => def; set => def = value; }

        public Transform Anchor => anchor;
    }
}
