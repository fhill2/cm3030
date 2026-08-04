using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Shared component for both player and enemy. Manages the shield's trigger
    /// collider — enabling it when blocking, disabling it when not.
    ///
    /// The shield collider must be tagged "Shield" and be a trigger Collider on
    /// a child of this GameObject (or this GameObject itself).
    /// WeaponHitbox checks for the "Shield" tag to detect blocks.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShieldBlock : MonoBehaviour
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
                if (shieldCollider != null)
                {
                    shieldCollider.enabled = value;
                    Debug.Log($"[ShieldBlock] {gameObject.name} IsBlocking={value}");
                }
            }
        }

        void Awake()
        {
            shieldCollider = GetComponent<Collider>();
            if (shieldCollider != null)
            {
                shieldCollider.enabled = false;
                Debug.Log($"[ShieldBlock] {gameObject.name} initialized — collider: {shieldCollider.GetType().Name}, tag='{shieldCollider.tag}', isTrigger={shieldCollider.isTrigger}");
            }
            else
            {
                Debug.LogError($"[ShieldBlock] {gameObject.name} — no Collider component found on this GameObject!");
            }
        }
    }
}
