using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Game.Audio;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class Knockback : MonoBehaviour
    {
        [Tooltip("Upward launch speed in m/s.")]
        [SerializeField] private float launchHeight = 12f;

        [Tooltip("Horizontal push speed away from the attacker in m/s.")]
        [SerializeField] private float launchPush = 7f;

        [Tooltip("Downward acceleration while airborne (m/s^2).")]
        [SerializeField] private float gravity = -16f;

        [Tooltip("Distance above the ground at which the enemy is considered landed.")]
        [SerializeField] private float landDistance = 0.2f;

        [Tooltip("Seconds the enemy is frozen on impact before the launch sends them flying.")]
        [SerializeField] private float launchDelay = 0.25f;

        [Tooltip("Radius of the wall sweep while airborne, matched loosely to the body.")]
        [SerializeField] private float wallRadius = 0.35f;

        [Tooltip("Gap kept between the enemy and whatever stopped them.")]
        [SerializeField] private float skinWidth = 0.05f;

        private NavMeshAgent agent;
        private WeaponCollider[] weaponColliders;
        private Vector3 velocity;
        private bool airborne;
        private Coroutine pendingLaunch;

        public bool Airborne => airborne;

        public void Launch(Vector3 fromPosition)
        {
            if (airborne || pendingLaunch != null) return;

            agent = GetComponent<NavMeshAgent>();
            weaponColliders = GetComponentsInChildren<WeaponCollider>(true);

            ActorAudio audio = GetComponentInChildren<ActorAudio>();
            if (audio != null) audio.PlayHit();

            if (agent != null && agent.enabled)
            {
                agent.velocity = Vector3.zero;
                agent.enabled = false;
            }

            foreach (WeaponCollider wc in weaponColliders)
                if (wc != null) wc.enabled = false;

            pendingLaunch = StartCoroutine(LaunchRoutine(fromPosition));
        }

        private IEnumerator LaunchRoutine(Vector3 fromPosition)
        {
            yield return new WaitForSeconds(launchDelay);
            pendingLaunch = null;

            Vector3 away = transform.position - fromPosition;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.forward;

            velocity = Vector3.up * launchHeight + away * launchPush;
            airborne = true;
        }

        void Update()
        {
            if (!airborne) return;

            velocity.y += gravity * Time.deltaTime;
            MoveWithCollisions();
            TryLand();
        }

        private void MoveWithCollisions()
        {
            Vector3 move = velocity * Time.deltaTime;
            float dist = move.magnitude;
            if (dist < 0.0001f) return;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.SphereCast(origin, wallRadius, move.normalized, out RaycastHit hit,
                    dist + skinWidth, ~0, QueryTriggerInteraction.Ignore) &&
                !CombatProbe.IsIgnored(hit.transform, transform))
            {
                float travel = Mathf.Max(0f, hit.distance - skinWidth);
                transform.position += move.normalized * travel;
                velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
            }
            else
            {
                transform.position += move;
            }
        }

        private void TryLand()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (velocity.y < 0f &&
                Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.5f + landDistance,
                    ~0, QueryTriggerInteraction.Ignore) &&
                !CombatProbe.IsIgnored(hit.transform, transform))
            {
                Vector3 grounded = hit.point;
                grounded.y += 0.05f;
                transform.position = grounded;
                Land();
            }
        }

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;

            if (pendingLaunch != null)
            {
                StopCoroutine(pendingLaunch);
                pendingLaunch = null;
            }

            airborne = false;
        }

        private void Land()
        {
            airborne = false;

            if (agent != null && !agent.isActiveAndEnabled)
                agent.enabled = true;

            foreach (WeaponCollider wc in weaponColliders)
                if (wc != null) wc.enabled = true;

            Destroy(this);
        }
    }
}
