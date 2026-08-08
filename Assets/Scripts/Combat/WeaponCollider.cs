using System.Collections.Generic;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    // Physics-based melee hit detection. Lives on the weapon's blade collider.
    // Melee arms it (BeginSwing/EndSwing) around the weapon's damage window.
    // Damage/damageType are owned by the weapon (WeaponDef), reached through
    // the Weapon component — this class only does hit detection.
    [RequireComponent(typeof(Collider))]
    public class WeaponCollider : MonoBehaviour
    {
        private readonly HashSet<IDamageable> m_hitTargets = new();
        private readonly HashSet<Transform> m_blockedRoots = new();
        private bool m_swinging;

        // The weapon's data, reached through the Weapon component on the weapon
        // root. Null if no weapon is equipped (damage falls back to zero).
        private WeaponDef ResolveDef() => GetComponentInParent<Weapon>()?.Def;

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

                var def = ResolveDef();
                float dmg = def != null ? def.Damage : 0f;
                var type = def != null ? def.DamageType : DamageType.Light;
                damageable.TakeDamage(dmg, type, gameObject);
            }
        }
    }
}
