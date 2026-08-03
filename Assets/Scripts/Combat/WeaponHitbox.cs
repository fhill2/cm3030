using System.Collections.Generic;
using UnityEngine;
using Game.Shared;

namespace Game.Combat
{
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private DamageType damageType = DamageType.Light;

        private readonly HashSet<Collider> m_hitThisSwing = new();
        private bool m_swinging;

        public void BeginSwing()
        {
            m_hitThisSwing.Clear();
            m_swinging = true;

            var col = GetComponent<Collider>();
            var overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);
            Debug.Log($"SWING START — overlapping {overlaps.Length} colliders");
            foreach (var other in overlaps)
                TryHit(other);
        }

        public void EndSwing() => m_swinging = false;

        void OnTriggerEnter(Collider other) => TryHit(other);

        private void TryHit(Collider other)
        {
            if (!m_swinging) return;
            if (m_hitThisSwing.Contains(other)) return;
            if (other.transform.root == transform.root)
            {
                Debug.Log($"REJECTED self: {other.name}");
                return;
            }

            Debug.Log($"TOUCHED: {other.name}");

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                m_hitThisSwing.Add(other);
                damageable.TakeDamage(damage, damageType, gameObject);
            }
        }
    }
}