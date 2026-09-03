using UnityEngine;
using Game.Audio;
using Game.Shared;

namespace Game.Core
{
    // Flies forward until it hits something, damages the first damageable
    // thing it meets, then removes itself.
    //
    // Targets are found with an overlap check rather than a cast, because a
    // sphere cast ignores colliders it is already touching when the sweep
    // begins. The frame's movement is broken into sub-steps so a fast
    // projectile can't jump past a target between checks on a slow frame.
    //
    // Goes on the projectile prefab. No collider or rigidbody needed.
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
        private Transform casterRoot;

        // Kept so the projectile can play the spell's hit audio when it lands.
        private SpellDef spell;

        private GameObject impactEffect;
        private float impactScale = 1f;
        private float impactLifetime = 3f;

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

        // Impact visuals are set separately so Launch keeps the same signature
        // it had before the VFX existed.
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

                // Targets first. An enemy standing against a wall should take
                // the hit rather than the stonework behind them.
                if (TryHitTarget()) return;
                if (TryHitEnvironment(step)) return;

                transform.position += transform.forward * step;
            }
        }

        // Anything damageable within reach of where the projectile is now.
        private bool TryHitTarget()
        {
            Collider[] near = Physics.OverlapSphere(
                transform.position, targetRadius, hitMask, QueryTriggerInteraction.Ignore);

            foreach (Collider col in near)
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

        // Nothing damageable in range, so stop against scenery if we met any.
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

            // Face the effect out of the surface, so sparks fly away from the
            // wall instead of into it.
            Detonate(hit.point, hit.normal);
            return true;
        }

        // Spawn the impact effect and remove the projectile. The effect is a
        // separate object so it can outlive the ball and finish its particles.
        private void Detonate(Vector3 position, Vector3 normal)
        {
            if (impactEffect != null)
            {
                GameObject vfx = Instantiate(
                    impactEffect, position, Quaternion.LookRotation(normal));

                vfx.transform.localScale *= impactScale;
                Destroy(vfx, impactLifetime);
            }

            var hitClips = spell != null ? spell.HitClip : null;
            if (hitClips != null)
                foreach (var clip in hitClips)
                    OneShotAudio.Play2D(clip, transform.position);
            else if (logHits)
                Debug.Log($"[SpellProjectile] {spell?.DisplayName} hit has no clips");

            Destroy(gameObject);
        }

        // Only the caster's own hierarchy is passed through.
        private bool IsCaster(Transform other)
        {
            if (casterRoot == null || other == null) return false;
            return other.IsChildOf(casterRoot);
        }
    }
}