using UnityEngine;
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

        private float damage;
        private float speed;
        private GameObject caster;

        // Called by SpellCaster the moment the projectile is created.
        public void Launch(float damageAmount, float projectileSpeed,
                           float lifetime, GameObject source)
        {
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
                // Ignore the caster's own colliders, otherwise the projectile
                // dies on the frame it spawns.
                if (caster == null || hit.transform.root != caster.transform.root)
                {
                    Hit(hit);
                    return;
                }
            }

            transform.position += transform.forward * step;
        }

        private void Hit(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null && target.IsAlive)
            {
                target.TakeDamage(damage, DamageType.Magic, caster);
            }

            Destroy(gameObject);
        }
    }
}