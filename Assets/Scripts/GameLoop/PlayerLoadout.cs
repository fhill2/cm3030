using UnityEngine;
using Game.Combat;

namespace Game.Core
{
    // Applies shop upgrades to the player's gear.
    //
    // The weapon's WeaponDef is a shared asset — writing to it directly would
    // upgrade every enemy holding the same sword and would dirty the file in
    // git. So the first time an upgrade lands we swap the weapon over to a
    // runtime copy made with Instantiate(), and only ever edit that copy.
    // The copy dies with play mode, so upgrades last a run, not forever.
    //
    // Goes on the GameManager object.
    public class PlayerLoadout : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player object. The weapon is found in its children at runtime.")]
        [SerializeField] private Transform player;

        [Header("Limits")]
        [Tooltip("Swing duration can never drop below this, however many speed upgrades are bought.")]
        [SerializeField] private float minimumSwingDuration = 0.4f;

        // The runtime copy we're allowed to modify. Null until the first upgrade.
        private WeaponDef weaponCopy;

        public void AddWeaponDamage(float amount)
        {
            WeaponDef def = EditableWeaponDef();
            if (def == null) return;

            def.Damage += amount;
            Debug.Log($"[Loadout] Weapon damage now {def.Damage}");
        }

        public void AddWeaponSpeed(float amount)
        {
            WeaponDef def = EditableWeaponDef();
            if (def == null) return;

            // Speed is a duration, so a faster weapon is a smaller number.
            def.Speed = Mathf.Max(minimumSwingDuration, def.Speed - amount);
            Debug.Log($"[Loadout] Swing duration now {def.Speed}");
        }

        // Finds the player's weapon and makes sure we're pointing at a copy
        // of its def rather than the shared asset.
        private WeaponDef EditableWeaponDef()
        {
            if (player == null)
            {
                Debug.LogWarning("[Loadout] No player assigned.");
                return null;
            }

            Weapon weapon = player.GetComponentInChildren<Weapon>();
            if (weapon == null)
            {
                Debug.LogWarning("[Loadout] No Weapon found on the player. Is the weapon spawned yet?");
                return null;
            }

            // Already swapped to our copy, and the weapon still holds it.
            if (weaponCopy != null && weapon.Def == weaponCopy) return weaponCopy;

            if (weapon.Def == null)
            {
                Debug.LogWarning("[Loadout] Weapon has no Def.");
                return null;
            }

            weaponCopy = Instantiate(weapon.Def);
            weaponCopy.name = weapon.Def.name + " (Upgraded)";
            weapon.Def = weaponCopy;

            return weaponCopy;
        }
    }
}