using UnityEngine;
using Game.Audio;
using Game.Shared;

namespace Game.Core
{
    // Flies forward until it hits something, damages the first damageable
    // thing it meets, then removes itself.
    // Movement is broken into sub-steps so a fast projectile can't jump past a target.
    // Goes on the projectile prefab. 
    public class SpellProjectile : MonoBehaviour
    {
        [Header("Environment")]
        [Tooltip("Tight check so the projectile stops where it looks like it should.")]
        [SerializeField] private float radius = 0.15f;

        [Tooltip("Walls, terrain and anything else solid the projectile dies against.")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Targets")]
        [Tooltip("Generous check so the projectile can't thread between an enemy's arm and torso.")]
        [SerializeField] private float targetRadius = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool logHits;

        private float damage;
        private float speed;
        private GameObject caster;
        private SpellDef spell;
        private Transform casterRoot;

        private GameObject impactEffect;
        private float impactScale = 1f;
        private float impactLifetime = 3f;

        // Called by SpellCaster as soon as the projectile is created.
        public void Launch(SpellDef spellDef, float damageAmount, float projectileSpeed,
                           float lifetime, GameObject source)
        {
            spell = spellDef;
            damage = damageAmount;
            speed = projectileSpeed;
            caster = source;
            casterRoot = source != null ? source.transform.root : null;

            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        public void SetImpact(GameObject effect, float scale, float lifetime)
        {
            impactEffect = effect;
            impactScale = scale;
            impactLifetime = lifetime;
        }

        private void Update()
        {
            float remaining = speed * Time.deltaTime;

            // Never advance further than the target radius in one go, or the
            // projectile can pass clean through an enemy between checks.
            float maxStep = Mathf.Max(0.05f, targetRadius * 0.5f);

            while (remaining > 0f)
            {
                float step = Mathf.Min(maxStep, remaining);
                remaining -= step;

                // Targets first, so an enemy standing against a wall takes the
                // hit rather than the stonework behind them.
                if (TryHitTarget()) return;
                if (TryHitEnvironment(step)) return;

                transform.position += transform.forward * step;
            }
        }

        private bool TryHitTarget()
        {
            Collider[] nearby = Physics.OverlapSphere(
                transform.position, targetRadius, hitMask, QueryTriggerInteraction.Ignore);

            foreach (Collider col in nearby)
            {
                if (IsCaster(col.transform)) continue;

                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive) continue;

                if (logHits)
                    Debug.Log($"[SpellProjectile] Hit {col.transform.root.name} for {damage}");

                target.TakeDamage(damage, DamageType.Magic, caster);
                Detonate(transform.position, -transform.forward);
                return true;
            }

            return false;
        }

        private bool TryHitEnvironment(float step)
        {
            if (!Physics.SphereCast(transform.position, radius, transform.forward,
                    out RaycastHit hit, step, hitMask,
                    QueryTriggerInteraction.Ignore))
                return false;

            if (IsCaster(hit.transform))
            {
                // Pass through our own body rather than dying on the frame we
                // spawned.
                transform.position += transform.forward * step;
                return true;
            }

            if (logHits)
                Debug.Log($"[SpellProjectile] Stopped on {hit.collider.name}");

            // Face the effect out of the surface so sparks fly away from the
            // wall, not into it.
            Detonate(hit.point, hit.normal);
            return true;
        }

        // The effect is a separate object so it can outlive the ball and
        // finish its particles.
        private void Detonate(Vector3 position, Vector3 normal)
        {
            AudioClip[] hitClips = spell != null ? spell.HitClip : null;
            if (hitClips != null)
                foreach (AudioClip clip in hitClips)
                    OneShotAudio.Play2D(clip, transform.position);
            else if (logHits)
                Debug.Log($"[SpellProjectile] {spell?.DisplayName} hit has no clips");

            if (impactEffect != null)
            {
                GameObject vfx = Instantiate(
                    impactEffect, position, Quaternion.LookRotation(normal));

                vfx.transform.localScale *= impactScale;
                Destroy(vfx, impactLifetime);
            }

            Destroy(gameObject);
        }

        private bool IsCaster(Transform other)
        {
            if (casterRoot == null || other == null) return false;
            return other.IsChildOf(casterRoot);
        }
    }
}