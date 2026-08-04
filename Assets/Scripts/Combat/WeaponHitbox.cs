using System.Collections.Generic;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private DamageType damageType = DamageType.Light;

        private readonly HashSet<Collider> m_hitThisSwing = new();
        private readonly HashSet<Transform> m_blockedRoots = new();
        private bool m_swinging;

        public void BeginSwing()
        {
            m_hitThisSwing.Clear();
            m_blockedRoots.Clear();
            m_swinging = true;

            var col = GetComponent<Collider>();
            var overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);
            Debug.Log($"[WeaponHitbox] BeginSwing on {gameObject.name} — found {overlaps.Length} overlapping colliders");
            foreach (var other in overlaps)
                TryHit(other);
        }

        public void EndSwing() => m_swinging = false;

        void OnTriggerEnter(Collider other) => TryHit(other);

        private void TryHit(Collider other)
        {
            if (!m_swinging) return;
            if (m_hitThisSwing.Contains(other)) return;
            if (other.transform.root == transform.root) return;

            Debug.Log($"[WeaponHitbox] {gameObject.name} hit collider '{other.name}' on '{other.transform.root.name}' — tag='{other.tag}', isTrigger={other.isTrigger}, enabled={other.enabled}");

            // Shield block detection — collider tagged "Shield" intercepts the blow.
            if (other.CompareTag("Shield"))
            {
                Debug.Log($"[WeaponHitbox] BLOCKED! Shield collider hit on {other.transform.root.name}");
                m_hitThisSwing.Add(other);
                m_blockedRoots.Add(other.transform.root);
                EventManager.RaiseBlock(new BlockArgs(other.transform.root.gameObject, gameObject));
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (m_blockedRoots.Contains(other.transform.root))
                {
                    Debug.Log($"[WeaponHitbox] Skipped damage on {other.transform.root.name} — already blocked this swing");
                    return;
                }
                Debug.Log($"[WeaponHitbox] DAMAGE: {damage} to {other.transform.root.name}");
                m_hitThisSwing.Add(other);
                damageable.TakeDamage(damage, damageType, gameObject);
            }
            else
            {
                Debug.Log($"[WeaponHitbox] No IDamageable found on '{other.name}' root '{other.transform.root.name}'");
            }
        }
    }
}