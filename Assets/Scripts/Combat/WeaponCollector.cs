using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    // Tab picks up the nearest dropped weapon, throwing the current one clear.
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard.tabKey.wasPressedThisFrame) return;
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

            // Strip the ground-state components before it goes into the hand.
            foreach (Halo halo in gear.GetComponentsInChildren<Halo>()) Destroy(halo);

            ThrownWeapon flight = gear.GetComponent<ThrownWeapon>();
            if (flight != null) Destroy(flight);

            Gravity fall = gear.GetComponent<Gravity>();
            if (fall != null) Destroy(fall);

            // Point the prefab reference at the picked-up weapon too, so a
            // later respawn gives the right one.
            GameObject sourcePrefab = FindPrefabFor(gear);
            if (sourcePrefab != null) equipment.WeaponPrefab = sourcePrefab;

            equipment.TakeWeaponInstance(gear);

            if (actorAudio != null) actorAudio.PlayUnequipDelayed();

            Debug.Log($"[WeaponCollector] Swapped for {gear.name}.");
        }

        // Matches by name, since the instance has lost its link to the prefab.
        private GameObject FindPrefabFor(GameObject gear)
        {
            string name = gear.name.Replace("(Clone)", "").Trim();

            foreach (EquipmentEntry entry in EquipmentCatalog.Weapons)
                if (entry.Prefab != null && entry.Prefab.name == name)
                    return entry.Prefab;

            return null;
        }

        private Weapon FindNearestGroundWeapon()
        {
            Weapon best = null;
            float bestDistanceSqr = collectRadius * collectRadius;

            foreach (Weapon weapon in FindObjectsByType<Weapon>(FindObjectsSortMode.None))
            {
                // A weapon still in someone's hand is a child of that actor.
                if (weapon.transform.root != weapon.transform) continue;

                Gravity falling = weapon.GetComponent<Gravity>();
                if (falling != null && falling.IsFalling) continue;

                float distanceSqr = (weapon.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr > bestDistanceSqr) continue;

                bestDistanceSqr = distanceSqr;
                best = weapon;
            }

            return best;
        }
    }
}