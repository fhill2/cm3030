using System.Collections.Generic;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    [RequireComponent(typeof(Collider))]
    public class WeaponCollider : MonoBehaviour
    {
        private readonly HashSet<IDamageable> m_hitTargets = new();
        private readonly HashSet<Transform> m_blockedRoots = new();
        private bool m_swinging;
        private bool m_resultRegistered;

        private WeaponDef ResolveDef() => GetComponentInParent<Weapon>()?.Def;

        public void BeginSwing()
        {
            m_hitTargets.Clear();
            m_blockedRoots.Clear();
            m_swinging = true;
            m_resultRegistered = false;
        }

        public void EndSwing() => m_swinging = false;

        void Update()
        {
            if (!m_swinging || m_resultRegistered) return;

            var col = GetComponent<Collider>();
            var overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);

            foreach (var other in overlaps)
            {
                TryHit(other);
                if (m_resultRegistered) break;
            }
        }

        private void TryHit(Collider other)
        {
            if (!m_swinging) return;
            if (m_resultRegistered) return;
            if (other.transform.root == transform.root) return;

            // Friendly fire guard: skip anything on the same team as whoever
            // owns this weapon (e.g. an enemy's swing landing on another
            // enemy). Player and Enemy are tags on each character's root
            // GameObject, same convention as the self-hit check above.
            if (other.transform.root.CompareTag(transform.root.tag)) return;

            if (other.CompareTag("Shield"))
            {
                if (other.enabled)
                    RegisterBlock(other.transform.root);
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (m_blockedRoots.Contains(other.transform.root)) return;

                if (IsBlockedByShield(other.transform.root))
                {
                    RegisterBlock(other.transform.root);
                    return;
                }

                if (!m_hitTargets.Add(damageable)) return;

                var def = ResolveDef();
                float dmg = def != null ? def.Damage : 0f;
                damageable.TakeDamage(dmg, DamageType.Melee, gameObject);
                m_resultRegistered = true;
            }
        }

        private void RegisterBlock(Transform root)
        {
            if (m_blockedRoots.Add(root))
            {
                EventManager.RaiseBlock(new BlockArgs(root.gameObject, gameObject));
                m_resultRegistered = true;
            }
        }

        private bool IsBlockedByShield(Transform targetRoot)
        {
            var bladeCol = GetComponent<Collider>();
            if (bladeCol == null) return false;

            foreach (var col in targetRoot.GetComponentsInChildren<Collider>())
            {
                if (!col.CompareTag("Shield")) continue;
                if (!col.enabled) continue;

                if (bladeCol.bounds.Intersects(col.bounds))
                    return true;
            }
            return false;
        }
    }
}
