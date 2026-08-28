using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    public class WeaponCollector : MonoBehaviour
    {
        [Tooltip("Maximum distance from the player at which a dropped weapon can be collected.")]
        [SerializeField] private float collectRadius = 2.5f;

        private Equipment equipment;
        private EquipmentEjector ejector;
        private ActorAudio actorAudio;
        private StaminaSystem stamina;
        private Animator animator;

        void Awake()
        {
            equipment = GetComponent<Equipment>();
            ejector = GetComponent<EquipmentEjector>();
            actorAudio = GetComponent<ActorAudio>();
            stamina = GetComponent<StaminaSystem>();
            animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.tabKey.wasPressedThisFrame) return;
            if (PlayerInputLock.InputLocked) return;
            if (stamina != null && !stamina.CanAct) return;

            TryCollect();
        }

        void TryCollect()
        {
            Weapon candidate = FindNearestGroundWeapon();
            if (candidate == null) return;

            if (animator != null) animator.SetTrigger(AnimParams.Collect);

            GameObject gear = candidate.gameObject;

            GameObject current = equipment != null ? equipment.WeaponInstance : null;
            if (current != null && ejector != null) ejector.Eject(current);
            else if (current != null && equipment != null) equipment.Detach(current);

            if (current != null && actorAudio != null) actorAudio.PlayDrop();

            foreach (var halo in gear.GetComponentsInChildren<Halo>()) Destroy(halo);
            var flight = gear.GetComponent<ThrownWeapon>();
            if (flight != null) Destroy(flight);
            var fall = gear.GetComponent<Gravity>();
            if (fall != null) Destroy(fall);

            GameObject sourcePrefab = FindPrefabFor(gear);
            if (sourcePrefab != null) equipment.WeaponPrefab = sourcePrefab;

            equipment.TakeWeaponInstance(gear);

            if (actorAudio != null) actorAudio.PlayUnequipDelayed();

            Debug.Log($"[WeaponCollector] Swapped for {gear.name}.");
        }

        private GameObject FindPrefabFor(GameObject gear)
        {
            string name = gear.name.Replace("(Clone)", "").Trim();

            foreach (var entry in EquipmentCatalog.Weapons)
                if (entry.Prefab != null && entry.Prefab.name == name)
                    return entry.Prefab;

            return null;
        }

        private Weapon FindNearestGroundWeapon()
        {
            Weapon best = null;
            float bestSqr = collectRadius * collectRadius;

            foreach (Weapon w in FindObjectsByType<Weapon>(FindObjectsSortMode.None))
            {
                if (w.transform.root != w.transform) continue;

                var falling = w.GetComponent<Gravity>();
                if (falling != null && falling.IsFalling) continue;

                float sqr = (w.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                best = w;
            }

            return best;
        }
    }
}
