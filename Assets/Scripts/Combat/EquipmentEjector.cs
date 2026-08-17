using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class EquipmentEjector : MonoBehaviour
    {
        private const float EjectDelay = 0.5f;
        private const float UpForce = 2.5f;
        private const float LateralForce = 1.5f;
        private const float Spin = 6f;
        private const float GearMass = 1.5f;

        private Equipment equipment;

        void Awake()
        {
            equipment = GetComponent<Equipment>();
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
            StartCoroutine(EjectRoutine());
        }

        private IEnumerator EjectRoutine()
        {
            yield return new WaitForSeconds(EjectDelay);

            if (equipment == null) yield break;

            Eject(equipment.WeaponInstance, false);
            Eject(equipment.ShieldInstance, true);
        }

        private void Eject(GameObject gear, bool needsSolidCollider)
        {
            if (gear == null) return;

            foreach (var wc in gear.GetComponentsInChildren<WeaponCollider>())
                wc.enabled = false;

            foreach (var sc in gear.GetComponentsInChildren<ShieldCollider>())
                sc.enabled = false;

            gear.transform.SetParent(null);

            if (needsSolidCollider) AddSolidCollider(gear);

            var rb = gear.AddComponent<Rigidbody>();
            rb.mass = GearMass;

            Vector3 lateral = Random.insideUnitSphere;
            lateral.y = 0f;
            lateral = lateral.sqrMagnitude > 0.001f ? lateral.normalized : Vector3.forward;

            rb.linearVelocity = Vector3.up * UpForce + lateral * LateralForce;
            rb.angularVelocity = Random.insideUnitSphere * Spin;
        }

        private static void AddSolidCollider(GameObject gear)
        {
            var renderers = gear.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var col = gear.AddComponent<BoxCollider>();
            col.center = gear.transform.InverseTransformPoint(bounds.center);

            Vector3 s = gear.transform.lossyScale;
            col.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)),
                bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)),
                bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
        }
    }
}
