using System.Collections.Generic;
using UnityEngine;
using Game.Shared;

namespace Game.Combat
{
    /// <summary>
    /// Physics-based melee hit detection. Lives on a GameObject with a trigger
    /// collider that represents the weapon's blade volume (usually parented to
    /// the attacker's hand bone, so it sweeps with the swing animation).
    ///
    /// During a swing the attacker calls <see cref="BeginSwing"/>; while active,
    /// <see cref="OnTriggerEnter"/> bridges to <see cref="IDamageable.TakeDamage"/>
    /// on whatever body hitbox the blade enters. A per-swing HashSet prevents a
    /// single swing from hitting the same target more than once.
    ///
    /// This is the foundation of the directional combat system: later, weapon-
    /// vs-weapon/shield colliders can return "blocked" instead of damaging,
    /// by filtering on tag/layer in OnTriggerEnter.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private DamageType damageType = DamageType.Light;

        private readonly HashSet<Collider> m_hitThisSwing = new();
        private bool m_swinging;

        /// <summary>Call when a swing begins — clears the hit list and arms the hitbox.</summary>
        public void BeginSwing()
        {
            m_hitThisSwing.Clear();
            m_swinging = true;
        }

        /// <summary>Call when the swing's damage window ends.</summary>
        public void EndSwing() => m_swinging = false;

        void OnTriggerEnter(Collider other)
        {
            if (!m_swinging) return;
            if (m_hitThisSwing.Contains(other)) return;             // one hit per swing per target
            if (other.transform.root == transform.root) return;     // never hit self

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                m_hitThisSwing.Add(other);
                damageable.TakeDamage(damage, damageType, gameObject);
            }
        }
    }
}
