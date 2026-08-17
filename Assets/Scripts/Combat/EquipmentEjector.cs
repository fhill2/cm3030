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
        private const float Spin = 300f;

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

            Launch(equipment.WeaponInstance);
            Launch(equipment.ShieldInstance);
        }

        private void Launch(GameObject gear)
        {
            if (gear == null) return;

            foreach (var wc in gear.GetComponentsInChildren<WeaponCollider>())
                wc.enabled = false;

            foreach (var sc in gear.GetComponentsInChildren<ShieldCollider>())
                sc.enabled = false;

            Transform owner = gear.transform.root;
            gear.transform.SetParent(null);

            var gravity = gear.GetComponent<Gravity>();
            if (gravity == null) gravity = gear.AddComponent<Gravity>();

            Vector3 lateral = Random.insideUnitSphere;
            lateral.y = 0f;
            lateral = lateral.sqrMagnitude > 0.001f ? lateral.normalized : Vector3.forward;

            gravity.Launch(
                Vector3.up * UpForce + lateral * LateralForce,
                Random.insideUnitSphere * Spin,
                owner);
        }
    }
}
