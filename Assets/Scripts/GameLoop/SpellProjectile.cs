using UnityEngine;
using Game.Audio;
using Game.Shared;

namespace Game.Core
{
    // Flies forward until it hits something, damages the first damageable
    // thing it meets, then removes itself.
    //
    // Movement is a cast per frame rather than a trigger collider, so a fast
    // projectile can't pass straight through a thin enemy between frames.
    //
    // Goes on the projectile prefab. No collider or rigidbody needed.
    public class SpellProjectile : MonoBehaviour
    {
        [Tooltip("Radius of the hit check. Slightly generous so near misses still land.")]
        [SerializeField] private float radius = 0.4f;

        [Tooltip("Layers the projectile stops against. Include enemies and the environment.")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("Log what the projectile hits, to trace one that dies too early.")]
        [SerializeField] private bool logHits = true;

        private float damage;
        private float speed;
        private GameObject caster;
        private SpellDef spell;

        public void Launch(SpellDef spellDef, float damageAmount, float projectileSpeed,
                           float lifetime, GameObject source)
        {
            spell = spellDef;
            damage = damageAmount;
            speed = projectileSpeed;
            caster = source;

            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;

            if (Physics.SphereCast(transform.position, radius, transform.forward,
                    out RaycastHit hit, step, hitMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (IsCaster(hit.transform))
                {
                    // Pass straight through our own body rather than dying on
                    // the frame we spawned.
                    transform.position += transform.forward * step;
                    return;
                }

                Hit(hit);
                return;
            }

            transform.position += transform.forward * step;
        }

        // The caster reference is whatever object SpellCaster sits on, which
        // isn't necessarily the root of the player hierarchy, so compare the
        // whole branch instead of just the roots.
        private bool IsCaster(Transform other)
        {
            if (caster == null || other == null) return false;

            Transform casterRoot = caster.transform.root;
            return other == caster.transform
                   || other.IsChildOf(casterRoot)
                   || casterRoot.IsChildOf(other);
        }

        private void Hit(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (logHits)
            {
                string name = hit.collider.name;
                string root = hit.transform.root.name;
                Debug.Log($"[SpellProjectile] Hit {name} (root {root}), damageable: {target != null}");
            }

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage, DamageType.Magic, caster);
            }

            var hitClips = spell != null ? spell.HitClip : null;
            if (hitClips != null)
                foreach (var clip in hitClips)
                    OneShotAudio.Play2D(clip, transform.position);
            else if (logHits)
                Debug.Log($"[SpellProjectile] {spell?.DisplayName} hit has no clips");

            Destroy(gameObject);
        }
    }
}