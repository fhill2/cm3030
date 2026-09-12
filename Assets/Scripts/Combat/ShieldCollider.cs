using UnityEngine;

namespace Game.Combat
{
    // Turns the shield's trigger collider on while blocking and off otherwise.
    [RequireComponent(typeof(Collider))]
    public class ShieldCollider : MonoBehaviour
    {
        private Collider shieldCollider;

        private bool isBlocking;
        public bool IsBlocking
        {
            get => isBlocking;
            set
            {
                if (isBlocking == value) return;
                isBlocking = value;
                if (shieldCollider != null) shieldCollider.enabled = value;
            }
        }

        void Awake()
        {
            shieldCollider = GetComponent<Collider>();
            if (shieldCollider != null) shieldCollider.enabled = false;
        }
    }
}