using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class BloodEffects : MonoBehaviour
    {
        [Tooltip("RVFX splash prefabs; one is picked at random per hit.")]
        [SerializeField] private GameObject[] splashPrefabs;

        private const float CleanupDelay = 6f;

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target != gameObject) return;
            if (splashPrefabs == null || splashPrefabs.Length == 0) return;

            Vector3 source = e.Source != null
                ? e.Source.transform.position
                : transform.position + Vector3.forward;

            Bounds bounds = WorldBounds();
            Vector3 point = bounds.ClosestPoint(source);
            Vector3 direction = point - source;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.up;
            direction.Normalize();

            GameObject prefab = splashPrefabs[Random.Range(0, splashPrefabs.Length)];
            GameObject splash = Instantiate(prefab, point, Quaternion.LookRotation(direction));
            Destroy(splash, CleanupDelay);
        }

        private Bounds WorldBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in renderers)
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any ? bounds : new Bounds(transform.position, Vector3.one);
        }
    }
}
