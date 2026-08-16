using UnityEngine;

namespace Game.Combat
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponDef def;

        // Settable so the shop can swap in an upgraded runtime copy of the def.
        public WeaponDef Def { get => def; set => def = value; }
    }
}