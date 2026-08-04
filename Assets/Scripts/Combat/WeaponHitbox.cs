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

        private readonly HashSet<IDamageable> m_hitTargets = new();
        private readonly HashSet<Transform> m_blockedRoots = new();
        private bool m_swinging;

        public void BeginSwing()
        {
            m_hitTargets.Clear();
            m_blockedRoots.Clear();
            m_swinging = true;

            var col = GetComponent<Collider>();
            var overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);
            foreach (var other in overlaps)
                TryHit(other);
        }

        public void EndSwing() => m_swinging = false;

        void OnTriggerEnter(Collider other) => TryHit(other);

        private void TryHit(Collider other)
        {
            if (!m_swinging) return;
            if (other.transform.root == transform.root) return;

            // Shield block detection — collider tagged "Shield" intercepts the blow.
            if (other.CompareTag("Shield"))
            {
                if (m_blockedRoots.Add(other.transform.root))
                    EventManager.RaiseBlock(new BlockArgs(other.transform.root.gameObject, gameObject));
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (m_blockedRoots.Contains(other.transform.root)) return;
                if (!m_hitTargets.Add(damageable)) return;
                Debug.Log($"[WeaponHitbox] {damage} damage to {other.transform.root.name}");
                damageable.TakeDamage(damage, damageType, gameObject);
            }
        }
    }
}