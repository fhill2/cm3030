using UnityEngine;

namespace Game.Combat
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private WeaponDef def;

        public WeaponDef Def => def;
    }
}
