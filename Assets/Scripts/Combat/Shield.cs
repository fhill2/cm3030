using UnityEngine;

namespace Game.Combat
{
    public class Shield : MonoBehaviour
    {
        [SerializeField] private ShieldDef def;

        public ShieldDef Def => def;
    }
}
