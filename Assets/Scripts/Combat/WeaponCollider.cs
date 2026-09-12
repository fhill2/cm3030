using System.Collections.Generic;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    // Sits on the blade. While a swing is live it checks what it overlaps and
    // deals the weapon's damage to the first valid target, or reports a block
    // if a raised shield is in the way.
    [RequireComponent(typeof(Collider))]
    public class WeaponCollider : MonoBehaviour
    {
        private readonly HashSet<IDamageable> hitTargets = new();
        private readonly HashSet<Transform> blockedRoots = new();
        private bool swinging;

        // One hit or block per swing, so a single slash can't chew through a
        // crowd or land twice on the same enemy.
        private bool resultRegistered;

        private WeaponDef ResolveDef() => GetComponentInParent<Weapon>()?.Def;

        public void BeginSwing()
        {
            hitTargets.Clear();
            blockedRoots.Clear();
            swinging = true;
            resultRegistered = false;
        }

        public void EndSwing() => swinging = false;

        void Update()
        {
            if (!swinging || resultRegistered) return;

            Collider bladeCollider = GetComponent<Collider>();
            Collider[] overlaps = Physics.OverlapBox(
                bladeCollider.bounds.center, bladeCollider.bounds.extents, transform.rotation);

            foreach (Collider other in overlaps)
            {
                TryHit(other);
                if (resultRegistered) break;
            }
        }

        private void TryHit(Collider other)
        {
            if (!swinging) return;
            if (resultRegistered) return;
            if (other.transform.root == transform.root) return;

            // Skip anything on the same team as whoever owns this weapon, so
            // an orc's swing can't land on another orc. Player and Enemy are
            // tags on each character's root.
            if (other.transform.root.CompareTag(transform.root.tag)) return;

            if (other.CompareTag("Shield"))
            {
                if (other.enabled)
                    RegisterBlock(other.transform.root);
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                if (blockedRoots.Contains(other.transform.root)) return;

                if (IsBlockedByShield(other.transform.root))
                {
                    RegisterBlock(other.transform.root);
                    return;
                }

                if (!hitTargets.Add(damageable)) return;

                WeaponDef def = ResolveDef();
                float damage = def != null ? def.Damage : 0f;
                damageable.TakeDamage(damage, DamageType.Melee, gameObject);
                resultRegistered = true;
            }
        }

        private void RegisterBlock(Transform root)
        {
            if (blockedRoots.Add(root))
            {
                EventManager.RaiseBlock(new BlockArgs(root.gameObject, gameObject));
                resultRegistered = true;
            }
        }

        // Catches the case where the blade reaches the body and the shield at
        // the same time, so a raised shield still wins.
        private bool IsBlockedByShield(Transform targetRoot)
        {
            Collider bladeCollider = GetComponent<Collider>();
            if (bladeCollider == null) return false;

            foreach (Collider collider in targetRoot.GetComponentsInChildren<Collider>())
            {
                if (!collider.CompareTag("Shield")) continue;
                if (!collider.enabled) continue;

                if (bladeCollider.bounds.Intersects(collider.bounds))
                    return true;
            }
            return false;
        }
    }
}