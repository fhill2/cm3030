using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    public class ThrownWeapon : MonoBehaviour
    {
        [Tooltip("Metres per second the weapon flies.")]
        [SerializeField] private float speed = 30f;

        [Tooltip("Radius of the hit check. Slightly generous so near misses still land.")]
        [SerializeField] private float radius = 0.4f;

        [Tooltip("Degrees per second the weapon spins in flight.")]
        [SerializeField] private float spin = 720f;

        [Tooltip("Upper bound of the random roll around the blade axis added on top of the tumble.")]
        [SerializeField] private float rollSpin = 120f;

        [Tooltip("Seconds of flight before it falls, so it never sails off the map.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("Downward acceleration while in flight (m/s^2).")]
        [SerializeField] private float gravity = -5f;

        private GameObject thrower;
        private Vector3 flightDirection = Vector3.forward;
        private float roll;
        private float fallSpeed;
        private float launchedAt;
        private bool spent;

        public void Launch(GameObject source, Vector3 direction)
        {
            thrower = source;
            flightDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            roll = Random.Range(-rollSpin, rollSpin);
            fallSpeed = 0f;
            launchedAt = Time.time;
        }

        private void Update()
        {
            if (spent) return;

            transform.Rotate(Vector3.right, spin * Time.deltaTime, Space.Self);
            transform.Rotate(Vector3.up, roll * Time.deltaTime, Space.Self);

            fallSpeed += gravity * Time.deltaTime;

            Vector3 velocity = flightDirection * speed + Vector3.up * fallSpeed;
            float step = velocity.magnitude * Time.deltaTime;

            if (Physics.SphereCast(transform.position, radius, velocity.normalized,
                    out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsThrower(hit.transform))
                {
                    Impact(hit);
                    return;
                }
            }

            transform.position += velocity * Time.deltaTime;

            if (Time.time - launchedAt > lifetime) Fall();
        }

        private bool IsThrower(Transform other)
        {
            if (thrower == null || other == null) return false;

            Transform throwerRoot = thrower.transform.root;
            return other == thrower.transform
                   || other.IsChildOf(throwerRoot)
                   || throwerRoot.IsChildOf(other);
        }

        private void Impact(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (target != null && target.IsAlive)
                target.TakeDamage(99999f, DamageType.Melee, thrower);

            transform.position = hit.point;
            Fall();
        }

        private void Fall()
        {
            spent = true;

            var gravity = GetComponent<Gravity>();
            if (gravity == null) gravity = gameObject.AddComponent<Gravity>();
            gravity.SelfRighting = true;

            Vector3 lateral = Random.insideUnitSphere;
            lateral.y = 0f;
            lateral = lateral.sqrMagnitude > 0.001f ? lateral.normalized : Vector3.forward;

            gravity.Launch(
                Vector3.up * 2.5f + lateral * 2f,
                Random.insideUnitSphere * 300f,
                thrower != null ? thrower.transform : null);

            StartCoroutine(HaloAfterDelay());
        }

        private IEnumerator HaloAfterDelay()
        {
            yield return new WaitForSeconds(1f);

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
                gameObject.AddComponent<Halo>().Place(hit.point);
        }
    }
}
