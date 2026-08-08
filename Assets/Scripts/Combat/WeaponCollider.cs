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

            if (overlaps.Length > 0)
            {
                string info = "";
                foreach (var o in overlaps)
                    info += $"'{o.name}'(tag:{o.tag}) ";
                Debug.Log($"[WC] blade overlaps: {info}");
            }

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

            if (other.CompareTag("Shield"))
            {
                if (m_blockedRoots.Add(other.transform.root))
                {
                    EventManager.RaiseBlock(new BlockArgs(other.transform.root.gameObject, gameObject));
                    m_resultRegistered = true;
                }
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (m_blockedRoots.Contains(other.transform.root)) return;
                if (!m_hitTargets.Add(damageable)) return;

                var def = ResolveDef();
                float dmg = def != null ? def.Damage : 0f;
                damageable.TakeDamage(dmg, DamageType.Melee, gameObject);
                m_resultRegistered = true;
            }
        }
    }
}
