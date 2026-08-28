using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class EquipmentEjector : MonoBehaviour
    {
        private const float EjectDelay = 0.5f;
        private const float HaloDelay = 1f;
        private const float MinUpForce = 2.5f;
        private const float MaxUpForce = 4.5f;
        private const float MinLateralForce = 3f;
        private const float MaxLateralForce = 6f;
        private const float MinSpin = 250f;
        private const float MaxSpin = 450f;

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

        public void Eject(GameObject gear)
        {
            if (gear == null) return;
            Launch(gear);
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
            equipment.Detach(gear);

            var gravity = gear.GetComponent<Gravity>();
            if (gravity == null) gravity = gear.AddComponent<Gravity>();
            gravity.SelfRighting = true;

            Vector3 lateral = Random.insideUnitSphere;
            lateral.y = 0f;
            lateral = lateral.sqrMagnitude > 0.001f ? lateral.normalized : Vector3.forward;

            gravity.Launch(
                Vector3.up * Random.Range(MinUpForce, MaxUpForce) + lateral * Random.Range(MinLateralForce, MaxLateralForce),
                Random.insideUnitSphere * Random.Range(MinSpin, MaxSpin),
                owner);

            StartCoroutine(HaloAfterDelay(gear));
        }

        private IEnumerator HaloAfterDelay(GameObject gear)
        {
            yield return new WaitForSeconds(HaloDelay);

            if (gear == null) yield break;

            Vector3 origin = gear.transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
                gear.AddComponent<Halo>().Place(hit.point);
        }
    }
}
