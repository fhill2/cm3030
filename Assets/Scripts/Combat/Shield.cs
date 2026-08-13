using UnityEngine;

namespace Game.Combat
{
    public class Shield : MonoBehaviour
    {
        [SerializeField] private ShieldDef def;

        // Settable so the shop can swap in an upgraded runtime copy of the def.
        public ShieldDef Def { get => def; set => def = value; }
    }
}